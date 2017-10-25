using GraphQL.Types;

namespace GitAutomation.GraphQL
{
    internal class CommitStatusInterface : ObjectGraphType<GitService.CommitStatus>
    {
        public CommitStatusInterface()
        {
            Field(r => r.Key);
            Field(r => r.Description);
            Field(r => r.Url);

            Field<CommitStatusStateTypeEnum>()
                .Name(nameof(GitService.CommitStatus.State))
                .Resolve(ctx => ctx.Source.State);
        }
    }
}