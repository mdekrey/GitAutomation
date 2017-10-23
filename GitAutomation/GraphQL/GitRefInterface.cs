using GitAutomation.GraphQL.Resolvers;
using GitAutomation.Repository;
using GraphQL.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static GitAutomation.GraphQL.Resolvers.Resolver;

namespace GitAutomation.GraphQL
{
    public class GitRefInterface : ObjectGraphType<GitRef>
    {
        public GitRefInterface()
        {
            Name = "GitRef";

            Field(r => r.Name);
            Field(r => r.Commit);

            Field<NonNullGraphType<StringGraphType>>()
                .Name("mergeBase")
                .Argument<NonNullGraphType<StringGraphType>>("commitish", "target of the merge base")
                .Argument<NonNullGraphType<CommitishKindTypeEnum>>("kind", "the type of 'commitish'")
                .Resolve(Resolve(this, nameof(MergeBase)));
        }

        private Task<string> MergeBase([FromArgument] string commitish, [FromArgument] CommitishKind kind, [Source] GitRef gitRef, [FromServices] Loaders loaders)
        {
            switch (kind)
            {
                case CommitishKind.CommitHash:
                    return loaders.GetMergeBaseOfCommits(gitRef.Commit, commitish);
                case CommitishKind.RemoteBranch:
                    return loaders.GetMergeBaseOfCommitAndRemoteBranch(gitRef.Commit, commitish);
                case CommitishKind.LastestFromGroup:
                    return loaders.GetMergeBaseOfCommitAndGroup(gitRef.Commit, commitish);
                default:
                    return Task.FromResult<string>(null);
            }
        }
    }
}
