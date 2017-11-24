using GitAutomation.Orchestration;
using GitAutomation.Processes;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Threading.Tasks;

namespace GitAutomation.Repository
{
    class LocalRepositoryState : ILocalRepositoryState
    {
        private readonly IGitCli cli;
        private readonly Subject<Unit> allUpdates = new Subject<Unit>();
        private readonly IObservable<ImmutableList<GitRef>> remoteBranches;
        private readonly IObservable<ImmutableDictionary<Tuple<string, string>, LazyObservable<string>>> mergeBaseBranches;
        private readonly ConcurrentDictionary<Tuple<string, string>, Task<string>> mergeBaseCommits = new ConcurrentDictionary<Tuple<string, string>, Task<string>>();

        public LocalRepositoryState(IReactiveProcessFactory factory, IRepositoryOrchestration orchestration, IOptions<GitRepositoryOptions> options)
        {
            cli = new _GitCli(factory, checkoutPath: options.Value.CheckoutPath, repository: options.Value.Repository, userName: options.Value.UserName, userEmail: options.Value.UserEmail);

            this.remoteBranches = BuildRemoteBranches();
            this.mergeBaseBranches = BuildMergeBases();
        }

        public IGitCli Cli => cli;

        public async Task<ImmutableList<GitRef>> DetectUpstream(string branchName, bool allowSame)
        {
            var remotes = await remoteBranches.Take(1);
            var currentCommitish = remotes.FirstOrDefault(gitref => gitref.Name == branchName).Commit;

            var branches = await Task.WhenAll(from remote in remotes
                                              where remote.Name != branchName
                                              select MergeBaseBetween(remote.Name, branchName).ContinueWith(t =>
                                              {
                                                  var mergeBase = t.Result;
                                                  return new
                                                  {
                                                      remote,
                                                      mergeBase,
                                                  };
                                              }));

            var items = from branch in branches
                        where branch.remote.Commit == branch.mergeBase
                        where allowSame || branch.remote.Commit != currentCommitish
                        select branch.remote;
            return items.ToImmutableList();
        }
        public async Task<string> MergeBaseBetween(string branchName1, string branchName2)
        {
            var key = branchName1.CompareTo(branchName2) < 0
                ? Tuple.Create(branchName1, branchName2)
                : Tuple.Create(branchName2, branchName1);
            return await (this.mergeBaseBranches.SelectMany(bases => bases.ContainsKey(key)
                ? bases[key]
                : Observable.Return<string>(null))).Take(1);
        }

        public Task<string> MergeBaseBetweenCommits(string commit1, string commit2)
        {
            return mergeBaseCommits.GetOrAdd(
                key: commit1.CompareTo(commit2) < 0
                    ? Tuple.Create(commit1, commit2)
                    : Tuple.Create(commit2, commit1),
                valueFactory: key => cli.MergeBaseCommits(key.Item1, key.Item2).FirstOutputMessage().ToTask()
            );
        }

        public void NotifyRemoved(string remoteBranch)
        {
            allUpdates.OnNext(Unit.Default);
        }

        public IObservable<ImmutableList<GitRef>> RemoteBranches()
        {
            return remoteBranches;
        }

        public void ShouldFetch(string specificRef = null)
        {
            cli.Fetch().ActiveState.Subscribe(onNext: _ => { }, onCompleted: () =>
            {
                allUpdates.OnNext(Unit.Default);
            });
        }


        private IObservable<ImmutableList<GitRef>> BuildRemoteBranches()
        {
            return Observable.Merge(
                allUpdates
                    .StartWith(Unit.Default)
                    .SelectMany(async _ =>
                    {
                        if (!cli.IsGitInitialized)
                        {
                            await cli.Initialize();
                        }
                        // Because listing remote branches doesn't affect the index, it doesn't need to be an action, but it does need to wait until initialization is ensured.
                        return cli.GetRemoteBranches().ActiveOutput;
                    })
                    .Select(GitCliExtensions.BranchListingToRefs)
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

        private IObservable<string> GetMergeBase(Tuple<string, string> commitPair)
        {
            return MergeBaseBetweenCommits(commitPair.Item1, commitPair.Item2).ToObservable();
        }

        private Tuple<string, string> ToCommits(Tuple<GitRef, GitRef> pair)
        {
            var list = new[] { pair.Item1.Commit, pair.Item2.Commit }.OrderBy(commit => commit).ToArray();
            return Tuple.Create(list[0], list[1]);
        }
        
    }
}
