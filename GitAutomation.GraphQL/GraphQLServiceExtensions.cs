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
            services.AddScoped<IDocumentExecuter, DocumentExecuter>();
            services.AddScoped<GraphQLExecutor>();
            services.AddSingleton<GitAutomationQuery>();
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
