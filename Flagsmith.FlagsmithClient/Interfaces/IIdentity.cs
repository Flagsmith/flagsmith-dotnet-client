using System.Collections.Generic;

namespace Flagsmith.Interfaces
{
    public interface IIdentity
    {
        IReadOnlyCollection<IFlag> Flags { get; }
        IReadOnlyCollection<ITrait> Traits { get; }
    }
}
