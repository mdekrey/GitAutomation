using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GitAutomation.Repository
{
    class BranchIterationMediator : IBranchIterationMediator
    {
        private readonly IBranchIterationNamingConvention convention;
        private readonly IRemoteRepositoryState repository;

        public BranchIterationMediator(IBranchIterationNamingConvention convention, IRemoteRepositoryState repository)
        {
            this.convention = convention;
            this.repository = repository;
        }

        public string GetLatestBranchNameIteration(string branchName, IEnumerable<string> existingNames)
        {
            var temp = existingNames.Where(branch => IsBranchIteration(branchName, branch)).ToArray();
            return this.convention.GetLatestBranchNameIteration(branchName, temp);
        }

        public async Task<string> GetNextBranchNameIteration(string branchName, IEnumerable<string> existingNames)
        {
            var candidateNames = convention.GetBranchNameIterations(branchName, existingNames);
            var remoteBranches = repository.RemoteBranches().Take(1);
            return await candidateNames.CombineLatest(remoteBranches, (name, branches) => new { name, branches })
                .Where(target => !target.branches.Select(b => b.Name).Contains(target.name))
                .Select(target => target.name)
                .FirstOrDefaultAsync();
        }

        public string GuessBranchIterationRoot(string branchName)
        {
            return new Regex("-[1-9][0-9]*$").Replace(branchName, "");
        }

        public bool IsBranchIteration(string originalName, string candidateName)
        {
            return this.convention.IsBranchIteration(originalName, candidateName);
        }
    }
}
