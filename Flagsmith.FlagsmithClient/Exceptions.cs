using System;

namespace Flagsmith
{
    public class FlagsmithClientError : Exception
    {
        public FlagsmithClientError(string message)
            : base(message)
        {
        }

        public FlagsmithClientError(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

    public class FlagsmithAPIError : FlagsmithClientError
    {
        public FlagsmithAPIError(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
