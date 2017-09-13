using GitAutomation.Processes;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Text;

namespace GitAutomation.Repository
{
    internal static class GitCliExtensions
    {
        public static IObservable<bool> HasOutstandingCommits(this GitCli cli, string upstreamBranch, string downstreamBranch) =>
            Observable.CombineLatest(
                        cli.MergeBase(upstreamBranch, downstreamBranch).FirstOutputMessage(),
                        cli.ShowRef(upstreamBranch).FirstOutputMessage(),
                        (mergeBaseResult, showRefResult) => mergeBaseResult != showRefResult
                    );
    }
}
