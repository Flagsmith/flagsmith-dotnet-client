using System;

namespace FlagsmithEngine.Exceptions
{
    public class DuplicateFeatureState : Exception
    {
    }
    public class InvalidPercentageAllocation : Exception
    {
        public InvalidPercentageAllocation(string message) : base(message) { }
    }
}
