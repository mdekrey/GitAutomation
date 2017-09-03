using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace GitAutomation.GitService
{
    public interface IGitServiceApi
    {
        Task<bool> OpenPullRequest(string title, string targetBranch, string sourceBranch, string body = null);
        Task<bool> HasOpenPullRequest(string targetBranch = null, string sourceBranch = null);
    }
}
