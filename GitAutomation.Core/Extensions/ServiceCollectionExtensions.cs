using GitAutomation.BranchSettings;
using GitAutomation.GitService;
using GitAutomation.Repository;
using GitAutomation.SqlServer;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Net.Http;
using System.Text;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddGitUtilities(this IServiceCollection services, PersistenceOptions persistenceOptions, GitRepositoryOptions repositoryOptions)
        {
            services.AddSingleton<GitAutomation.Processes.IReactiveProcessFactory, GitAutomation.Processes.ReactiveProcessFactory>();
            services.AddSingleton<IRepositoryState, RepositoryState>();
            services.AddSingleton<GitCli>();
            services.AddSingleton<Func<HttpClient>>(() => new HttpClient());

            services.AddSingleton<GitAutomation.Work.IUnitOfWorkFactory, GitAutomation.Work.UnitOfWorkFactory>();

            // TODO - should have some sort of registry for persistence types
            services.AddSingleton<IBranchSettingsNotifiers, BranchSettingsNotifiers>();
            if (persistenceOptions.Type == "SqlServer")
            {
                services.AddSingleton<IBranchSettings, SqlBranchSettings>();
                services.AddScoped(serviceProvider =>
                {
                    var result = new ConnectionManagement(persistenceOptions.Connectionstring);
                    return result;
                });
            }
            else
            {
                throw new NotSupportedException($"Unknown persistence type: {persistenceOptions.Type}. Supported options: SqlServer");
            }

            // TODO - should have some sort of registry for Repository API types
            if (repositoryOptions.ApiType == "GitHub")
            {
                services.AddSingleton<IGitServiceApi, GitHubServiceApi>();
            }
            else
            {
                throw new NotSupportedException($"Unknown repository api type: {repositoryOptions.ApiType}. Supported options: SqlServer");
            }

            return services;
        }
    }
}
