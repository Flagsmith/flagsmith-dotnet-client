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
        private readonly List<Flag> _flags;
        private readonly IAnalyticsCollector _analytics;
        private readonly Func<string, IFlag> _defaultFlagHandler;

        IReadOnlyCollection<IFlag> IFlags.Flags => _flags;

        public Flags(List<Flag> flags, IAnalyticsCollector analytics, Func<string, IFlag> defaultFlagHandler)
        {
            _flags = flags;
            _analytics = analytics;
            _defaultFlagHandler = defaultFlagHandler;
        }

        public string GetFeatureValue(string featureName) => GetFlag(featureName).Value;

        public bool IsFeatureEnabled(string featureName) => GetFlag(featureName).Enabled;

        public IFlag GetFlag(string featureName)
        {
            var flag = _flags?.FirstOrDefault(f => f.Feature.Name == featureName);
            if (flag == null)
                return _defaultFlagHandler?.Invoke(featureName) ?? throw new FlagsmithClientError("Feature does not exist: " + featureName);

            _analytics.TrackFeature(flag.Feature.Name);
            return flag;
        }

        public Task<IReadOnlyCollection<IFlag>> AllFlags() => Task.FromResult<IReadOnlyCollection<IFlag>>(_flags);

        private static Flag FromFeatureStateModel(FeatureStateModel featureStateModel, string identityId = null) =>
            new Flag(new Feature(featureStateModel.Feature.Name, featureStateModel.Feature.Id), featureStateModel.Enabled, featureStateModel.GetValue(identityId)?.ToString());

        public static Flags FromFeatureStateModel(IAnalyticsCollector analytics, Func<string, IFlag> defaultFlagHandler, List<FeatureStateModel> featureStateModels, string identityId = null)
        {
            var flags = featureStateModels.Select(f => FromFeatureStateModel(f, identityId)).ToList();
            return new Flags(flags, analytics, defaultFlagHandler);
        }
    }
}
