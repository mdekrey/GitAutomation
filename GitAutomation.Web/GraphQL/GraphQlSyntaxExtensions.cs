using GraphQLParser;
using GraphQLParser.AST;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GitAutomation.Web.GraphQL
{
    public static class GraphQlSyntaxExtensions
    {
        private static readonly JsonSerializer serializer = new JsonSerializer()
        {
            Converters =
            {
                new Newtonsoft.Json.Converters.StringEnumConverter()
            }
        };

        public static GraphQLDocument AsGraphQlAst(this string graphqlQuery)
        {
            var lexer = new Lexer();
            var parser = new Parser(lexer);
            var ast = parser.Parse(new Source(graphqlQuery));
            return ast;
        }

        public static JToken ToJson(this GraphQLDocument ast, object subject)
        {
            return ToJson(ast, (ast.Definitions.First() as GraphQLOperationDefinition).SelectionSet, subject);
        }

        private static JToken ToJson(GraphQLDocument ast, GraphQLSelectionSet currentNode, object subject)
        {
            if (subject == null)
            {
                return JValue.CreateNull();
            }
            else if (subject is IEnumerable enumerable && !(subject is string) && !(subject is IDictionary))
            {
                return ToJsonArray(ast, currentNode, enumerable.Cast<object>());
            }
            else if (currentNode != null)
            {
                return ToJsonObject(ast, currentNode, subject);
            }
            else
            {
                return JToken.FromObject(subject, serializer);
            }
        }

        private static readonly Type[] indexerParameters = new[] { typeof(string) };

        private static JObject ToJsonObject(GraphQLDocument ast, GraphQLSelectionSet currentNode, object subject)
        {
            var subjType = subject.GetType();
            var properties = subjType.GetProperties().ToDictionary(p => p.Name, StringComparer.InvariantCultureIgnoreCase);
            var indexer = subjType.GetProperties()
                .FirstOrDefault(p => 
                    p.GetIndexParameters().Select(p => p.ParameterType).SequenceEqual(indexerParameters) 
                    && p.Name == "Item"
                );
            object GetValue(string keyName)
            {
                try
                {
                    if (properties.ContainsKey(keyName))
                    {
                        return properties[keyName].GetValue(subject);
                    }
                    else
                    {
                        return indexer.GetValue(subject, new[] { keyName });
                    }
                }
                catch
                {
                    // TODO - do something with errors
                }
                return null;
            }

            var result = new JObject();
            foreach (var selection in currentNode.Selections)
            {
                if (selection is GraphQLFieldSelection fieldSelection)
                {
                    var displayName = (fieldSelection.Alias ?? fieldSelection.Name).Value;
                    var actualName = fieldSelection.Name.Value;
                    result.Add(displayName, ToJson(ast, fieldSelection.SelectionSet, GetValue(actualName)));
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

        private static JArray ToJsonArray(GraphQLDocument ast, GraphQLSelectionSet currentNode, IEnumerable<object> enumerable)
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
