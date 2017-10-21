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
            services.AddScoped<GitAutomationQuery>();
            services.AddScoped<ISchema>(serviceProvider =>
            {
                return new Schema(type =>
                {
                    var result = (IGraphType)ActivatorUtilities.GetServiceOrCreateInstance(serviceProvider, type);
                    return result;
                })
                {
                    Query = serviceProvider.GetRequiredService<GitAutomationQuery>(),
                };
            });
        }
    }
}
