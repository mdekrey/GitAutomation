using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace GitAutomation.GitService
{
    public static class GitServiceApiExtensions
    {
        public static Task<bool> HasOpenPullRequest(this IGitServiceApi api, string targetBranch = null, string sourceBranch = null)
        {
            return api.GetPullRequests(state: PullRequestState.Open, targetBranch: targetBranch, sourceBranch: sourceBranch).ContinueWith(t => t.Result.Count > 0);
        }
    }
}
