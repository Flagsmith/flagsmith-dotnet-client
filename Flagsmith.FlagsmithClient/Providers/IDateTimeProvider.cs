using System;

namespace Flagsmith.Providers
{
    public interface IDateTimeProvider
    {
        DateTime Now();
    }
}