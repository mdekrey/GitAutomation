using GitAutomation.Plugins;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using GitAutomation.BranchSettings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using GitAutomation.Auth;
using GitAutomation.EFCore.SecurityModel;

namespace GitAutomation.Postgres
{
    public class RegisterPostgres : IRegisterBranchSettings, IRegisterPrincipalValidation
    {
        public void RegisterBranchSettings(IServiceCollection services, IConfiguration configuration)
        {
            RegisterCommon(services, configuration.GetSection("postgres"));
            services.AddEfBranchingContext<PostgresBranchingContextCustomization>();
        }

        public void RegisterPrincipalValidation(IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<EfPermissionManagementOptions>(configuration);
            // This assumes that the branch settings are already registered
            // TODO - some cleverness to use the common section in both
            services.AddEfSecurityContext<PostgresSecurityContextCustomization>();
        }

        private void RegisterCommon(IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions();
            services.Configure<PostgresOptions>(configuration);
        }
    }
}
