using System;
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

        public Type GetScript<TParams>(string scriptName)
        {
            if (scriptName.Contains(","))
            {
                // security, since we don't want to load arbitrary assemblies.
                throw new ArgumentException($"Cannot load generics or types from another assembly. Use the type's FullName property (assembly and class name). Got: {scriptName}");
            }
            var type = Type.GetType(scriptName);
            if (!typeof(IScript<TParams>).IsAssignableFrom(type) || !type.IsClass || type.IsAbstract)
            {
                throw new ArgumentException($"Type ({type.AssemblyQualifiedName}) must be concrete and implement the interface '{typeof(IScript<TParams>).FullName}'.");
            }
            return type;
        }

        public ScriptProgress Invoke<TParams>(Type scriptType, TParams loggedParameters, IAgentSpecification agentSpecification)
        {
            // TODO - scope the service provider?
            if (ActivatorUtilities.GetServiceOrCreateInstance(serviceProvider, scriptType) is IScript<TParams> script)
            {
                var logger = new WrappedLogger(loggerFactory.CreateLogger(scriptType));
                var completion = script.Run(loggedParameters, logger, agentSpecification);
                return new ScriptProgress(completion, logger.Logs, inputs: loggedParameters);
            }
            throw new ArgumentException($"Provided script type {scriptType.FullName} cannot take params of type {typeof(TParams).FullName}", paramName: nameof(scriptType));
        }


    }
}
