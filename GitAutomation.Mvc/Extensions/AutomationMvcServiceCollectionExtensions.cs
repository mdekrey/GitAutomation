using GitAutomation.Mvc;
using GitAutomation.Plugins;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class AutomationMvcServiceCollectionExtensions
    {
        public static IServiceCollection AddMvcExtensions(this IServiceCollection services, IConfiguration mvcSection, IMvcBuilder mvcBuilder)
        {
            var mvcOptions = mvcSection.Get<MvcExtensionOptions>();
            foreach (var plugin in mvcOptions?.Types?.Select(PluginActivator.GetPluginOrNull<IMvcExtension>) ?? Enumerable.Empty<IMvcExtension>())
            {
                plugin?.RegisterAdditionalMvc(services, mvcSection, mvcBuilder);
            }
            return services;
        }
    }
}
