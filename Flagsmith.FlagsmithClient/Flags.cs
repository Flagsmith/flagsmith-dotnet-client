using FlagsmithEngine.Feature.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flagsmith
{
    public class Flags : IFlags
    {
        List<IFlag> _Flags;
        private readonly AnalyticsProcessor _AnalyticsProcessor;
        private Func<string, IFlag> _DefaultFlagHandler;

        public Flags(List<IFlag> flags, AnalyticsProcessor analyticsProcessor, Func<string, IFlag> defaultFlagHandler)
        {
            _Flags = flags;
            _AnalyticsProcessor = analyticsProcessor;
            _DefaultFlagHandler = defaultFlagHandler;
        }
        public async Task<string> GetFeatureValue(string featureName) => (await GetFlag(featureName)).Value;
        public async Task<bool> IsFeatureEnabled(string featureName) => (await GetFlag(featureName)).Enabled;
        public async Task<IFlag> GetFlag(string featureName)
        {
            var flag = _Flags?.FirstOrDefault(f => f.GetFeatureName().Equals(featureName));
            if (flag == null)
            {
                return _DefaultFlagHandler?.Invoke(featureName) ?? throw new FlagsmithClientError("Feature does not exist: " + featureName);

            }
            if (_AnalyticsProcessor != null)
                await _AnalyticsProcessor.TrackFeature(flag.GetFeatureName());
            return flag;
        }
        public List<IFlag> AllFlags() => _Flags;
        private static IFlag FromFeatureStateModel(FeatureStateModel featureStateModel, string identityId = null) =>
            new Flag(new Feature(featureStateModel.Feature.Name, featureStateModel.Feature.Id), featureStateModel.Enabled, featureStateModel.GetValue(identityId)?.ToString(), featureStateModel.Feature.Id);

        public static IFlags FromFeatureStateModel(AnalyticsProcessor analyticsProcessor, Func<string, IFlag> defaultFlagHandler, List<FeatureStateModel> featureStateModels, string identityId = null)
        {
            var flags = featureStateModels.Select(f => FromFeatureStateModel(f, identityId)).ToList();
            return new Flags(flags, analyticsProcessor, defaultFlagHandler);
        }
        public static IFlags FromApiFlag(AnalyticsProcessor analyticsProcessor, Func<string, IFlag> defaultFlagHandler, List<IFlag> flags)
        => new Flags(flags, analyticsProcessor, defaultFlagHandler);

    }
}
