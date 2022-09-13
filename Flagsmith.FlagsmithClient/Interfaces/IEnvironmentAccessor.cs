using FlagsmithEngine.Environment.Models;

namespace Flagsmith.Interfaces
{
    public interface IEnvironmentAccessor
    {
        EnvironmentModel GetEnvironmentModel();
    }
}
