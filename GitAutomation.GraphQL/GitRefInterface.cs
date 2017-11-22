using GitAutomation.GitService;
using GitAutomation.GraphQL.Utilities.Resolvers;
using GitAutomation.Repository;
using GraphQL.Types;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace GitAutomation.GraphQL
{
    public class GitRefInterface : ObjectGraphType<GitRef>
    {
        public GitRefInterface()
        {
            Name = "GitRef";

            Field(r => r.Name);
            Field(r => r.Commit);
            Field<StringGraphType>()
                .Name("url")
                .Resolve(this, nameof(BranchUrl));

            Field<NonNullGraphType<StringGraphType>>()
                .Name("mergeBase")
                .Argument<NonNullGraphType<StringGraphType>>("commitish", "target of the merge base")
                .Argument<NonNullGraphType<CommitishKindTypeEnum>>("kind", "the type of 'commitish'")
                .Resolve(this, nameof(MergeBase));

            Field<NonNullGraphType<ListGraphType<CommitStatusInterface>>>()
                .Name("statuses")
                .Resolve(this, nameof(GetStatuses));

            Field<NonNullGraphType<ListGraphType<PullRequestInterface>>>()
                .Name("pullRequestsInto")
                .Argument<StringGraphType>("target", "target of pull requests")
                .Resolve(this, nameof(PullRequestsInto));

            Field<NonNullGraphType<ListGraphType<PullRequestInterface>>>()
                .Name("pullRequestsFrom")
                .Argument<StringGraphType>("source", "source of pull requests")
                .Resolve(this, nameof(PullRequestsFrom));
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

        private Task<string> BranchUrl([Source] GitRef gitRef, [FromServices] IGitServiceApi gitApi)
        {
            return gitApi.GetBranchUrl(gitRef.Name);
        }

        private Task<ImmutableList<GitService.CommitStatus>> GetStatuses([Source] GitRef gitRef, [FromServices] Loaders loaders)
        {
            return loaders.LoadBranchStatus(gitRef.Commit);
        }

        private Task<ImmutableList<GitService.PullRequest>> PullRequestsInto([Source] GitRef gitRef, [FromArgument] string target, [FromServices] Loaders loaders, ResolveFieldContext resolveContext)
        {
            
            return loaders.LoadPullRequests(source: gitRef.Name, target: target, includeReviews: resolveContext.FieldAst.SelectionSet.Selections.OfType<global::GraphQL.Language.AST.Field>().Any(field => field.Name == "reviews"));
        }

        private Task<ImmutableList<GitService.PullRequest>> PullRequestsFrom([Source] GitRef gitRef, [FromArgument] string source, [FromServices] Loaders loaders, ResolveFieldContext resolveContext)
        {
            return loaders.LoadPullRequests(source: source, target: gitRef.Name, includeReviews: resolveContext.FieldAst.SelectionSet.Selections.OfType<global::GraphQL.Language.AST.Field>().Any(field => field.Name == "reviews"));
        }
    }
}
