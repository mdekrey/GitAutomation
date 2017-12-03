using GitAutomation;
using GitAutomation.BranchSettings;
using GitAutomation.GitService;
using GitAutomation.Orchestration;
using GitAutomation.Orchestration.Actions.MergeStrategies;
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
    public static class AutomationCoreServiceCollectionExtensions
    {
        public static IServiceCollection AddGitUtilities(this IServiceCollection services, IConfiguration persistenceConfiguration, IConfiguration repositoryConfiguration, IConfiguration appConfiguration)
        {
            services.AddSingleton<IRepositoryMediator, RepositoryMediator>();
            services.AddSingleton<GitAutomation.Processes.IReactiveProcessFactory, GitAutomation.Processes.ReactiveProcessFactory>();
            services.AddSingleton<IRepositoryOrchestration, RepositoryOrchestration>();
            services.AddSingleton<IOrchestrationActions, OrchestrationActions>();
            services.AddSingleton<IRepositoryStateDriver, RepositoryStateDriver>();
            services.AddSingleton<IRemoteRepositoryState, RemoteRepositoryState>();
            services.AddSingleton<ILocalRepositoryState, LocalRepositoryState>();
            services.AddSingleton<IGitCli>(sp => sp.GetRequiredService<ILocalRepositoryState>().Cli);
            services.AddSingleton<Func<HttpClient>>(() => new HttpClient());
            services.AddTransient<GitAutomation.Orchestration.Actions.IntegrateBranchesOrchestration>();

            services.AddSingleton<GitAutomation.Work.IUnitOfWorkFactory, GitAutomation.Work.UnitOfWorkFactory>();

            services.AddSingleton<IBranchSettingsNotifiers, BranchSettingsNotifiers>();

            services.AddSingleton(appConfiguration.Get<AppOptions>() ?? new AppOptions());

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

            services.AddIntegrationNamingConvention(repositoryOptions);

            services.AddTransient<NormalMergeStrategy>();
            services.AddTransient<MergeNextIterationMergeStrategy>();
            services.AddTransient<ForceFreshMergeStrategy>();
            services.AddTransient<IMergeStrategyManager, MergeStrategyManager>();

            return services;
        }
    }
}
