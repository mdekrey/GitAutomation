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

        public static PullRequestReview.ApprovalState? ToApprovalState(string state)
        {
            switch (state.ToUpper())
            {
                case "APPROVED":
                    return PullRequestReview.ApprovalState.Approved;
                case "COMMENTED":
                case "DISMISSED":
                    return null;
                case "CHANGES_REQUESTED":
                    return PullRequestReview.ApprovalState.ChangesRequested;
                default:
                    return PullRequestReview.ApprovalState.Pending;
            }
        }
    }
}
