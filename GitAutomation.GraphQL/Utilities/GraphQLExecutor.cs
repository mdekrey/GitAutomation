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
                    Inputs = GetInputs(query.Variables),
                    UserContext = serviceProvider,
                };
                return documentExecuter.ExecuteAsync(executionOptions);
            });
        }

        private Inputs GetInputs(object variables)
        {
            try
            {
                if (variables is Newtonsoft.Json.Linq.JObject obj)
                {
                    return obj.ToInputs();
                }
                if (variables is IDictionary<string, Object> vars)
                {
                    return new Inputs(vars);
                }
                else if (variables is string s)
                {
                    return Newtonsoft.Json.JsonConvert.DeserializeObject<Inputs>(s);
                }
            }
            catch
            {
            }
            return null;
        }
    }
}
