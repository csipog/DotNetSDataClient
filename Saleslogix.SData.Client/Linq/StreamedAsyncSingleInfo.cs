// Copyright (c) 1997-2014, SalesLogix NA, LLC. All rights reserved.

using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Remotion.Linq;
using Remotion.Linq.Clauses.StreamedData;
using Remotion.Linq.Utilities;

namespace Saleslogix.SData.Client.Linq
{
    internal class StreamedAsyncSingleInfo : StreamedValueInfo
    {
        private static readonly MethodInfo _executeMethod = new Func<QueryModel, IAsyncQueryExecutor, CancellationToken, object>(ExecuteSingleQueryModel<object>).GetMethodInfo().GetGenericMethodDefinition();

        private readonly Type _resultType;
        private readonly CancellationToken _cancel;

        public StreamedAsyncSingleInfo(Type resultType, CancellationToken cancel)
            : base(typeof (Task<>).MakeGenericType(resultType))
        {
            _resultType = resultType;
            _cancel = cancel;
        }

        public override IStreamedData ExecuteQueryModel(QueryModel queryModel, IQueryExecutor executor)
        {
            ArgumentUtility.CheckNotNull("queryModel", queryModel);
            var asyncExecutor = ArgumentUtility.CheckNotNullAndType<IAsyncQueryExecutor>("executor", executor);
            var executeMethod = _executeMethod.MakeGenericMethod(_resultType);
            var result = executeMethod.Invoke(null, new object[] {queryModel, asyncExecutor, _cancel});
            return new StreamedValue(result, this);
        }

        protected override StreamedValueInfo CloneWithNewDataType(Type dataType)
        {
            ArgumentUtility.CheckNotNull("dataType", dataType);
            if (dataType != DataType)
            {
                throw new NotSupportedException("Cloning StreamedAsyncSingleInfo with a different data type not supported");
            }
            return new StreamedAsyncSingleInfo(_resultType, _cancel);
        }

        private static object ExecuteSingleQueryModel<T>(QueryModel queryModel, IAsyncQueryExecutor executor, CancellationToken cancel)
        {
            ArgumentUtility.CheckNotNull("queryModel", queryModel);
            ArgumentUtility.CheckNotNull("executor", executor);
            return executor.ExecuteSingleAsync<T>(queryModel, cancel);
        }
    }
}