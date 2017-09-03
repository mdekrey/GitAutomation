using GitAutomation.BranchSettings;
using GitAutomation.GitService;
using GitAutomation.Orchestration;
using GitAutomation.Plugins;
using GitAutomation.Repository;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Net.Http;
using System.Text;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddGitUtilities(this IServiceCollection services, IConfiguration persistenceConfiguration, IConfiguration repositoryConfiguration)
        {
            services.AddSingleton<GitAutomation.Processes.IReactiveProcessFactory, GitAutomation.Processes.ReactiveProcessFactory>();
            services.AddSingleton<IRepositoryOrchestration, RepositoryOrchestration>();
            services.AddSingleton<IOrchestrationActions, OrchestrationActions>();
            services.AddSingleton<IRepositoryStateDriver, RepositoryStateDriver>();
            services.AddSingleton<IRepositoryState, RepositoryState>();
            services.AddSingleton<GitCli>();
            services.AddSingleton<Func<HttpClient>>(() => new HttpClient());

            services.AddSingleton<GitAutomation.Work.IUnitOfWorkFactory, GitAutomation.Work.UnitOfWorkFactory>();

            services.AddSingleton<IBranchSettingsNotifiers, BranchSettingsNotifiers>();

            var persistenceOptions = persistenceConfiguration.Get<PersistenceOptions>();
            PluginActivator.GetPlugin<IRegisterBranchSettings>(
                typeName: persistenceOptions.Type,
                errorMessage: $"Unknown persistence registry: {persistenceOptions.Type}. Specify a .Net type."
            ).RegisterBranchSettings(services, persistenceConfiguration);
            
            var repositoryOptions = repositoryConfiguration.Get<GitRepositoryOptions>();
            PluginActivator.GetPlugin<IRegisterGitServiceApi>(
                typeName: repositoryOptions.ApiType,
                errorMessage: $"Unknown git service api registry: {repositoryOptions.ApiType}. Specify a .Net type, such as `{typeof(RegisterMemory).FullName}`"
            ).RegisterGitServiceApi(services, repositoryConfiguration);

            return services;
        }
    }
}
