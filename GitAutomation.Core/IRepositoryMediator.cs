using GitAutomation.BranchSettings;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading.Tasks;
using GitAutomation.Work;

namespace GitAutomation
{
    public interface IRepositoryMediator
    {
        IObservable<ImmutableList<BranchBasicDetails>> AllBranches();
        IObservable<ImmutableList<BranchHierarchyDetails>> AllBranchesHierarchy();
        IObservable<ImmutableList<string>> DetectShallowUpstream(string branchName);
        IObservable<string> LatestBranchName(BranchBasicDetails details);
        IObservable<string> GetNextCandidateBranch(BranchDetails details, bool shouldMutate);
        IObservable<BranchDetails> GetBranchDetails(string branchName);
        void ConsolidateBranches(IEnumerable<string> branchesToRemove, string targetBranch, IUnitOfWork unitOfWork);
    }
}
