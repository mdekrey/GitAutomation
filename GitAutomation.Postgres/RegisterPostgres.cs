using GitAutomation.Plugins;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using GitAutomation.BranchSettings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using GitAutomation.Auth;

namespace GitAutomation.Postgres
{
    public class RegisterPostgres : IRegisterBranchSettings, IRegisterPrincipalValidation
    {
        public void RegisterBranchSettings(IServiceCollection services, IConfiguration configuration)
        {
            RegisterCommon(services, configuration.GetSection("postgres"));
            services.AddSingleton<IBranchSettings, PostgresBranchSettings>();
        }

        public void RegisterPrincipalValidation(IServiceCollection services, IConfiguration configuration)
        {
            // This assumes that the branch settings are already registered
            // TODO - some cleverness to use the common section in both
            services.AddEfSecurityContext<PostgresSecurityContextCustomization>();
        }

        private void RegisterCommon(IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions();
            services.Configure<PostgresOptions>(configuration);
            services.AddScoped<ConnectionManagement>();
            services.AddSingleton<ContextFactory>();
        }
    }
}
