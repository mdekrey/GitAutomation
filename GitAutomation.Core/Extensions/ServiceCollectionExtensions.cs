using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddGitUtilities(this IServiceCollection services, GitAutomation.BranchSettings.PersistenceOptions persistenceOptions)
        {
            services.AddSingleton<GitAutomation.Processes.IReactiveProcessFactory, GitAutomation.Processes.ReactiveProcessFactory>();
            services.AddSingleton<GitAutomation.Repository.IRepositoryState, GitAutomation.Repository.RepositoryState>();
            services.AddSingleton<GitAutomation.Repository.GitCli>();

            services.AddSingleton<GitAutomation.Work.IUnitOfWorkFactory, GitAutomation.Work.UnitOfWorkFactory>();

            // TODO - should have some sort of registry for persistence types
            services.AddSingleton<GitAutomation.BranchSettings.IBranchSettingsNotifiers, GitAutomation.BranchSettings.BranchSettingsNotifiers>();
            if (persistenceOptions.Type == "SqlServer")
            {
                services.AddSingleton<GitAutomation.BranchSettings.IBranchSettings, GitAutomation.BranchSettings.SqlBranchSettings>();
                services.AddScoped<DbConnection>(serviceProvider =>
                {
                    var result = new System.Data.SqlClient.SqlConnection(persistenceOptions.Connectionstring);
                    return result;
                });
            }
            else
            {
                throw new NotSupportedException($"Unknown persistence type: {persistenceOptions.Type}. Supported options: SqlServer");
            }
            return services;
        }
    }
}
