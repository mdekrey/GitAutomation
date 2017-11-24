using GitAutomation.Processes;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GitAutomation.Repository
{
    internal static class GitCliExtensions
    {
        private static readonly Regex remoteBranches = new Regex(@"^(?<commit>\S+)\s+refs/heads/(?<branch>.+)");

        public static IObservable<bool> HasOutstandingCommits(this IGitCli cli, string upstreamBranch, string downstreamBranch) =>
            Observable.CombineLatest(
                        cli.MergeBase(upstreamBranch, downstreamBranch).FirstOutputMessage(),
                        cli.ShowRef(upstreamBranch).FirstOutputMessage(),
                        (mergeBaseResult, showRefResult) => mergeBaseResult != showRefResult
                    );


        public static Task<ImmutableList<GitRef>> BranchListingToRefs(IObservable<OutputMessage> refListing)
        {
            return (
                from output in refListing
                where output.Channel == OutputChannel.Out
                let remoteBranchLine = output.Message
                let remoteBranch = remoteBranches.Match(remoteBranchLine)
                where remoteBranch.Success
                select new GitRef { Commit = remoteBranch.Groups["commit"].Value, Name = remoteBranch.Groups["branch"].Value }
            )
                .Aggregate(ImmutableList<GitRef>.Empty, (list, next) => list.Add(next))
                .FirstOrDefaultAsync().ToTask();
        }

    }
}
