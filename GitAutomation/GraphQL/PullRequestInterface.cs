using GitAutomation.GitService;
using GraphQL.Types;

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


            Field<PullRequestStateTypeEnum>()
                .Name(nameof(PullRequest.State))
                .Resolve(ctx => ctx.Source.State);

        }
    }
}