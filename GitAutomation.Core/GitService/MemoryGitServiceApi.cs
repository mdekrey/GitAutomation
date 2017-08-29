using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitAutomation.GitService
{
    class MemoryGitServiceApi : IGitServiceApi
    {
        readonly HashSet<Tuple<string, string>> pullRequests = new HashSet<Tuple<string, string>>();

        public Task<bool> HasOpenPullRequest(string targetBranch = null, string sourceBranch = null)
        {
            var hasOpenPullRequest = pullRequests.Any(ex => (targetBranch == null || ex.Item1 == targetBranch) && (sourceBranch == null || ex.Item2 == sourceBranch));
            return Task.FromResult(hasOpenPullRequest);
        }

        public Task<bool> OpenPullRequest(string title, string targetBranch, string sourceBranch, string body = null)
        {
            if (!pullRequests.Contains(new Tuple<string, string>(targetBranch, sourceBranch)))
            {
                pullRequests.Add(new Tuple<string, string>(targetBranch, sourceBranch));
            }
            return Task.FromResult(true);
        }
    }
}
