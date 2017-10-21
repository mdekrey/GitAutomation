using DataLoader;
using GitAutomation.BranchSettings;
using GitAutomation.GraphQL.Resolvers;
using GraphQL.Types;
using System;
using System.Threading.Tasks;
using static GitAutomation.GraphQL.Resolvers.Resolver;

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
    }
}