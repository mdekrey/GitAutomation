using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitAutomation.Repository
{
    class IntegrationNamingMediator : IIntegrationNamingMediator
    {
        private readonly IIntegrationNamingConvention convention;
        private readonly IRemoteRepositoryState repository;

        public IntegrationNamingMediator(IIntegrationNamingConvention convention, IRemoteRepositoryState repository)
        {
            this.convention = convention;
            this.repository = repository;
        }

        public async Task<string> GetIntegrationBranchName(string branchA, string branchB)
        {
            var candidateNames = convention.GetIngtegrationBranchNameCandidates(branchA, branchB);
            var remoteBranches = repository.RemoteBranches().Take(1);
            return await candidateNames.CombineLatest(remoteBranches, (name, branches) => new { name, branches })
                .Where(target => !target.branches.Select(b => b.Name).Contains(target.name))
                .Select(target => target.name)
                .FirstOrDefaultAsync()
                ?? $"integrate/{Guid.NewGuid().ToString()}";
        }
    }
}
