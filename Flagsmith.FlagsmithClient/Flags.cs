using Flagsmith.Interfaces;
using FlagsmithEngine.Feature.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Flagsmith
{
    public class Flags : IFlags
    {
        private readonly List<Flag> _Flags;
        private readonly AnalyticsProcessor _AnalyticsProcessor;
        private readonly Func<string, IFlag> _DefaultFlagHandler;

        public Flags(List<Flag> flags, AnalyticsProcessor analyticsProcessor, Func<string, IFlag> defaultFlagHandler)
        {
            _Flags = flags;
            _AnalyticsProcessor = analyticsProcessor;
            _DefaultFlagHandler = defaultFlagHandler;
        }

        public async Task<string> GetFeatureValue(string featureName) => (await GetFlag(featureName)).Value;

        public async Task<bool> IsFeatureEnabled(string featureName) => (await GetFlag(featureName)).Enabled;

        public async Task<IFlag> GetFlag(string featureName)
        {
            var flag = _Flags?.FirstOrDefault(f => f.Feature.Name == featureName);
            if (flag == null)
            {
                return _DefaultFlagHandler?.Invoke(featureName) ?? throw new FlagsmithClientError("Feature does not exist: " + featureName);

            }
            if (_AnalyticsProcessor != null)
                await _AnalyticsProcessor.TrackFeature(flag.GetFeatureName());
            return flag;
        }

        public Task<IReadOnlyCollection<IFlag>> AllFlags() => Task.FromResult<IReadOnlyCollection<IFlag>>(_Flags);

        private static Flag FromFeatureStateModel(FeatureStateModel featureStateModel, string identityId = null) =>
            new Flag(new Feature(featureStateModel.Feature.Name, featureStateModel.Feature.Id), featureStateModel.Enabled, featureStateModel.GetValue(identityId)?.ToString());

        public static Flags FromFeatureStateModel(AnalyticsProcessor analyticsProcessor, Func<string, IFlag> defaultFlagHandler, List<FeatureStateModel> featureStateModels, string identityId = null)
        {
            var flags = featureStateModels.Select(f => FromFeatureStateModel(f, identityId)).ToList();
            return new Flags(flags, analyticsProcessor, defaultFlagHandler);
        }

        public static Flags FromApiFlag(AnalyticsProcessor analyticsProcessor, Func<string, IFlag> defaultFlagHandler, List<Flag> flags) =>
            new Flags(flags, analyticsProcessor, defaultFlagHandler);
    }
}
