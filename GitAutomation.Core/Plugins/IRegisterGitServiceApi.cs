using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace GitAutomation.Plugins
{
    public interface IRegisterGitServiceApi
    {
        void RegisterGitServiceApi(IServiceCollection services, IConfiguration configuration);
    }
}
