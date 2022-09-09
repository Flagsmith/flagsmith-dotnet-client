using Microsoft.Extensions.Logging;
using System;

namespace ClientTest
{
    public sealed class FakeLogger<T> : ILogger<T>
    {
        public static readonly ILogger<T> Instance = new FakeLogger<T>();

        public IDisposable BeginScope<TState>(TState state)
        {
            throw new NotImplementedException();
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
        }
    }
}
