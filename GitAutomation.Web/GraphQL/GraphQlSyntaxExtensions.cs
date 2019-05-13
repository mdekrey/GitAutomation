using GraphQLParser;
using GraphQLParser.AST;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GitAutomation.Web.GraphQL
{
    public static class GraphQlSyntaxExtensions
    {
        public static GraphQLDocument AsGraphQlAst(this string graphqlQuery)
        {
            var lexer = new Lexer();
            var parser = new Parser(lexer);
            var ast = parser.Parse(new Source(graphqlQuery));
            return ast;
        }

        public static JToken ToJson(this GraphQLDocument ast, object subject)
        {
            // TODO - shouldn't pre-convert to JObject for all of this, as it precludes cycles, etc. Instead, fix the ToJsonObject method
            return ToJson(ast, (ast.Definitions.First() as GraphQLOperationDefinition).SelectionSet, JToken.FromObject(subject));
        }

        private static JToken ToJson(GraphQLDocument ast, GraphQLSelectionSet currentNode, JToken subject)
        {
            if (subject is JArray enumerable)
            {
                return ToJsonArray(ast, currentNode, enumerable);
            }
            else if (subject is JObject obj && currentNode != null)
            {
                return ToJsonObject(ast, currentNode, obj);
            }
            else
            {
                return (subject is JValue) ? subject : JValue.CreateNull();
            }
        }

        private static JObject ToJsonObject(GraphQLDocument ast, GraphQLSelectionSet currentNode, JObject subject)
        {
            var result = new JObject();
            foreach (var selection in currentNode.Selections)
            {
                if (selection is GraphQLFieldSelection fieldSelection)
                {
                    var displayName = (fieldSelection.Alias ?? fieldSelection.Name).Value;
                    var actualName = fieldSelection.Name.Value;
                    var prop = subject.Properties().FirstOrDefault(p => string.Compare(p.Name, actualName, true) == 0);
                    if (prop != null)
                    {
                        result.Add(displayName, ToJson(ast, fieldSelection.SelectionSet, prop.Value));
                    }
                    else if (actualName == "_")
                    {
                        foreach (var p in subject.Properties())
                        {
                            result.Add(p.Name, ToJson(ast, fieldSelection.SelectionSet, p.Value));
                        }
                    }
                }
                else if (selection is GraphQLFragmentSpread fragmentSpread)
                {
                    var def = ast.Definitions.OfType<GraphQLFragmentDefinition>().FirstOrDefault(f => f.Name.Value == fragmentSpread.Name.Value);
                    var fragmentResult = ToJsonObject(ast, def.SelectionSet, subject);
                    foreach (var prop in fragmentResult.Properties())
                    {
                        result.Add(prop.Name, prop.Value);
                    }
                }
            }
            return result;
        }

        private static JArray ToJsonArray(GraphQLDocument ast, GraphQLSelectionSet currentNode, JArray enumerable)
        {
            var result = new JArray();

            foreach (var entry in enumerable)
            {
                result.Add(ToJson(ast, currentNode, entry));
            }

            return result;
        }
    }
}
