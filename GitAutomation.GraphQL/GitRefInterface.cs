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

            Field<NonNullGraphType<MergeBaseGraphType>>()
                .Name("compareWith")
                .Argument<NonNullGraphType<StringGraphType>>("commitish", "target of the comparison")
                .Argument<NonNullGraphType<CommitishKindTypeEnum>>("kind", "the type of 'commitish'")
                .Resolve(this, nameof(MergeBase));

            Field<NonNullGraphType<ListGraphType<CommitStatusInterface>>>()
                .Name("statuses")
                .Resolve(this, nameof(GetStatuses));

            Field<NonNullGraphType<BooleanGraphType>>()
                .Name("isBad")
                .DeprecationReason("Use `badInfo`")
                .Resolve(this, nameof(IsBad));

            Field<BadBranchInfoGraphType>()
                .Name("badInfo")
                .Resolve(this, nameof(GetBadBranchInfo));

            Field<NonNullGraphType<ListGraphType<PullRequestInterface>>>()
                .Name("pullRequestsInto")
                .Argument<StringGraphType>("target", "target of pull requests")
                .Argument<BooleanGraphType>("system", "true to include system-created PRs, false to exclude system-created PRs, omit to receive all")
                .Resolve(this, nameof(PullRequestsInto));

            Field<NonNullGraphType<ListGraphType<PullRequestInterface>>>()
                .Name("pullRequestsFrom")
                .Argument<StringGraphType>("source", "source of pull requests")
                .Argument<BooleanGraphType>("system", "true to include system-created PRs, false to exclude system-created PRs, omit to receive all")
                .Resolve(this, nameof(PullRequestsFrom));
        }

        private Task<MergeBaseInfo> MergeBase([FromArgument] string commitish, [FromArgument] CommitishKind kind, [Source] GitRef gitRef, [FromServices] Loaders loaders)
        {
            if (commitish == null)
            {
                return Task.FromResult(new MergeBaseInfo { Source = gitRef, TargetCommit = null });
            }
            switch (kind)
            {
                case CommitishKind.CommitHash:
                    return Task.FromResult(new MergeBaseInfo { Source = gitRef, TargetCommit = commitish });
                case CommitishKind.RemoteBranch:
                    return loaders.LoadAllGitRefs()
                        .ContinueWith(t => t.Result.FirstOrDefault(r => r.Name == commitish))
                        .ContinueWith(t => MergeBase(t.Result.Commit, CommitishKind.CommitHash, gitRef, loaders))
                        .Unwrap();
                case CommitishKind.LastestFromGroup:
                    return loaders.LoadLatestBranch(gitRef.Commit)
                        .ContinueWith(t => MergeBase(t.Result?.Commit, CommitishKind.RemoteBranch, gitRef, loaders))
                        .Unwrap();
                default:
                    return Task.FromResult(new MergeBaseInfo { Source = gitRef, TargetCommit = null });
            }
        }

        private Task<bool> IsBad([Source] GitRef gitRef, [FromServices] IRepositoryMediator repository)
        {
            return repository.IsBadBranch(gitRef.Name);
        }

        private Task<BadBranchInfo> GetBadBranchInfo([Source] GitRef gitRef, [FromServices] IRepositoryMediator repository)
        {
            return repository.GetBadBranchInfo(gitRef.Name);
        }

        private Task<string> BranchUrl([Source] GitRef gitRef, [FromServices] IGitServiceApi gitApi)
        {
            return gitApi.GetBranchUrl(gitRef.Name);
        }

        private Task<ImmutableList<GitService.CommitStatus>> GetStatuses([Source] GitRef gitRef, [FromServices] Loaders loaders)
        {
            return loaders.LoadBranchStatus(gitRef.Commit);
        }

        private Task<ImmutableList<GitService.PullRequest>> PullRequestsInto([Source] GitRef gitRef, [FromArgument] string target, [FromArgument] bool? system, [FromServices] Loaders loaders, ResolveFieldContext resolveContext)
        {
            
            return loaders.LoadPullRequests(
                source: gitRef.Name, 
                target: target, 
                includeReviews: resolveContext.FieldAst.SelectionSet.Selections.OfType<global::GraphQL.Language.AST.Field>().Any(field => field.Name == "reviews"),
                authorMode: system == null ? PullRequestAuthorMode.All
                    : system == true ? PullRequestAuthorMode.SystemOnly
                    : PullRequestAuthorMode.NonSystemOnly
            );
        }

        private Task<ImmutableList<GitService.PullRequest>> PullRequestsFrom([Source] GitRef gitRef, [FromArgument] string source, [FromArgument] bool? system, [FromServices] Loaders loaders, ResolveFieldContext resolveContext)
        {
            return loaders.LoadPullRequests(
                source: source, 
                target: gitRef.Name, 
                includeReviews: resolveContext.FieldAst.SelectionSet.Selections.OfType<global::GraphQL.Language.AST.Field>().Any(field => field.Name == "reviews"),
                authorMode: system == null ? PullRequestAuthorMode.All
                    : system == true ? PullRequestAuthorMode.SystemOnly
                    : PullRequestAuthorMode.NonSystemOnly
            );
        }
    }
}
