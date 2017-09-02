using GitAutomation.Plugins;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace GitAutomation.GitService
{
    public class RegisterMemory : IRegisterGitServiceApi
    {
        public void RegisterGitServiceApi(IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<IGitServiceApi, MemoryGitServiceApi>();
        }
    }
}
