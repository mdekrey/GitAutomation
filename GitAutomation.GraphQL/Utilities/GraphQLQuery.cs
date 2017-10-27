using GraphQL;
using System;
using System.Collections.Generic;
using System.Text;

namespace GitAutomation.GraphQL.Utilities
{
    public class GraphQLQuery
    {
        public string OperationName { get; set; }
        public string NamedQuery { get; set; }
        public string Query { get; set; }
        public Object Variables { get; set; }

        public override string ToString()
        {
            var builder = new StringBuilder();
            builder.AppendLine();
            if (!string.IsNullOrWhiteSpace(OperationName))
            {
                builder.AppendLine($"OperationName = {OperationName}");
            }
            if (!string.IsNullOrWhiteSpace(NamedQuery))
            {
                builder.AppendLine($"NamedQuery = {NamedQuery}");
            }
            if (!string.IsNullOrWhiteSpace(Query))
            {
                builder.AppendLine($"Query = {Query}");
            }
            if (Variables != null)
            {
                if (Variables is string s)
                {
                    builder.AppendLine($"Variables = {s}");
                }
                else
                {
                    builder.AppendLine($"Variables = {Newtonsoft.Json.JsonConvert.SerializeObject(Variables)}");

                }
            }

            return builder.ToString();
        }
    }

}
