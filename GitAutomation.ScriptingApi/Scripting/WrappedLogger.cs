using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace GitAutomation.Scripting
{
    class WrappedLogger : ILogger
    {
        private ILogger logger;

        private readonly ConcurrentQueue<LogMessage> logs = new ConcurrentQueue<LogMessage>();
        public IEnumerable<LogMessage> Logs => logs;

        public WrappedLogger(ILogger logger)
        {
            this.logger = logger;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return logger.BeginScope(state);
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logger.IsEnabled(logLevel);
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (IsEnabled(logLevel))
            {
                logs.Enqueue(new LogMessage(logLevel, eventId, state, exception, formatter(state, exception)));
            }
            logger.Log(logLevel, eventId, state, exception, formatter);
        }
    }
}
