using GraphQL;
using GraphQL.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GitAutomation.GraphQL.Resolvers
{
    abstract class DataResolverBase<TSource, TIn, TOut> : IDataResolver<TSource>
        where TIn : class, new()
    {
        public Task<object> Resolve(ResolveFieldContext<TSource> context)
        {
            var argInstance = GetArguments(context);
            return Resolve(argInstance).ContinueWith(task => (object)task.Result);
        }

        protected virtual TIn GetArguments(ResolveFieldContext<TSource> context)
        {
            return context.Arguments.ToObject<TIn>();
        }

        protected abstract Task<TOut> Resolve(TIn args);
    }

}
