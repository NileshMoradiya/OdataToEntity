﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace OdataToEntity.Parsers
{
    public class OeEnumerableToQuerableVisitor : ExpressionVisitor
    {
        private static MethodInfo GetQuerableMethodInfo(MethodInfo enumerableMethodInfo, ReadOnlyCollection<Expression> arguments)
        {
            var enumerableTypes = new Type[arguments.Count - 1];
            for (int i = 1; i < arguments.Count; i++)
                if (arguments[i].Type.IsGenericType)
                    enumerableTypes[i - 1] = arguments[i].Type.GetGenericTypeDefinition();
                else
                    enumerableTypes[i - 1] = arguments[i].Type;

            foreach (MethodInfo methodInfo in typeof(Queryable).GetMethods())
            {
                if (methodInfo.Name == enumerableMethodInfo.Name)
                {
                    ParameterInfo[] querableParameters = methodInfo.GetParameters();
                    if (querableParameters.Length == arguments.Count)
                    {
                        bool matched = true;
                        for (int i = 1; i < querableParameters.Length && matched; i++)
                            if (querableParameters[i].ParameterType.IsGenericType)
                            {
                                if (querableParameters[i].ParameterType.IsSubclassOf(typeof(LambdaExpression)))
                                {
                                    Type lambdaType = querableParameters[i].ParameterType.GetGenericArguments()[0];
                                    matched = lambdaType.GetGenericTypeDefinition() == enumerableTypes[i - 1];
                                }
                                else
                                {
                                    Type queryableParameter = querableParameters[i].ParameterType.GetGenericTypeDefinition();
                                    if (queryableParameter == typeof(IEnumerable<>) && enumerableTypes[i - 1] == typeof(IQueryable<>))
                                        matched = true;
                                    else
                                        matched = queryableParameter == enumerableTypes[i - 1];
                                }
                            }
                            else
                                matched = querableParameters[i].ParameterType == enumerableTypes[i - 1];

                        if (matched)
                            if (methodInfo.ReturnType == enumerableMethodInfo.ReturnType ||
                                (methodInfo.ReturnType.IsGenericType && (
                                methodInfo.ReturnType.GetGenericTypeDefinition() == typeof(IQueryable<>)) ||
                                methodInfo.ReturnType.GetGenericTypeDefinition() == typeof(IOrderedQueryable<>)))
                                return methodInfo.GetGenericMethodDefinition().MakeGenericMethod(enumerableMethodInfo.GetGenericArguments());
                    }
                }
            }

            if (enumerableMethodInfo.Name == nameof(Enumerable.Min) ||
                enumerableMethodInfo.Name == nameof(Enumerable.Max))
                return enumerableMethodInfo;

            throw new InvalidOperationException("method " + enumerableMethodInfo.Name + " not found in Querable");
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            if (node.Value is OeEnumerableStub enumerableStub)
                return Expression.Constant(enumerableStub, typeof(IQueryable<>).MakeGenericType(enumerableStub.ElementType));
            return base.VisitConstant(node);
        }
        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            node = (Expression<T>)base.VisitLambda(node);
            if (node.ReturnType.IsGenericType && node.ReturnType.GetGenericTypeDefinition() == typeof(ICollection<>))
            {
                Expression body = Expression.Convert(node.Body, typeof(IEnumerable<>).MakeGenericType(node.ReturnType.GetGenericArguments()));
                return Expression.Lambda(body, node.Parameters);
            }
            return node;
        }
        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            ReadOnlyCollection<Expression> arguments = base.Visit(node.Arguments);
            if (node.Method.DeclaringType != typeof(Enumerable))
                return Expression.Call(base.Visit(node.Object), node.Method, arguments);

            Type genericTypeDefinition = arguments[0].Type.GetGenericTypeDefinition();
            if (genericTypeDefinition == typeof(IGrouping<,>) ||
                genericTypeDefinition == typeof(ICollection<>) ||
                genericTypeDefinition == typeof(IEnumerable<>) ||
                genericTypeDefinition == typeof(IOrderedEnumerable<>))
                return Expression.Call(base.Visit(node.Object), node.Method, arguments);

            MethodInfo querableMethodInfo = GetQuerableMethodInfo(node.Method, arguments);
            return Expression.Call(querableMethodInfo, arguments);
        }
    }
}
