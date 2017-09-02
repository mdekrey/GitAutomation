using GitAutomation.Plugins;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using GitAutomation.BranchSettings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace GitAutomation.SqlServer
{
    public class RegisterSqlServer : IRegisterBranchSettings
    {
        public void RegisterBranchSettings(IServiceCollection services, IConfiguration configuration)
        {
            RegisterCommon(services, configuration.GetSection("sqlServer"));
            services.AddSingleton<IBranchSettings, SqlBranchSettings>();
        }

        private void RegisterCommon(IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions();
            services.Configure<SqlServerOptions>(configuration);
            services.AddScoped<ConnectionManagement>();
        }
    }
}
