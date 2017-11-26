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
        private AsyncLazy<ImmutableList<GitRef>> remoteBranchesAsync;
        private readonly ConcurrentDictionary<Tuple<string, string>, Task<string>> mergeBaseCommits = new ConcurrentDictionary<Tuple<string, string>, Task<string>>();

        public LocalRepositoryState(IReactiveProcessFactory factory, IRepositoryOrchestration orchestration, IOptions<GitRepositoryOptions> options)
        {
            cli = new GitCli(factory, checkoutPath: options.Value.CheckoutPath, repository: options.Value.Repository, userName: options.Value.UserName, userEmail: options.Value.UserEmail);

            this.remoteBranchesAsync = BuildRemoteBranches();
        }

        public IGitCli Cli => cli;

        public async Task<ImmutableList<GitRef>> DetectUpstream(string branchName, bool allowSame)
        {
            var remotes = await remoteBranchesAsync.Value;
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
            var refs = await remoteBranchesAsync.Value;
            return await MergeBaseBetweenCommits(
                refs.Find(r => r.Name == branchName1).Commit,
                refs.Find(r => r.Name == branchName2).Commit
            );
        }

        public Task<string> MergeBaseBetweenCommits(string commit1, string commit2)
        {
            if (commit1 == null || commit2 == null)
            {
                return Task.FromResult<string>(null);
            }
            return mergeBaseCommits.GetOrAdd(
                key: commit1.CompareTo(commit2) < 0
                    ? Tuple.Create(commit1, commit2)
                    : Tuple.Create(commit2, commit1),
                valueFactory: key => cli.MergeBaseCommits(key.Item1, key.Item2).FirstOutputMessage().ToTask()
            );
        }
        
        public IObservable<ImmutableList<GitRef>> RemoteBranches()
        {
            return allUpdates.StartWith(Unit.Default).Select(_ => remoteBranchesAsync.Value).Switch();
        }
        

        private AsyncLazy<ImmutableList<GitRef>> BuildRemoteBranches()
        {
            return new AsyncLazy<ImmutableList<GitRef>>(async () =>
            {
                await cli.EnsureInitialized;
                var remoteBranches = cli.GetRemoteBranches();
                await remoteBranches.ActiveState;
                // Because listing remote branches doesn't affect the index, it doesn't need to be an action, but it does need to wait until initialization is ensured.
                return await GitCliExtensions.BranchListingToRefs(remoteBranches.Output.ToObservable());
            });
        }

        public void RefreshAll()
        {
            remoteBranchesAsync = BuildRemoteBranches();
            allUpdates.OnNext(Unit.Default);
        }

        public async void BranchUpdated(string branchName, string revision)
        {
            if (revision != null)
            {
                var hasRevision = cli.HasRevision(revision);
                await hasRevision.ActiveState;
                if (hasRevision.ExitCode != 0)
                {
                    await cli.Fetch(revision).ActiveState;
                }
            }
            await cli.UpdateRemoteRef(branchName, revision).ActiveState;

            remoteBranchesAsync = BuildRemoteBranches();
            allUpdates.OnNext(Unit.Default);
        }
    }
}
