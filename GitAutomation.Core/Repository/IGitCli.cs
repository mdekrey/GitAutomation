using System.Threading.Tasks;
using GitAutomation.Processes;

namespace GitAutomation.Repository
{
    public interface IGitCli
    {
        Task EnsureInitialized { get; }

        IReactiveProcess AnnotatedTag(string tagName, string message);
        IReactiveProcess Checkout(string branchName);
        IReactiveProcess CheckoutNew(string branchName);
        IReactiveProcess CheckoutRemote(string branchName);
        IReactiveProcess CheckRefFormat(string branchName);
        IReactiveProcess Clean();
        IReactiveProcess Clone();
        IReactiveProcess Config(string configKey, string configValue);
        IReactiveProcess DeleteRemote(string branchName);
        IReactiveProcess Fetch(string specificRef = null);
        IReactiveProcess GetCommitParents(string commitish);
        IReactiveProcess GetCommitTimestamps(params string[] commitishes);
        IReactiveProcess GetRemoteBranches();
        IReactiveProcess HasRevision(string revision);
        IReactiveProcess MergeBase(string branchA, string branchB);
        IReactiveProcess MergeBaseCommits(string branchA, string branchB);
        IReactiveProcess MergeFastForward(string branchName);
        IReactiveProcess MergeRemote(string branchName, string message = null, string commitDate = null);
        IReactiveProcess Push(string branchName, string remoteBranchName = null, bool force = false);
        string RemoteBranch(string branchName);
        IReactiveProcess UpdateRemoteRef(string branchName, string newValue);
        IReactiveProcess Reset();
        IReactiveProcess ShowRef(string branchName);
    }
}