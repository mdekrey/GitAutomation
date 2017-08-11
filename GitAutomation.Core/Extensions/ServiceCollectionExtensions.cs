using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddGitUtilities(this IServiceCollection services)
        {
            services.AddSingleton<GitAutomation.Processes.IReactiveProcessFactory, GitAutomation.Processes.ReactiveProcessFactory>();
            services.AddSingleton<GitAutomation.Repository.IRepositoryState, GitAutomation.Repository.RepositoryState>();
            services.AddSingleton<GitAutomation.Repository.GitCli, GitAutomation.Repository.GitCli>();
            return services;
        }
    }
}
