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
                    { "Owner", "mdekrey" }
                },
                Numbers = new[] { 1, 2, 3 }
            };

        [TestMethod]
        public void AllowSubsetsOfArbitraryObjects()
        {
            var lexer = new Lexer();
            var parser = new Parser(lexer);
            var ast = parser.Parse(new Source(@"
{
  name
  labels: meta { owner }
}"));
            var subset = ast.ToJson(subject);
            Assert.AreEqual(JToken.FromObject(new { name = "Test", labels = new { owner = "mdekrey" } }).ToString(), subset.ToString());
        }

        [TestMethod]
        public void AllowFragmentsOfArbitraryObjects()
        {
            var lexer = new Lexer();
            var parser = new Parser(lexer);
            var ast = parser.Parse(new Source(@"
{
  name
  ...meta
}

fragment meta on Whatever
{
    meta { owner }
}
"));
            var subset = ast.ToJson(subject);
            Assert.AreEqual(JToken.FromObject(new { name = "Test", meta = new { owner = "mdekrey" } }).ToString(), subset.ToString());
        }

        [TestMethod]
        public void SupportArrays()
        {
            var lexer = new Lexer();
            var parser = new Parser(lexer);
            var ast = parser.Parse(new Source(@"
{
  name
  numbers
}"));
            var subset = ast.ToJson(subject);
            Assert.AreEqual(JToken.FromObject(new { name = "Test", numbers = new[] { 1, 2, 3 } }).ToString(), subset.ToString());
        }

        [TestMethod]
        public void IgnoreObjectsIfPropertiesArentMapped()
        {
            var lexer = new Lexer();
            var parser = new Parser(lexer);
            var ast = parser.Parse(new Source(@"
{
  name
  meta
}"));
            var subset = ast.ToJson(subject);
            Assert.AreEqual(JToken.FromObject(new { name = "Test", meta = (object)null }).ToString(), subset.ToString());
        }
    }
}
