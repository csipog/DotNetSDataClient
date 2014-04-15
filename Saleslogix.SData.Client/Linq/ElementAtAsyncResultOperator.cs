﻿// Copyright (c) 1997-2013, SalesLogix NA, LLC. All rights reserved.

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using Remotion.Linq.Clauses;
using Remotion.Linq.Clauses.StreamedData;
using Remotion.Linq.Utilities;

namespace Saleslogix.SData.Client.Linq
{
    internal class ElementAtAsyncResultOperator : ElementAtResultOperator
    {
        private readonly CancellationToken _cancel;

        public ElementAtAsyncResultOperator(int index, bool returnDefaultWhenEmpty, CancellationToken cancel)
            : base(index, returnDefaultWhenEmpty)
        {
            _cancel = cancel;
        }

        public override StreamedValue ExecuteInMemory<T>(StreamedSequence input)
        {
            var sequence = input.GetTypedSequence<T>();
            var result = ReturnDefaultWhenEmpty ? sequence.ElementAtOrDefault(Index) : sequence.ElementAt(Index);
            return new StreamedValue(result, (StreamedValueInfo) base.GetOutputDataInfo(input.DataInfo));
        }

        public override IStreamedDataInfo GetOutputDataInfo(IStreamedDataInfo inputInfo)
        {
            var inputSequenceInfo = ArgumentUtility.CheckNotNullAndType<StreamedSequenceInfo>("inputInfo", inputInfo);
            return new StreamedAsyncSingleInfo(inputSequenceInfo.ResultItemType, _cancel);
        }

        public override ResultOperatorBase Clone(CloneContext cloneContext)
        {
            return new ElementAtAsyncResultOperator(Index, ReturnDefaultWhenEmpty, _cancel);
        }

        public override void TransformExpressions(Func<Expression, Expression> transformation)
        {
        }

        public override string ToString()
        {
            return string.Format(ReturnDefaultWhenEmpty ? "ElementAtOrDefaultAsync({0})" : "ElementAtAsync({0})", Index);
        }
    }
}