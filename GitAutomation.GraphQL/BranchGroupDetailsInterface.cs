using DataLoader;
using GitAutomation.BranchSettings;
using GitAutomation.GraphQL.Utilities.Resolvers;
using GitAutomation.Repository;
using GraphQL.Types;
using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using static GitAutomation.GraphQL.Utilities.Resolvers.Resolver;

namespace GitAutomation.GraphQL
{
    internal class BranchGroupDetailsInterface : ObjectGraphType<string>
    {
        public BranchGroupDetailsInterface()
        {
            Name = "BranchGroupDetails";

            Field(nameof(BranchGroup.GroupName), d => d)
                .Description("The full name of the group.");

            Field<BooleanGraphType>()
                .Name(nameof(BranchGroup.RecreateFromUpstream))
                .Resolve(Resolve(this, nameof(LoadRecreateFromUpstream)));

            Field<BranchGroupTypeEnum>()
                .Name(nameof(BranchGroup.BranchType))
                .Resolve(Resolve(this, nameof(LoadBranchType)));


            Field<ListGraphType<BranchGroupDetailsInterface>>()
                .Name("directDownstream")
                .Resolve(Resolve(this, nameof(DownstreamBranches)));


            Field<ListGraphType<BranchGroupDetailsInterface>>()
                .Name("directUpstream")
                .Resolve(Resolve(this, nameof(UpstreamBranches)));

            Field<ListGraphType<GitRefInterface>>()
                .Name("branches")
                .Resolve(Resolve(this, nameof(ActualBranches)));

            Field<GitRefInterface>()
                .Name("latestBranch")
                .Resolve(Resolve(this, nameof(LatestBranch)));
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