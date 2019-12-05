using System;
using Microsoft.Extensions.Logging;

namespace GitAutomation.Scripting
{
    public class LogMessage
    {
        public readonly LogLevel LogLevel;
        public readonly EventId EventId;
        public readonly object? State;
        public readonly Exception Exception;
        public readonly string Formatted;

        public LogMessage(LogLevel logLevel, EventId eventId, object? state, Exception exception, string formatted)
        {
            this.LogLevel = logLevel;
            this.EventId = eventId;
            this.State = state;
            this.Exception = exception;
            this.Formatted = formatted;
        }
    }
}