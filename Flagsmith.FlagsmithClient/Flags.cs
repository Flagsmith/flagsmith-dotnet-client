using FlagsmithEngine.Feature.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flagsmith
{
    public class Flags
    {
        List<Flag> _Flags;
        private readonly AnalyticsProcessor _AnalyticsProcessor;
        private Func<string, Flag> _DefaultFlagHandler;

        public Flags(List<Flag> flags, AnalyticsProcessor analyticsProcessor, Func<string, Flag> defaultFlagHandler)
        {
            _Flags = flags;
            _AnalyticsProcessor = analyticsProcessor;
            _DefaultFlagHandler = defaultFlagHandler;
        }
        public async Task<string> GetFeatureValue(string featureName) => (await GetFeatureFlag(featureName)).GetValue();
        public async Task<bool> IsFeatureEnabled(string featureName) => (await GetFeatureFlag(featureName)).IsEnabled();
        public async Task<Flag> GetFeatureFlag(string featureName)
        {
            var flag = _Flags?.FirstOrDefault(f => f.GetFeature().GetName().Equals(featureName));
            if (flag == null)
            {
                return _DefaultFlagHandler?.Invoke(featureName) ?? throw new FlagsmithClientError("Feature does not exist: " + featureName);

            }
            if (_AnalyticsProcessor != null)
                await _AnalyticsProcessor.TrackFeature(flag.GetFeature().GetId());
            return flag;
        }
        public List<Flag> AllFlags() => _Flags;
        private static Flag FromFeatureStateModel(FeatureStateModel featureStateModel, string identityId = null) =>
         new Flag(new Feature(featureStateModel.Feature.Id, featureStateModel.Feature.Name),
             featureStateModel.Enabled,
             featureStateModel.GetValue(identityId)?.ToString());
        public static Flags FromFeatureStateModel(AnalyticsProcessor analyticsProcessor, Func<string, Flag> defaultFlagHandler, List<FeatureStateModel> featureStateModels, string identityId = null)
        {
            var flags = featureStateModels.Select(f => FromFeatureStateModel(f, identityId)).ToList();
            return new Flags(flags, analyticsProcessor, defaultFlagHandler);
        }
        public static Flags FromApiFlag(AnalyticsProcessor analyticsProcessor, Func<string, Flag> defaultFlagHandler, List<Flag> flags)
        => new Flags(flags, analyticsProcessor, defaultFlagHandler);

    }
}
