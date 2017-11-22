using GitAutomation.GitService;
using GraphQL.Types;
using System.Collections.Immutable;

namespace GitAutomation.GraphQL
{
    public class PullRequestInterface : ObjectGraphType<PullRequest>
    {
        public PullRequestInterface()
        {
            Name = "PullRequest";

            Field(r => r.Id);
            Field(r => r.SourceBranch);
            Field(r => r.TargetBranch);
            Field(r => r.Url);
            Field(r => r.Author);
            Field<NonNullGraphType<ListGraphType<NonNullGraphType<PullRequestReviewInterface>>>>()
                .Name("Reviews")
                .Resolve(ctx => ctx.Source.Reviews);

            Field<PullRequestStateTypeEnum>()
                .Name(nameof(PullRequest.State))
                .Resolve(ctx => ctx.Source.State);

        }
    }
}