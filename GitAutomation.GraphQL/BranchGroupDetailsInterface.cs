using DataLoader;
using GitAutomation.BranchSettings;
using GitAutomation.GraphQL.Utilities.Resolvers;
using GitAutomation.Repository;
using GraphQL.Types;
using System;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace GitAutomation.GraphQL
{
    internal class BranchGroupDetailsInterface : ObjectGraphType<string>
    {
        public BranchGroupDetailsInterface()
        {
            Name = "BranchGroupDetails";

            Field(nameof(BranchGroup.GroupName), d => d)
                .Description("The full name of the group.");

            Field<NonNullGraphType<BooleanGraphType>>()
                .Name(nameof(BranchGroup.RecreateFromUpstream))
                .Resolve(this, nameof(LoadRecreateFromUpstream));

            Field<NonNullGraphType<BranchGroupTypeEnum>>()
                .Name(nameof(BranchGroup.BranchType))
                .Resolve(this, nameof(LoadBranchType));


            Field<NonNullGraphType<ListGraphType<BranchGroupDetailsInterface>>>()
                .Name("directDownstream")
                .Resolve(this, nameof(DownstreamBranches));


            Field<NonNullGraphType<ListGraphType<BranchGroupDetailsInterface>>>()
                .Name("directUpstream")
                .Resolve(this, nameof(UpstreamBranches));

            Field<NonNullGraphType<ListGraphType<GitRefInterface>>>()
                .Name("branches")
                .Resolve(this, nameof(ActualBranches));

            Field<GitRefInterface>()
                .Name("latestBranch")
                .Resolve(this, nameof(LatestBranch));
        }

        Task<bool?> LoadRecreateFromUpstream([Source] string name, [FromServices] Loaders loaders)
        {
            return loaders.LoadBranchGroup(name)
                .ContinueWith(r =>
                {
                    return r.Result?.RecreateFromUpstream;
                });
        }

        Task<BranchGroupType?> LoadBranchType([Source] string name, [FromServices] Loaders loaders)
        {
            return loaders.LoadBranchGroup(name)
                .ContinueWith(r =>
                {
                    return r.Result?.BranchType;
                });
        }

        Task<ImmutableList<string>> DownstreamBranches([Source] string name, [FromServices] Loaders loaders)
        {
            return loaders.LoadDownstreamBranches(name);
        }

        Task<ImmutableList<string>> UpstreamBranches([Source] string name, [FromServices] Loaders loaders)
        {
            return loaders.LoadUpstreamBranches(name);
        }

        Task<ImmutableList<GitRef>> ActualBranches([Source] string name, [FromServices] Loaders loaders)
        {
            return loaders.LoadActualBranches(name);
        }

        Task<GitRef> LatestBranch([Source] string name, [FromServices] Loaders loaders)
        {
            return loaders.LoadLatestBranch(name);
        }
    }
}