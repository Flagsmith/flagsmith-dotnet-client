using FlagsmithEngine.Environment.Models;
using FlagsmithEngine.Feature.Models;
using FlagsmithEngine.Identity.Models;
using FlagsmithEngine.Trait.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace FlagsmithEngine.Interfaces
{
    public interface IEngine
    {
        EvaluationResult<SegmentMetadataT, FeatureMetadataT> GetEvaluationResult<SegmentMetadataT, FeatureMetadataT>(EvaluationContext<SegmentMetadataT, FeatureMetadataT> context);
    }
}
