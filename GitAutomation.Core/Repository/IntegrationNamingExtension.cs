using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitAutomation.Repository
{
    public static class IntegrationNamingExtension
    {
        public static void AddIntegrationNamingConvention(this IServiceCollection services, GitRepositoryOptions options)
        {
            var type = Plugins.PluginActivator.GetPluginTypeOrNull(options.IntegrationNamingConventionType);
            services.AddTransient(typeof(IIntegrationNamingConvention), type ?? typeof(StandardIntegrationNamingConvention));
        }

        public static async Task<string> GetIntegrationBranchName(this IIntegrationNamingConvention convention, IRepositoryState repository, string branchA, string branchB)
        {
            var candidateNames = convention.GetIngtegrationBranchNameCandidates(branchA, branchB);
            var remoteBranches = repository.RemoteBranches().Take(1);
            return await candidateNames.CombineLatest(remoteBranches, (name, branches) => new { name, branches })
                .Where(target => !target.branches.Contains(target.name))
                .Select(target => target.name)
                .FirstOrDefaultAsync()
                ?? $"integrate/{Guid.NewGuid().ToString()}";
        }
    }
}
