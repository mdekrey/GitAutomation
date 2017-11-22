using GitAutomation.GitService;
using GraphQL.Types;

namespace GitAutomation.GraphQL
{
    internal class PullRequestReviewInterface : ObjectGraphType<PullRequestReview>
    {
        public PullRequestReviewInterface()
        {
            Field(v => v.Author);
            Field(r => r.Url);
            Field<PullRequestReviewApprovalStateEnum>()
                .Name(nameof(PullRequestReview.State))
                .Resolve(ctx => ctx.Source.State);
        }
    }
}