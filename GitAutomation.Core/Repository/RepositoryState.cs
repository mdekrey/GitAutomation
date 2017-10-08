using GitAutomation.Processes;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive;
using System.Collections.Immutable;
using GitAutomation.Orchestration.Actions;
using GitAutomation.Orchestration;
using System.Threading.Tasks;
using System.Reactive.Threading.Tasks;

namespace GitAutomation.Repository
{
    class RepositoryState : IRepositoryState
    {
        private readonly string checkoutPath;

        private readonly IObservable<Unit> allUpdates;
        private readonly IObservable<ImmutableList<GitRef>> remoteBranches;
        private readonly IRepositoryOrchestration orchestration;
        private readonly GitCli cli;
        private readonly IObservable<ImmutableDictionary<Tuple<string, string>, LazyObservable<string>>> mergeBases;

        public event EventHandler Updated;

        public RepositoryState(IRepositoryOrchestration orchestration, IOptions<GitRepositoryOptions> options, GitCli cli)
        {
            this.checkoutPath = options.Value.CheckoutPath;
            this.orchestration = orchestration;
            this.cli = cli;

            this.allUpdates = Observable.FromEventPattern<EventHandler, EventArgs>(
                handler => this.Updated += handler,
                handler => this.Updated -= handler
            ).Select(_ => Unit.Default);
            this.remoteBranches = BuildRemoteBranches();

            this.mergeBases = BuildMergeBases();
        }

        #region Reset

        public IObservable<OutputMessage> DeleteRepository()
        {
            return orchestration.EnqueueAction(new ClearAction());
        }
        
        #endregion

        #region Updates

        protected virtual void OnUpdated()
        {
            Updated?.Invoke(this, EventArgs.Empty);
        }

        public IObservable<OutputMessage> CheckForUpdates()
        {
            return orchestration.EnqueueAction(new UpdateAction()).Finally(OnUpdated);
        }

        public void NotifyPushedRemoteBranch(string downstreamBranch)
        {
            Updated?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        private IObservable<ImmutableList<GitRef>> BuildRemoteBranches()
        {
            return Observable.Merge(
                allUpdates
                    .StartWith(Unit.Default)
                    .SelectMany(async _ =>
                    {
                        if (!cli.IsGitInitialized)
                        {
                            await orchestration.EnqueueAction(new EnsureInitializedAction());
                        }
                        // Because listing remote branches doesn't affect the index, it doesn't need to be an action, but it does need to wait until initialization is ensured.
                        return cli.GetRemoteBranches().Output;
                    })
                    .Select(GitCli.BranchListingToRefs)
            )
                .Replay(1).ConnectFirst();
        }

        private IObservable<ImmutableDictionary<Tuple<string, string>, LazyObservable<string>>> BuildMergeBases()
        {
            var branchPairs = remoteBranches.Select(branches =>
                from branch1 in branches
                from branch2 in branches.Where(branch => branch.Name.CompareTo(branch1.Name) > 0)
                select (branch1, branch2).ToTuple());
            var mergeBases = branchPairs
                .Select(pairs => pairs.Select(ToCommits).Distinct().ToImmutableHashSet())
                .Scan(ImmutableDictionary<Tuple<string, string>, LazyObservable<string>>.Empty,
                (old, commitPairs) =>
                {
                    foreach (var removing in old.Where(kvp => !commitPairs.Contains(kvp.Key)))
                    {
                        removing.Value.Dispose();
                    }

                    return old.Where(kvp => commitPairs.Contains(kvp.Key))
                        .Concat(from pair in commitPairs
                                where !old.ContainsKey(pair)
                                select new System.Collections.Generic.KeyValuePair<Tuple<string, string>, LazyObservable<string>>(
                                    pair, 
                                    new LazyObservable<string>(GetMergeBase(pair).Replay(1))
                                )
                            )
                        .ToImmutableDictionary();
                });
            return mergeBases.Zip(branchPairs, (commits, branches) =>
                    branches.ToImmutableDictionary(pair => Tuple.Create(pair.Item1.Name, pair.Item2.Name), pair => commits[ToCommits(pair)])
                ).Replay(1).ConnectFirst();
        }
        
        public IObservable<ImmutableList<GitRef>> RemoteBranches()
        {
            return remoteBranches;
        }

        private Tuple<string, string> ToCommits(Tuple<GitRef, GitRef> pair)
        {
            var list = new[] { pair.Item1.Commit, pair.Item2.Commit }.OrderBy(commit => commit).ToArray();
            return Tuple.Create(list[0], list[1]);
        }

        public IObservable<string> MergeBaseBetween(string branchName1, string branchName2)
        {
            var key = branchName1.CompareTo(branchName2) < 0
                ? Tuple.Create(branchName1, branchName2)
                : Tuple.Create(branchName2, branchName1);
            return this.mergeBases.SelectMany(bases => bases.ContainsKey(key)
                ? bases[key]
                : Observable.Return<string>(null));
        }

        private IObservable<string> GetMergeBase(Tuple<string, string> commitPair)
        {
            return cli.MergeBaseCommits(commitPair.Item1, commitPair.Item2).GetFirstOutput();
        }

        public IObservable<OutputMessage> DeleteBranch(string branchName)
        {
            return orchestration.EnqueueAction(new DeleteBranchAction(branchName)).Finally(OnUpdated);
        }

        public IObservable<ImmutableList<GitRef>> DetectUpstream(string branchName, bool allowSame)
        {
            return remoteBranches
                .Select(remotes =>
                {
                    var currentCommitish = remotes.FirstOrDefault(gitref => gitref.Name == branchName).Commit;
                    
                    return (from remote in remotes.ToObservable()
                            where remote.Name != branchName
                            from mergeBase in MergeBaseBetween(remote.Name, branchName).Take(1)
                            select new
                            {
                                remote,
                                mergeBase,
                            } into branch
                            where branch.remote.Commit == branch.mergeBase
                            where allowSame || branch.remote.Commit != currentCommitish
                            select branch.remote).ToArray();
                }).Switch().Select(items => items.ToImmutableList());
        }
    }
}
