using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GitAutomation.Mvc
{
    /// <summary>
    /// Extend this class without anything else to create an extension that will cause MVC to search your assembly for Controller classes.
    /// </summary>
    public abstract class ControllerAssemblyMvcExtension : IMvcExtension
    {
        public virtual void RegisterAdditionalMvc(IServiceCollection services, IConfiguration mvcSection, IMvcBuilder mvcBuilder)
        {
            mvcBuilder.AddApplicationPart(this.GetType().Assembly);
        }
    }
}
