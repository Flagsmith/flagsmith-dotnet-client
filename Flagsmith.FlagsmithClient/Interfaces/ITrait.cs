using System;

namespace Flagsmith.Interfaces
{
    public interface ITrait
    {
        string Key { get; }
        dynamic Value { get; }

        [Obsolete("Use 'Key' property, method will be removed in next version.")]
        string GetTraitKey();

        [Obsolete("Use 'Value' property, method will be removed in next version.")]
        dynamic GetTraitValue();
    }
}
