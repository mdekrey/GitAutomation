using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GitAutomation.Auth
{
    public class RegisterRolesFromConfiguration : IRegisterPrincipalValidation
    {
        public void RegisterPrincipalValidation(IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<RolesFromConfigurationOptions>(configuration);
            services.AddSingleton<IPrincipalValidation, RolesFromConfiguration>();
        }
    }
}
