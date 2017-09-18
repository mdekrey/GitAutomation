using GitAutomation.BranchSettings;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading.Tasks;

namespace GitAutomation
{
    public interface IRepositoryMediator
    {
        IObservable<ImmutableList<BranchBasicDetails>> AllBranches();
        IObservable<ImmutableList<BranchHierarchyDetails>> AllBranchesHierarchy();
        IObservable<ImmutableList<string>> DetectShallowUpstream(string branchName);
        IObservable<string> LatestBranchName(BranchDetails details);
        IObservable<string> GetNextCandidateBranch(BranchDetails details, bool shouldMutate);
        IObservable<BranchDetails> GetBranchDetails(string branchName);

        IObservable<ImmutableList<Repository.GitRef>> GetAllBranchRefs();
        IObservable<string> GetBranchRef(string branchName);
        IObservable<bool> HasOutstandingCommits(string upstreamBranch, string downstreamBranch);
    }
}
