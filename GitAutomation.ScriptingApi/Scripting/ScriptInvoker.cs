using System;
using System.Linq;
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
            var type = Type.GetType(scriptName, GetLoadedAssemblyByName, null);
            if (type == null)
            {
                throw new ArgumentException($"Type ({scriptName}) was not found in loaded assemblies.");
            }
            if (!typeof(IScript<TParams>).IsAssignableFrom(type) || !type.IsClass || type.IsAbstract)
            {
                throw new ArgumentException($"Type ({type.AssemblyQualifiedName}) must be concrete and implement the interface '{typeof(IScript<TParams>).FullName}'.");
            }
            return type;
        }

        private static System.Reflection.Assembly GetLoadedAssemblyByName(System.Reflection.AssemblyName asmName)
        {
            return AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(asm => asm.FullName == asmName.FullName) 
                ?? AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(asm => asm.GetName().Name == asmName.Name);
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
