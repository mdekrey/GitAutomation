using GraphQL.Types;
using static GitAutomation.GitService.PullRequestReview;

namespace GitAutomation.GraphQL
{
    internal class PullRequestReviewApprovalStateEnum : EnumerationGraphType<ApprovalState>
    {
        public PullRequestReviewApprovalStateEnum()
        {
            Name = "PullRequestReviewApprovalState";
            Description = "Approval state of the review";

            foreach (var value in this.Values)
            {
                value.Name = ((ApprovalState)value.Value).ToString("g");
            }
        }
    }
}