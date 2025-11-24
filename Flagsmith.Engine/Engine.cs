using System.Linq;
using System.Collections.Generic;
using FlagsmithEngine.Exceptions;
using FlagsmithEngine.Interfaces;
using FlagsmithEngine.Segment;
using FlagsmithEngine.Environment.Models;
using FlagsmithEngine.Feature.Models;
using FlagsmithEngine.Identity.Models;
using FlagsmithEngine.Trait.Models;
using System.Collections;
using System.Data;

namespace FlagsmithEngine
{
    public class Engine : IEngine
    {
        /// <summary>
        /// Get the evaluation result for a given context
        /// </summary>
        /// <typeparam name="SegmentMetadataT">Segment metadata type</typeparam>
        /// <typeparam name="FeatureMetadataT">Feature metadata type</typeparam>
        /// <param name="context"></param>
        /// <returns></returns>
        public EvaluationResult<SegmentMetadataT, FeatureMetadataT> GetEvaluationResult<SegmentMetadataT, FeatureMetadataT>(EvaluationContext<SegmentMetadataT, FeatureMetadataT> context)
        {
            context = GetEnrichedEvaluationContext(context);
            var result = new EvaluationResult<SegmentMetadataT, FeatureMetadataT>();
            var segmentEvaluationResult = ContextEvaluator.EvaluateSegments(context);
            result.Flags = ContextEvaluator.EvaluateFlags(context, segmentEvaluationResult.SegmentOverrides);
            result.Segments = segmentEvaluationResult.Segments;
            return result;
        }

        private EvaluationContext<SegmentMetadataT, FeatureMetadataT> GetEnrichedEvaluationContext<SegmentMetadataT, FeatureMetadataT>(EvaluationContext<SegmentMetadataT, FeatureMetadataT> context)
        {
            if (context.Identity != null)
            {
                if (string.IsNullOrEmpty(context.Identity.Key))
                {
                    context = context.Clone();
                    context.Identity.Key = context.Environment.Key + "_" + context.Identity.Identifier;
                }
            }
            return context;
        }
    }
}
