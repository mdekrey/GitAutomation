using GraphQL.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace GitAutomation.GraphQL.Utilities.Resolvers
{
    public static class GraphQLFieldBuilderExtensions
    {
        /// <summary>
        /// Easy-use resolver by method. Takes the target and the method because C# doesn't have a way to easily convert a "method group" into a delegate.
        /// </summary>
        public static FieldBuilder<TSourceType, TReturnType> Resolve<TSourceType, TReturnType>(this FieldBuilder<TSourceType, TReturnType> fieldBuilder, object target, string methodName)
        {
            return fieldBuilder.Resolve(Resolver.Resolve(target, methodName));
        }
    }
}
