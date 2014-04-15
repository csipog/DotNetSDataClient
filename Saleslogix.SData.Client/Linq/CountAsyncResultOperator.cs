﻿// Copyright (c) 1997-2013, SalesLogix NA, LLC. All rights reserved.

using System.Linq;
using System.Threading;
using Remotion.Linq.Clauses;
using Remotion.Linq.Clauses.ResultOperators;
using Remotion.Linq.Clauses.StreamedData;
using Remotion.Linq.Utilities;

namespace Saleslogix.SData.Client.Linq
{
    internal class CountAsyncResultOperator : CountResultOperator
    {
        private readonly CancellationToken _cancel;

        public CountAsyncResultOperator(CancellationToken cancel)
        {
            _cancel = cancel;
        }

        public override ResultOperatorBase Clone(CloneContext cloneContext)
        {
            return new CountAsyncResultOperator(_cancel);
        }

        public override StreamedValue ExecuteInMemory<T>(StreamedSequence input)
        {
            var sequence = input.GetTypedSequence<T>();
            var result = sequence.Count();
            return new StreamedValue(result, (StreamedValueInfo) base.GetOutputDataInfo(input.DataInfo));
        }

        public override IStreamedDataInfo GetOutputDataInfo(IStreamedDataInfo inputInfo)
        {
            ArgumentUtility.CheckNotNullAndType<StreamedSequenceInfo>("inputInfo", inputInfo);
            return new StreamedAsyncScalarInfo(typeof (int), _cancel);
        }

        public override string ToString()
        {
            return "CountAsync()";
        }
    }
}