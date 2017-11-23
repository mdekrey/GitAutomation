using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GitAutomation.Mvc
{
    public interface IMvcExtension
    {
        void RegisterAdditionalMvc(IServiceCollection services, IConfiguration mvcSection, IMvcBuilder mvcBuilder);
    }
}
