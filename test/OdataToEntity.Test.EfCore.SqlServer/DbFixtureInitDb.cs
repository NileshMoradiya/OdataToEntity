﻿using System.Threading.Tasks;
using System;
using System.IO;
using System.Threading;

namespace OdataToEntity.Test
{
    public class DbFixtureInitDb : DbFixture, IDisposable
    {
        private bool _initialized;
        private int _queryCount;

        public DbFixtureInitDb()
        {
        }

        public override void Initalize()
        {
            if (_initialized)
                return;

            _initialized = true;
            var parser = new OeParser(new Uri("http://dummy/"), base.OeDataAdapter, base.EdmModel);
            parser.ExecuteOperationAsync(base.ParseUri("ResetDb"), OeRequestHeaders.JsonDefault, null, new MemoryStream(), CancellationToken.None).GetAwaiter().GetResult();
            base.ExecuteBatchAsync("Add").GetAwaiter().GetResult();
        }

        public override async Task Execute<T, TResult>(QueryParameters<T, TResult> parameters)
        {
            _queryCount++;
            Task t1 = base.Execute(parameters);
            //Task t2 = base.Execute(parameters);
            await Task.WhenAll(t1, Task.CompletedTask);
        }
        public override async Task Execute<T, TResult>(QueryParametersScalar<T, TResult> parameters)
        {
            _queryCount++;
            Task t1 = base.Execute(parameters);
            Task t2 = base.Execute(parameters);
            await Task.WhenAll(t1, t2);
        }

        public void Dispose()
        {
            if (base.OeDataAdapter.QueryCache.AllowCache)
                Xunit.Assert.InRange(_queryCount, _queryCount, base.OeDataAdapter.QueryCache.CacheCount);
        }
    }

    public class ManyColumnsFixtureInitDb : DbFixture, IDisposable
    {
        private bool _initialized;
        private int _queryCount;

        public ManyColumnsFixtureInitDb()
        {
        }

        public override void Initalize()
        {
            if (_initialized)
                return;

            _initialized = true;
            var parser = new OeParser(new Uri("http://dummy/"), base.OeDataAdapter, base.EdmModel);
            parser.ExecuteOperationAsync(base.ParseUri("ResetManyColumns"), OeRequestHeaders.JsonDefault, null, new MemoryStream(), CancellationToken.None).GetAwaiter().GetResult();
            base.ExecuteBatchAsync("ManyColumns").GetAwaiter().GetResult();
        }

        public override async Task Execute<T, TResult>(QueryParameters<T, TResult> parameters)
        {
            _queryCount++;
            Task t1 = base.Execute(parameters);
            Task t2 = base.Execute(parameters);
            await Task.WhenAll(t1, t2);
        }
        public override async Task Execute<T, TResult>(QueryParametersScalar<T, TResult> parameters)
        {
            _queryCount++;
            Task t1 = base.Execute(parameters);
            Task t2 = base.Execute(parameters);
            await Task.WhenAll(t1, t2);
        }

        public void Dispose()
        {
            if (base.OeDataAdapter.QueryCache.AllowCache)
                Xunit.Assert.InRange(_queryCount, _queryCount, base.OeDataAdapter.QueryCache.CacheCount);
        }
    }
}
