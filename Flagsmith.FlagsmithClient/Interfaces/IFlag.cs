using System;

namespace Flagsmith.Interfaces
{
    public interface IFlag
    {
        int Id { get; }
        bool Enabled { get; }
        string Value { get; }
        IFeature Feature { get; }

        [Obsolete("Use 'Feature.Id' property, method will be removed in next version.")]
        int getFeatureId();

        [Obsolete("Use 'Feature.Name' property, method will be removed in next version.")]
        string GetFeatureName();
    }
}
