using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Types;
using DataLoader;
using Microsoft.Extensions.DependencyInjection;

namespace GitAutomation.GraphQL.Utilities
{
    public class GraphQLExecutor
    {
        private readonly IDocumentExecuter documentExecuter;
        private readonly ISchema schema;
        private readonly IServiceProvider serviceProvider;

        public GraphQLExecutor(IDocumentExecuter documentExecuter, ISchema schema, IServiceProvider serviceProvider)
        {
            this.documentExecuter = documentExecuter;
            this.schema = schema;
            this.serviceProvider = serviceProvider;
        }

        public Task<ExecutionResult> Execute(GraphQLQuery query)
        {
            return DataLoaderContext.Run(loadCtx =>
            {
                serviceProvider.GetRequiredService<DataLoaderContextStore>().LoadContext = loadCtx;
                var executionOptions = new ExecutionOptions
                {
                    Schema = schema,
                    Query = query.Query,
                    Inputs = Newtonsoft.Json.JsonConvert.DeserializeObject<Inputs>(query.Variables),
                    UserContext = serviceProvider,
                };
                return documentExecuter.ExecuteAsync(executionOptions);
            });
        }
    }
}
