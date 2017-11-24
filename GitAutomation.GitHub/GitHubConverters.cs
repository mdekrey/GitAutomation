using GitAutomation.GitService;
using System;
using System.Collections.Generic;
using System.Text;

namespace GitAutomation.GitHub
{
    public static class GitHubConverters
    {

        public static CommitStatus.StatusState ToCommitState(string value)
        {
            switch (value.ToUpper())
            {
                case "SUCCESS": case "EXPECTED": return CommitStatus.StatusState.Success;
                case "PENDING": return CommitStatus.StatusState.Pending;
                default: return CommitStatus.StatusState.Error;
            }
        }

    }
}
