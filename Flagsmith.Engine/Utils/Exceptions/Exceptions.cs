using System;
using System.Collections.Generic;
using System.Text;

namespace FlagsmithEngine.Exceptions
{
    public class FeatureStateNotFound : Exception
    {
        //Overriding the Message property
        public override string Message
        {
            get
            {
                return "Feature State not found.";
            }
        }
    }
    public class DuplicateFeatureState : Exception
    {
    }
    public class InvalidPercentageAllocation : Exception
    {
        public InvalidPercentageAllocation(string message) : base(message) { }
    }
}
