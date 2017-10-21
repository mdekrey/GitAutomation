using GitAutomation.BranchSettings;
using GitAutomation.GraphQL.Resolvers;
using GraphQL;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Reflection;
using System.Threading.Tasks;

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
                .Resolve(Resolve<object, BranchGroupResolver>());
        }

        Func<ResolveFieldContext<TSource>, Task<object>> Resolve<TSource, TLoader>()
            where TLoader : IDataResolver<TSource>
        {
            return (context) =>
            {
                var loaderInstance = ActivatorUtilities.GetServiceOrCreateInstance<TLoader>(context.UserContext as IServiceProvider);
                return loaderInstance.Resolve(context);
            };
        }

        class BranchGroupResolver : DataResolverBase<object, BranchGroupResolver.Args, BranchGroup>
        {
            public class Args
            {
                public string Name { get; set; }
            }

            private readonly IBranchSettingsAccessor settings;

            public BranchGroupResolver(IBranchSettingsAccessor settings)
            {
                this.settings = settings;
            }

            protected override async Task<BranchGroup> Resolve(Args arg)
            {
                var result = await settings.GetBranchGroups(arg.Name).ConfigureAwait(false);
                
                return result.TryGetValue(arg.Name, out var value) 
                    ? value
                    : null;
            }
        }
    }
}