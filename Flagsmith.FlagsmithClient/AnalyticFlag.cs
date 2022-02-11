using FlagsmithEngine.Feature.Models;
using System.Collections.Generic;
using System.Linq;
using System;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace Flagsmith
{
    public class AnalyticFlag : Flag
    {
        [JsonIgnore]
        private readonly AnalyticsProcessor _AnalyticsProcessor;
        public AnalyticFlag(AnalyticsProcessor analyticsProcessor)
        {
            _AnalyticsProcessor = analyticsProcessor ?? throw new ArgumentNullException(nameof(analyticsProcessor));
        }
        public static AnalyticFlag FromFeatureStateModel(AnalyticsProcessor analyticsProcessor, FeatureStateModel featureStateModel, string identityId = null) =>
           new AnalyticFlag(analyticsProcessor)
           {
               Feature = new Feature(featureStateModel.Feature.Id, featureStateModel.Feature.Name),
               Enabled = featureStateModel.Enabled,
               Value = featureStateModel.GetValue(identityId)?.ToString(),
           };

        public static List<AnalyticFlag> FromFeatureStateModel(AnalyticsProcessor analyticsProcessor, List<FeatureStateModel> featureStateModels, string identityId = null)
        => featureStateModels.Select(f => FromFeatureStateModel(analyticsProcessor, f, identityId)).ToList();
        public static List<AnalyticFlag> FromApiFlag(AnalyticsProcessor analyticsProcessor, List<Flag> flags)
        => flags.Select(flag => ToAnalyticFlag(analyticsProcessor, flag)).ToList();
        private static AnalyticFlag ToAnalyticFlag(AnalyticsProcessor analyticsProcessor, Flag flag)
          => new AnalyticFlag(analyticsProcessor)
          {
              Enabled = flag.IsEnabled(),
              Value = flag.GetValue(),
              Feature = flag.GetFeature()
          };

        public override string GetValue()
        {
            _ = _AnalyticsProcessor.TrackFeature(FeatureId);
            return Value;
        }
    }
}
