using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace GitAutomation.Plugins
{
    public interface IRegisterBranchSettings
    {
        void RegisterBranchSettings(IServiceCollection services, IConfiguration configuration);
    }
}
