using GraphQL.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GitAutomation.GraphQL.Resolvers
{
    interface IDataResolver<TSource>
    {
        Task<object> Resolve(ResolveFieldContext<TSource> context);
    }

}
