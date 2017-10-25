using GitAutomation.GraphQL;
using GitAutomation.GraphQL.Utilities;
using GraphQL;
using GraphQL.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class GraphQLServiceExtensions
    {
        public static void AddGraphQLServices<TQuery>(this IServiceCollection services)
            where TQuery : class, IObjectGraphType
        {
            services.AddScoped<IDataLoaderContextAccessor>(sp => sp.GetRequiredService<DataLoaderContextStore>());
            services.AddScoped<DataLoaderContextStore>();
            // Do not access the lod context this way; it handles disposing itself
            //services.AddTransient(sp => sp.GetRequiredService<IDataLoaderContextAccessor>().LoadContext);

            services.AddScoped<IDocumentExecuter, DocumentExecuter>();
            services.AddScoped<GraphQLExecutor>();
            services.AddSingleton<TQuery>();
            services.AddSingleton<ISchema>(serviceProvider =>
            {
                return new Schema(type => (IGraphType)ActivatorUtilities.GetServiceOrCreateInstance(serviceProvider, type))
                {
                    Query = serviceProvider.GetRequiredService<TQuery>(),
                };
            });
        }
    }
}
