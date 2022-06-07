using System;
using System.Collections.Generic;
using System.Text;

namespace Flagsmith
{
    public class FlagsmithClientError : Exception
    {
        public FlagsmithClientError(string message) : base(message) { }
    }
    public class FlagsmithAPIError : FlagsmithClientError
    {
        public FlagsmithAPIError(string message) : base(message) { }
    }
}
