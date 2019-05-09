using GitAutomation.DomainModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using YamlDotNet.Serialization;

namespace GitAutomation
{
    [TestClass]
    public class YamlTest
    {
        private static RepositoryStructure testRepository = new RepositoryStructure.Builder()
        {
            BranchReserves = {
                { "line/1.0", new BranchReserve.Builder() { ReserveType = "ServiceLine", FlowType = "Auto", Status = "Stable", LastCommit = "0123456789012345678901234567890123456789" } },
                { "feature/a", new BranchReserve.Builder() {
                    ReserveType = "Feature",
                    FlowType = "Manual",
                    Status = "OutOfDate", 
                    Upstream = { "line/1.0" }, 
                    LastCommit = BranchReserve.EmptyCommit, 
                    Meta = new Dictionary<string, object> { { "Owner", "mdekrey" } } }
                },
                { "feature/b", new BranchReserve.Builder() { ReserveType = "Feature", FlowType = "Manual", Status = "OutOfDate", Upstream = { "line/1.0" }, LastCommit = BranchReserve.EmptyCommit } },
                { "rc/1.0.1", new BranchReserve.Builder() { ReserveType = "ReleaseCandidate", FlowType = "Auto", Status = "OutOfDate", Upstream = { "feature/b", "feature/a" }, LastCommit = BranchReserve.EmptyCommit } },
            }
        }.Build();
        private static string testYaml = @"
BranchReserves:
  feature/a:
    ReserveType: Feature
    FlowType: Manual
    Status: OutOfDate
    Upstream:
    - line/1.0
    LastCommit: 0000000000000000000000000000000000000000
    Meta:
      Owner: mdekrey
  feature/b:
    ReserveType: Feature
    FlowType: Manual
    Status: OutOfDate
    Upstream:
    - line/1.0
    LastCommit: 0000000000000000000000000000000000000000
    Meta: {}
  line/1.0:
    ReserveType: ServiceLine
    FlowType: Auto
    Status: Stable
    Upstream: []
    LastCommit: 0123456789012345678901234567890123456789
    Meta: {}
  rc/1.0.1:
    ReserveType: ReleaseCandidate
    FlowType: Auto
    Status: OutOfDate
    Upstream:
    - feature/a
    - feature/b
    LastCommit: 0000000000000000000000000000000000000000
    Meta: {}
".Trim();

        [TestMethod]
        public void TestSerializeRepository()
        {
            var serializer = new SerializerBuilder().DisableAliases().Build();
            var result = serializer.Serialize(testRepository);

            Assert.AreEqual(testYaml, result.Trim());
        }

        [TestMethod]
        public void TestDeserializeRepository()
        {
            var deserializer = new DeserializerBuilder().Build();
            var result = deserializer.Deserialize<RepositoryStructure.Builder>(testYaml).Build();

            Assert.AreEqual(4, result.BranchReserves.Count);
            Assert.AreEqual("ServiceLine", result.BranchReserves["line/1.0"].ReserveType);
            Assert.AreEqual("OutOfDate", result.BranchReserves["rc/1.0.1"].Status);
            Assert.AreEqual(2, result.BranchReserves["rc/1.0.1"].Upstream.Count);
            Assert.AreEqual("feature/a", result.BranchReserves["rc/1.0.1"].Upstream[0]);
            Assert.AreEqual("feature/b", result.BranchReserves["rc/1.0.1"].Upstream[1]);
        }
    }
}
