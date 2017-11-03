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
            Field<NonNullGraphType<ListGraphType<NonNullGraphType<PullRequestReviewInterface>>>>()
                .Name("Reviews")
                // TODO - add resolver
                .Resolve(ctx => ImmutableList<PullRequestReview>.Empty);

            Field<PullRequestStateTypeEnum>()
                .Name(nameof(PullRequest.State))
                .Resolve(ctx => ctx.Source.State);

        }
    }
}