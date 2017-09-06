using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitAutomation.Repository
{
    public static class IntegrationNamingExtension
    {
        public static void AddIntegrationNamingConvention(this IServiceCollection services, GitRepositoryOptions options)
        {
            var type = Plugins.PluginActivator.GetPluginTypeOrNull(options.IntegrationNamingConventionType);
            services.AddTransient(typeof(IIntegrationNamingConvention), type ?? typeof(StandardIntegrationNamingConvention));
            services.AddTransient<IIntegrationNamingMediator, IntegrationNamingMediator>();
        }
    }
}
