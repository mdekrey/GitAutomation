using GitAutomation.BranchSettings;
using GitAutomation.GraphQL.Resolvers;
using GitAutomation.Repository;
using GraphQL;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Reflection;
using System.Threading.Tasks;
using static GitAutomation.GraphQL.Resolvers.Resolver;

namespace GitAutomation.GraphQL
{
    internal class GitAutomationQuery : ObjectGraphType<object>
    {
        public GitAutomationQuery()
        {
            Name = "Query";

            Field<BranchGroupDetailsInterface>()
                .Name("branchGroup")
                .Argument<NonNullGraphType<StringGraphType>>("name", "full name of the branch group")
                .Resolve(ctx => ctx.GetArgument<string>("name"));

            Field<ListGraphType<BranchGroupDetailsInterface>>()
                .Name("configuredBranchGroups")
                .Resolve(Resolve(this, nameof(BranchGroups)));

            Field<ListGraphType<GitRefInterface>>()
                .Name("allActualBranches")
                .Resolve(Resolve(this, nameof(AllGitRefs)));
        }

        Task<ImmutableList<string>> BranchGroups([FromServices] Loaders loaders)
        {
            return loaders.LoadBranchGroups();
        }

        Task<ImmutableList<GitRef>> AllGitRefs([FromServices] Loaders loaders)
        {
            return loaders.LoadAllGitRefs();
        }
    }
}