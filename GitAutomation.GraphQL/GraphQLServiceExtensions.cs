using GitAutomation.GraphQL;
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
        public static void AddGraphQLServices(this IServiceCollection services)
        {
            services.AddScoped<IDataLoaderContextAccessor>(sp => sp.GetRequiredService<DataLoaderContextStore>());
            services.AddScoped<DataLoaderContextStore>();
            // Do not access the lod context this way; it handles disposing itself
            //services.AddTransient(sp => sp.GetRequiredService<IDataLoaderContextAccessor>().LoadContext);

            services.AddScoped<IDocumentExecuter, DocumentExecuter>();
            services.AddScoped<GraphQLExecutor>();
            services.AddSingleton<GitAutomationQuery>();
            services.AddScoped<Loaders>();
            services.AddSingleton<ISchema>(serviceProvider =>
            {
                return new Schema(type => (IGraphType)ActivatorUtilities.GetServiceOrCreateInstance(serviceProvider, type))
                {
                    Query = serviceProvider.GetRequiredService<GitAutomationQuery>(),
                };
            });
        }
    }
}
