using System;
using System.Collections.Generic;
using System.Linq;
using GitAutomation.Web.GraphQL;
using GraphQLParser;
using GraphQLParser.AST;
using Newtonsoft.Json.Linq;
using Xunit;

namespace GitAutomation.GraphQl
{
    public class GraphQlShould
    {
        private static readonly object subject = new {
                Name = "Test",
                Meta = new Dictionary<string, object>
                {
                    { "Owner", "mdekrey" },
                    { "owner", "nobody" }
                },
                Numbers = new[] { 1, 2, 3 }
            };

        [Fact]
        public void AllowSubsetsOfArbitraryObjects()
        {
            var subset = @"
{
  name
  labels: meta { owner: Owner }
}".AsGraphQlAst().ToJson(subject);
            Assert.Equal(JToken.FromObject(new { name = "Test", labels = new { owner = "mdekrey" } }).ToString(), subset.ToString());
        }

        [Fact]
        public void AllowFragmentsOfArbitraryObjects()
        {
            var subset = @"
{
  name
  ...meta
}

fragment meta on Whatever
{
    meta { owner: Owner }
}
".AsGraphQlAst().ToJson(subject);
            Assert.Equal(JToken.FromObject(new { name = "Test", meta = new { owner = "mdekrey" } }).ToString(), subset.ToString());
        }

        [Fact]
        public void AllowAccessToKeysAndProperties()
        {
            var subset = @"
{
  meta { Keys, Owner }
}
".AsGraphQlAst().ToJson(subject);
            Assert.Equal(JToken.FromObject(new { meta = new { Keys = new[] { "Owner", "owner" }, Owner = "mdekrey" } }).ToString(), subset.ToString());
        }

        [Fact]
        public void SupportArrays()
        {
            var subset = @"
{
  name
  numbers
}".AsGraphQlAst().ToJson(subject);
            Assert.Equal(JToken.FromObject(new { name = "Test", numbers = new[] { 1, 2, 3 } }).ToString(), subset.ToString());
        }

        [Fact]
        public void ProvideFullObjectsIfPropertiesArentMapped()
        {
            var subset = @"
{
  name
  meta
}".AsGraphQlAst().ToJson(subject);
            Assert.Equal(JToken.FromObject(new { name = "Test", meta = new { Owner = "mdekrey", owner = "nobody" } }).ToString(), subset.ToString());
        }
    }
}
