using System;
using System.Collections.Generic;
using System.Linq;
using GitAutomation.Web.GraphQL;
using GraphQLParser;
using GraphQLParser.AST;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace GitAutomation.GraphQl
{
    [TestClass]
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

        [TestMethod]
        public void AllowSubsetsOfArbitraryObjects()
        {
            var subset = @"
{
  name
  labels: meta { owner: Owner }
}".AsGraphQlAst().ToJson(subject);
            Assert.AreEqual(JToken.FromObject(new { name = "Test", labels = new { owner = "mdekrey" } }).ToString(), subset.ToString());
        }

        [TestMethod]
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
            Assert.AreEqual(JToken.FromObject(new { name = "Test", meta = new { owner = "mdekrey" } }).ToString(), subset.ToString());
        }

        [TestMethod]
        public void AllowAccessToKeysAndProperties()
        {
            var subset = @"
{
  meta { Keys, Owner }
}
".AsGraphQlAst().ToJson(subject);
            Assert.AreEqual(JToken.FromObject(new { meta = new { Keys = new[] { "Owner", "owner" }, Owner = "mdekrey" } }).ToString(), subset.ToString());
        }

        [TestMethod]
        public void SupportArrays()
        {
            var subset = @"
{
  name
  numbers
}".AsGraphQlAst().ToJson(subject);
            Assert.AreEqual(JToken.FromObject(new { name = "Test", numbers = new[] { 1, 2, 3 } }).ToString(), subset.ToString());
        }

        [TestMethod]
        public void ProvideFullObjectsIfPropertiesArentMapped()
        {
            var subset = @"
{
  name
  meta
}".AsGraphQlAst().ToJson(subject);
            Assert.AreEqual(JToken.FromObject(new { name = "Test", meta = new { Owner = "mdekrey", owner = "nobody" } }).ToString(), subset.ToString());
        }
    }
}
