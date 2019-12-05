using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using GitAutomation.DomainModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GitAutomation.Scripting
{
    public class ScriptInvoker : IScriptInvoker
    {
        private readonly IServiceProvider serviceProvider;
        private readonly ILoggerFactory loggerFactory;

        public ScriptInvoker(IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
        {
            this.serviceProvider = serviceProvider;
            this.loggerFactory = loggerFactory;
        }

        public ScriptProgress Invoke<TParams>(Type scriptType, TParams loggedParameters, IAgentSpecification agentSpecification)
        {
            var script = ActivatorUtilities.GetServiceOrCreateInstance(serviceProvider, scriptType) as IScript<TParams>;
            if (script == null)
            {
                throw new ArgumentException($"Provided script type {scriptType.FullName} cannot take params of type {typeof(TParams).FullName}", paramName: nameof(scriptType));
            }

            var logger = new WrappedLogger(loggerFactory.CreateLogger(scriptType));
            var completion = script.Run(loggedParameters, logger, agentSpecification);
            return new ScriptProgress(completion, logger.Logs, inputs: loggedParameters);
        }

        private class WrappedLogger : ILogger
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
}
