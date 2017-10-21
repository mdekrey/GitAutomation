using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Types;

namespace GitAutomation.GraphQL
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

        public Task<ExecutionResult> Execute(string query)
        {
            var executionOptions = new ExecutionOptions
            {
                Schema = schema,
                Query = query,
                UserContext = serviceProvider,
            };
            return documentExecuter.ExecuteAsync(executionOptions);
        }
    }
}
