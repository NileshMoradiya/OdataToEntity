﻿using Microsoft.OData;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace OdataToEntity.Parsers.Translators
{
    public readonly struct OeSelectTranslator
    {
        private sealed class ComputeProperty : IEdmStructuralProperty
        {
            public ComputeProperty(String alias, IEdmTypeReference edmTypeReference, Expression expression)
            {
                Name = alias;
                Type = edmTypeReference;
                Expression = expression;
            }

            public IEdmStructuredType DeclaringType => ModelBuilder.PrimitiveTypeHelper.TupleEdmType;
            public String DefaultValueString => throw new NotSupportedException();
            public Expression Expression { get; }
            public String Name { get; }
            public EdmPropertyKind PropertyKind => throw new NotSupportedException();
            public IEdmTypeReference Type { get; }
        }

        private sealed class ReplaceParameterVisitor : ExpressionVisitor
        {
            private readonly Expression _source;

            public ReplaceParameterVisitor(Expression source)
            {
                _source = source;
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                return node.Type == _source.Type ? _source : base.VisitParameter(node);
            }
        }

        private readonly IEdmModel _edmModel;
        private readonly OeJoinBuilder _joinBuilder;
        private readonly OeMetadataLevel _metadataLevel;
        private readonly OeNavigationSelectItem _navigationItem;
        private readonly OeQueryNodeVisitor _visitor;

        public OeSelectTranslator(IEdmModel edmModel, OeJoinBuilder joinBuilder, ODataUri odataUri, OeMetadataLevel metadataLevel)
        {
            _edmModel = edmModel;
            _joinBuilder = joinBuilder;
            _metadataLevel = metadataLevel;
            _navigationItem = new OeNavigationSelectItem(edmModel, odataUri);
            _visitor = joinBuilder.Visitor;
        }

        public Expression Build(Expression source, OeQueryContext queryContext)
        {
            bool isBuild = false;
            if (queryContext.UseModelBoundAttribute == OeModelBoundAttribute.Yes || queryContext.ODataUri.Skip != null || queryContext.ODataUri.Top != null)
            {
                Expression skipTakeSource = BuildSkipTakeSource(source, queryContext, _navigationItem);
                if (source != skipTakeSource)
                {
                    isBuild = true;
                    source = skipTakeSource;
                }

            }

            BuildSelect(queryContext.ODataUri.SelectAndExpand, queryContext.NavigationNextLink, queryContext.UseModelBoundAttribute);
            isBuild |= _navigationItem.StructuralItems.Count > 0 || _navigationItem.NavigationItems.Count > 0;

            if (queryContext.ODataUri.Compute != null)
            {
                isBuild = true;
                BuildCompute(queryContext.ODataUri.Compute);
            }

            source = BuildJoin(source);
            source = BuildOrderBy(source, queryContext.ODataUri.OrderBy);

            if (isBuild)
            {
                source = SelectStructuralProperties(source, _navigationItem);
                source = CreateSelectExpression(source, _joinBuilder);
            }

            return source;
        }
        private void BuildCompute(ComputeClause computeClause)
        {
            foreach (ComputeExpression computeExpression in computeClause.ComputedItems)
            {
                Expression expression = _visitor.TranslateNode(computeExpression.Expression);
                IEdmTypeReference edmTypeReference = OeEdmClrHelper.GetEdmTypeReference(_edmModel, expression.Type);
                var computeProperty = new ComputeProperty(computeExpression.Alias, edmTypeReference, expression);
                _navigationItem.AddStructuralItem(computeProperty, false);
            }
        }
        private Expression BuildJoin(Expression source)
        {
            List<OeNavigationSelectItem> navigationItems = FlattenNavigationItems(_navigationItem, true);
            for (int i = 1; i < navigationItems.Count; i++)
                if (!navigationItems[i].AlreadyUsedInBuildExpression)
                {
                    navigationItems[i].AlreadyUsedInBuildExpression = true;

                    ODataPathSegment segment = navigationItems[i].NavigationSelectItem.PathToNavigationProperty.LastSegment;
                    IEdmNavigationProperty navigationProperty = (((NavigationPropertySegment)segment).NavigationProperty);
                    Expression innerSource = GetInnerSource(navigationItems[i]);
                    source = _joinBuilder.Build(_edmModel, source, innerSource, navigationItems[i].Parent.GetJoinPath(), navigationProperty);
                }

            _visitor.ChangeParameterType(source);
            return source;
        }
        private Expression BuildOrderBy(Expression source, OrderByClause orderByClause)
        {
            _joinBuilder.Visitor.ChangeParameterType(source);
            if (!(source is MethodCallExpression callExpression &&
                (callExpression.Method.Name == nameof(Enumerable.Skip) || callExpression.Method.Name == nameof(Enumerable.Take))))
                source = OeOrderByTranslator.Build(_joinBuilder, source, _joinBuilder.Visitor.Parameter, orderByClause);

            List<OeNavigationSelectItem> navigationItems = FlattenNavigationItems(_navigationItem, false);
            for (int i = 1; i < navigationItems.Count; i++)
            {
                ExpandedNavigationSelectItem item = navigationItems[i].NavigationSelectItem;
                if (item.OrderByOption != null && item.TopOption == null && item.SkipOption == null)
                {
                    IReadOnlyList<IEdmNavigationProperty> joinPath = navigationItems[i].GetJoinPath();
                    source = OeOrderByTranslator.BuildNested(_joinBuilder, source, _joinBuilder.Visitor.Parameter, item.OrderByOption, joinPath);
                }
            }

            return source;
        }
        private void BuildOrderBySkipTake(OeNavigationSelectItem navigationItem, OrderByClause orderByClause, bool hasSelectItems)
        {
            while (orderByClause != null)
            {
                var propertyNode = (SingleValuePropertyAccessNode)orderByClause.Expression;
                if (propertyNode.Source is SingleNavigationNode navigationNode)
                {
                    OeNavigationSelectItem match = null;
                    ExpandedNavigationSelectItem navigationSelectItem = null;
                    do
                    {
                        if ((match = navigationItem.FindHierarchyNavigationItem(navigationNode.NavigationProperty)) != null)
                        {
                            match.AddStructuralItem((IEdmStructuralProperty)propertyNode.Property, true);
                            break;
                        }

                        SelectExpandClause selectExpandClause;
                        if (navigationSelectItem == null)
                        {
                            var pathSelectItem = new PathSelectItem(new ODataSelectPath(new PropertySegment((IEdmStructuralProperty)propertyNode.Property)));
                            selectExpandClause = new SelectExpandClause(new[] { pathSelectItem }, false);
                        }
                        else
                            selectExpandClause = new SelectExpandClause(new[] { navigationSelectItem }, false);

                        var segment = new NavigationPropertySegment(navigationNode.NavigationProperty, navigationNode.NavigationSource);
                        navigationSelectItem = new ExpandedNavigationSelectItem(new ODataExpandPath(segment), navigationNode.NavigationSource, selectExpandClause);
                    }
                    while ((navigationNode = navigationNode.Source as SingleNavigationNode) != null);

                    if (navigationSelectItem != null)
                    {
                        if (match == null)
                            match = navigationItem;

                        var selectItemTranslator = new OeSelectItemTranslator(_edmModel, false, true);
                        selectItemTranslator.Translate(match, navigationSelectItem);
                    }
                }
                else
                {
                    if (hasSelectItems)
                        navigationItem.AddStructuralItem((IEdmStructuralProperty)propertyNode.Property, true);
                }

                orderByClause = orderByClause.ThenBy;
            }
        }
        public void BuildSelect(SelectExpandClause selectClause, bool navigationNextLink, OeModelBoundAttribute useModelBoundAttribute)
        {
            if (selectClause != null)
            {
                var selectItemTranslator = new OeSelectItemTranslator(_edmModel, navigationNextLink, false);
                foreach (SelectItem selectItemClause in selectClause.SelectedItems)
                    selectItemTranslator.Translate(_navigationItem, selectItemClause);
            }

            if (useModelBoundAttribute == OeModelBoundAttribute.Yes)
            {
                SelectItem[] selectItems = _edmModel.GetSelectItems(_navigationItem.EntitySet.EntityType());
                if (selectItems != null)
                {
                    var selectItemTranslator = new OeSelectItemTranslator(_edmModel, navigationNextLink, false);
                    foreach (SelectItem selectItemClause in selectItems)
                        selectItemTranslator.Translate(_navigationItem, selectItemClause);
                }
            }

            _navigationItem.AddKeyRecursive(_metadataLevel != OeMetadataLevel.Full);
        }
        private Expression BuildSkipTakeSource(Expression source, OeQueryContext queryContext, OeNavigationSelectItem navigationItem)
        {
            ODataUri odataUri = queryContext.ODataUri;

            long? top = GetTop(navigationItem, odataUri.Top);
            if (top == null && odataUri.Skip == null)
                return source;

            bool hasSelectItems = HasSelectItems(odataUri.SelectAndExpand);
            BuildOrderBySkipTake(navigationItem, odataUri.OrderBy, hasSelectItems);
            source = BuildJoin(source);

            var expressionBuilder = new OeExpressionBuilder(_joinBuilder);
            source = expressionBuilder.ApplySkipToken(source, queryContext.SkipTokenNameValues, odataUri.OrderBy, queryContext.IsDatabaseNullHighestValue);
            source = expressionBuilder.ApplyOrderBy(source, odataUri.OrderBy);
            source = expressionBuilder.ApplySkip(source, odataUri.Skip, odataUri.Path);
            return expressionBuilder.ApplyTake(source, top, odataUri.Path);
        }
        public OeEntryFactory CreateEntryFactory(IEdmEntitySet entitySet, Type clrType, OePropertyAccessor[] skipTokenAccessors)
        {
            return CreateEntryFactory(_navigationItem, clrType, skipTokenAccessors);
        }
        private static OeEntryFactory CreateEntryFactory(OeNavigationSelectItem root, Type clrType, OePropertyAccessor[] skipTokenAccessors)
        {
            ParameterExpression parameter = Expression.Parameter(typeof(Object));
            UnaryExpression typedParameter = Expression.Convert(parameter, clrType);

            if (root.NavigationItems.Count == 0)
            {
                IReadOnlyList<MemberExpression> propertyExpressions = OeExpressionHelper.GetPropertyExpressions(typedParameter);
                OePropertyAccessor[] accessors;
                if (root.StructuralItems.Count == 0)
                    accessors = OePropertyAccessor.CreateFromType(typedParameter.Type, root.EntitySet);
                else
                {
                    accessors = new OePropertyAccessor[root.StructuralItems.Count];
                    for (int i = 0; i < root.StructuralItems.Count; i++)
                    {
                        OeStructuralSelectItem structuralItem = root.StructuralItems[i];
                        accessors[i] = OePropertyAccessor.CreatePropertyAccessor(structuralItem.EdmProperty, propertyExpressions[i], parameter, structuralItem.SkipToken);
                    }
                }

                var options = new OeEntryFactoryOptions()
                {
                    Accessors = accessors,
                    CountOption = root.CountOption,
                    EntitySet = root.EntitySet,
                    MaxTop = root.MaxTop,
                    PageSize = root.PageSize,
                    SkipTokenAccessors = skipTokenAccessors
                };
                root.EntryFactory = new OeEntryFactory(ref options);
            }
            else
            {
                List<OeNavigationSelectItem> navigationItems = FlattenNavigationItems(root, false);
                IReadOnlyList<MemberExpression> navigationProperties = OeExpressionHelper.GetPropertyExpressions(typedParameter);

                for (int i = navigationItems.Count - 1; i >= 0; i--)
                {
                    OeNavigationSelectItem navigationItem = navigationItems[i];
                    OePropertyAccessor[] accessors = GetAccessors(navigationProperties[i].Type, navigationItem.EntitySet, navigationItem.StructuralItems);
                    LambdaExpression linkAccessor = Expression.Lambda(navigationProperties[i], parameter);
                    OeEntryFactory[] nestedNavigationLinks = GetNestedNavigationLinks(navigationItem);

                    var options = new OeEntryFactoryOptions()
                    {
                        Accessors = accessors,
                        CountOption = navigationItem.CountOption,
                        EdmNavigationProperty = navigationItem.EdmProperty,
                        EntitySet = navigationItem.EntitySet,
                        LinkAccessor = linkAccessor,
                        MaxTop = navigationItem.MaxTop,
                        NavigationLinks = nestedNavigationLinks,
                        NavigationSelectItem = navigationItem.NavigationSelectItem,
                        PageSize = navigationItem.PageSize,
                        SkipTokenAccessors = skipTokenAccessors
                    };
                    navigationItem.EntryFactory = new OeEntryFactory(ref options);
                }
            }

            return root.EntryFactory;
        }
        private MethodCallExpression CreateSelectExpression(Expression source, OeJoinBuilder joinBuilder)
        {
            if (_navigationItem.NavigationItems.Count > 0)
                return (MethodCallExpression)source;

            if (_navigationItem.StructuralItems.Count == 0)
                return (MethodCallExpression)source;

            var expressions = new List<Expression>(_navigationItem.StructuralItems.Count);
            for (int i = 0; i < _navigationItem.StructuralItems.Count; i++)
            {
                IEdmProperty edmProperty = _navigationItem.StructuralItems[i].EdmProperty;
                PropertyInfo clrProperty = _visitor.Parameter.Type.GetPropertyIgnoreCase(edmProperty);
                expressions.Add(Expression.MakeMemberAccess(_visitor.Parameter, clrProperty));
            }
            NewExpression newTupleExpression = OeExpressionHelper.CreateTupleExpression(expressions);

            LambdaExpression lambda = Expression.Lambda(newTupleExpression, _visitor.Parameter);
            MethodInfo selectMethodInfo = OeMethodInfoHelper.GetSelectMethodInfo(_visitor.Parameter.Type, newTupleExpression.Type);
            return Expression.Call(selectMethodInfo, source, lambda);
        }
        private static List<OeNavigationSelectItem> FlattenNavigationItems(OeNavigationSelectItem root, bool includeSkipToken)
        {
            var navigationItems = new List<OeNavigationSelectItem>();
            var stack = new Stack<ValueTuple<OeNavigationSelectItem, int>>();
            stack.Push(new ValueTuple<OeNavigationSelectItem, int>(root, 0));
            do
            {
                ValueTuple<OeNavigationSelectItem, int> stackItem = stack.Pop();
                if (stackItem.Item2 == 0 && (!stackItem.Item1.SkipToken || includeSkipToken))
                    navigationItems.Add(stackItem.Item1);

                if (stackItem.Item2 < stackItem.Item1.NavigationItems.Count)
                {
                    stack.Push(new ValueTuple<OeNavigationSelectItem, int>(stackItem.Item1, stackItem.Item2 + 1));
                    OeNavigationSelectItem selectItem = stackItem.Item1.NavigationItems[stackItem.Item2];
                    stack.Push(new ValueTuple<OeNavigationSelectItem, int>(selectItem, 0));
                }
            }
            while (stack.Count > 0);
            return navigationItems;
        }
        private static OePropertyAccessor[] GetAccessors(Type clrEntityType, IEdmEntitySetBase entitySet, IReadOnlyList<OeStructuralSelectItem> selectItems)
        {
            if (selectItems.Count == 0)
                return OePropertyAccessor.CreateFromType(clrEntityType, entitySet);

            var accessors = new OePropertyAccessor[selectItems.Count];

            ParameterExpression parameter = Expression.Parameter(typeof(Object));
            UnaryExpression typedAccessorParameter = Expression.Convert(parameter, clrEntityType);
            IReadOnlyList<MemberExpression> propertyExpressions = OeExpressionHelper.GetPropertyExpressions(typedAccessorParameter);
            for (int i = 0; i < selectItems.Count; i++)
                accessors[i] = OePropertyAccessor.CreatePropertyAccessor(selectItems[i].EdmProperty, propertyExpressions[i], parameter, selectItems[i].SkipToken);

            return accessors;
        }
        private Expression GetInnerSource(OeNavigationSelectItem navigationItem)
        {
            Type clrEntityType = _edmModel.GetClrType(navigationItem.EdmProperty.DeclaringType);
            PropertyInfo navigationClrProperty = clrEntityType.GetPropertyIgnoreCase(navigationItem.EdmProperty);

            Type itemType = OeExpressionHelper.GetCollectionItemType(navigationClrProperty.PropertyType);
            if (itemType == null)
                itemType = navigationClrProperty.PropertyType;

            var visitor = new OeQueryNodeVisitor(_joinBuilder.Visitor, Expression.Parameter(itemType));
            var expressionBuilder = new OeExpressionBuilder(_joinBuilder, visitor);

            IEdmNavigationProperty navigationProperty = navigationItem.EdmProperty;
            if (navigationItem.EdmProperty.ContainsTarget)
            {
                ModelBuilder.ManyToManyJoinDescription joinDescription = _edmModel.GetManyToManyJoinDescription(navigationProperty);
                navigationProperty = joinDescription.TargetNavigationProperty;
            }
            IEdmEntitySet innerEntitySet = OeEdmClrHelper.GetEntitySet(_edmModel, navigationProperty);
            Expression innerSource = OeEnumerableStub.CreateEnumerableStubExpression(itemType, innerEntitySet);

            ExpandedNavigationSelectItem item = navigationItem.NavigationSelectItem;
            innerSource = expressionBuilder.ApplyFilter(innerSource, item.FilterOption);

            long? top = GetTop(navigationItem, item.TopOption);
            if (top == null && item.SkipOption == null)
                return innerSource;

            OrderByClause orderByClause = item.OrderByOption;
            if (navigationItem.PageSize > 0)
                orderByClause = OeSkipTokenParser.GetUniqueOrderBy(navigationItem.EntitySet, item.OrderByOption, null);

            var entitySet = (IEdmEntitySet)navigationItem.Parent.EntitySet;
            Expression source = OeEnumerableStub.CreateEnumerableStubExpression(navigationClrProperty.DeclaringType, entitySet);
            return OeCrossApplyBuilder.Build(expressionBuilder, source, innerSource, navigationItem.Path, orderByClause, item.SkipOption, top);
        }
        private static OeEntryFactory[] GetNestedNavigationLinks(OeNavigationSelectItem navigationItem)
        {
            var nestedEntryFactories = new List<OeEntryFactory>(navigationItem.NavigationItems.Count);
            for (int i = 0; i < navigationItem.NavigationItems.Count; i++)
                if (!navigationItem.NavigationItems[i].SkipToken)
                    nestedEntryFactories.Add(navigationItem.NavigationItems[i].EntryFactory);
            return nestedEntryFactories.ToArray();
        }
        private long? GetTop(OeNavigationSelectItem navigationItem, long? top)
        {
            if (navigationItem.MaxTop > 0 && navigationItem.MaxTop < top.GetValueOrDefault())
                top = navigationItem.MaxTop;

            if (navigationItem.PageSize > 0 && (top == null || navigationItem.PageSize < top.GetValueOrDefault()))
                top = navigationItem.PageSize;

            return top;
        }
        private static bool HasSelectItems(SelectExpandClause selectExpandClause)
        {
            if (selectExpandClause != null)
                foreach (SelectItem odataSelectItem in selectExpandClause.SelectedItems)
                    if (odataSelectItem is PathSelectItem pathSelectItem && pathSelectItem.SelectedPath.LastSegment is PropertySegment)
                        return true;

            return false;
        }
        private static Expression SelectStructuralProperties(Expression source, OeNavigationSelectItem root)
        {
            if (root.NavigationItems.Count == 0)
                return source;

            ParameterExpression parameter = Expression.Parameter(OeExpressionHelper.GetCollectionItemType(source.Type));
            IReadOnlyList<MemberExpression> joins = OeExpressionHelper.GetPropertyExpressions(parameter);
            var newJoins = new Expression[joins.Count];

            List<OeNavigationSelectItem> navigationItems = FlattenNavigationItems(root, true);
            for (int i = 0; i < navigationItems.Count; i++)
            {
                newJoins[i] = joins[i];
                if (navigationItems[i].StructuralItems.Count > 0)
                {
                    var properties = new Expression[navigationItems[i].StructuralItems.Count];
                    for (int j = 0; j < navigationItems[i].StructuralItems.Count; j++)
                        if (navigationItems[i].StructuralItems[j].EdmProperty is ComputeProperty computeProperty)
                            properties[j] = new ReplaceParameterVisitor(joins[i]).Visit(computeProperty.Expression);
                        else
                        {
                            PropertyInfo property = joins[i].Type.GetPropertyIgnoreCase(navigationItems[i].StructuralItems[j].EdmProperty);
                            properties[j] = Expression.Property(joins[i], property);
                        }
                    Expression newTupleExpression = OeExpressionHelper.CreateTupleExpression(properties);

                    if (i > 0 && navigationItems[i].EdmProperty.Type.IsNullable)
                    {
                        UnaryExpression nullConstant = Expression.Convert(OeConstantToVariableVisitor.NullConstantExpression, newTupleExpression.Type);
                        newTupleExpression = Expression.Condition(Expression.Equal(joins[i], OeConstantToVariableVisitor.NullConstantExpression), nullConstant, newTupleExpression);
                    }
                    newJoins[i] = newTupleExpression;
                }
            }

            NewExpression newSelectorBody = OeExpressionHelper.CreateTupleExpression(newJoins);
            MethodInfo selectMethodInfo = OeMethodInfoHelper.GetSelectMethodInfo(parameter.Type, newSelectorBody.Type);
            LambdaExpression newSelector = Expression.Lambda(newSelectorBody, parameter);

            //Quirk EF Core 2.1.1 bug Take/Skip must be last in expression tree
            var skipTakeExpressions = new List<MethodCallExpression>();
            while (source is MethodCallExpression callExpression && (callExpression.Method.Name == nameof(Enumerable.Skip) || callExpression.Method.Name == nameof(Enumerable.Take)))
            {
                skipTakeExpressions.Add(callExpression);
                source = callExpression.Arguments[0];
            }

            source = Expression.Call(selectMethodInfo, source, newSelector);

            for (int i = skipTakeExpressions.Count - 1; i >= 0; i--)
            {
                MethodInfo skipTakeMethodInfo = skipTakeExpressions[i].Method.GetGenericMethodDefinition().MakeGenericMethod(newSelector.ReturnType);
                source = Expression.Call(skipTakeMethodInfo, source, skipTakeExpressions[i].Arguments[1]);
            }

            return source;
        }
    }
}
