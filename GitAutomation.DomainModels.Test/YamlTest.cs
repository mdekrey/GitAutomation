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
            BranchReserves = new Dictionary<string, BranchReserve.Builder> {
                { "line/1.0", new BranchReserve.Builder() {
                    ReserveType = "ServiceLine",
                    FlowType = "Auto",
                    Status = "Stable",
                    OutputCommit = "0123456789012345678901234567890123456789",
                    IncludedBranches = { { "line/1.0", new BranchReserveBranch.Builder { LastCommit = "0123456789012345678901234567890123456789" } } } } },
                { "feature/a", new BranchReserve.Builder() {
                    ReserveType = "Feature",
                    FlowType = "Manual",
                    Status = "OutOfDate",
                    Upstream = new HashSet<string> { "line/1.0" },
                    IncludedBranches = { { "feature/a", new BranchReserveBranch.Builder { LastCommit = BranchReserve.EmptyCommit } } },
                    OutputCommit = BranchReserve.EmptyCommit,
                    Meta = new Dictionary<string, object> { { "Owner", "mdekrey" } } }
                },
                { "feature/b", new BranchReserve.Builder() {
                    ReserveType = "Feature",
                    FlowType = "Manual",
                    Status = "OutOfDate",
                    Upstream = new HashSet<string> { "line/1.0" },
                    IncludedBranches = { { "feature/b", new BranchReserveBranch.Builder { LastCommit = BranchReserve.EmptyCommit } } },
                    OutputCommit = BranchReserve.EmptyCommit } },
                { "rc/1.0.1", new BranchReserve.Builder() {
                    ReserveType = "ReleaseCandidate",
                    FlowType = "Auto",
                    Status = "OutOfDate",
                    Upstream = new HashSet<string> { "feature/b", "feature/a" },
                    IncludedBranches = {
                        { "rc/1.0.1", new BranchReserveBranch.Builder { LastCommit = BranchReserve.EmptyCommit } },
                        { "rc/1.0.1-1", new BranchReserveBranch.Builder { LastCommit = BranchReserve.EmptyCommit } },
                        { "rc/1.0.1-2", new BranchReserveBranch.Builder { LastCommit = BranchReserve.EmptyCommit } }
                    },
                    OutputCommit = BranchReserve.EmptyCommit } },
            }
        }.Build();
        private static string testYaml = @"
branchReserves:
  feature/a:
    reserveType: Feature
    flowType: Manual
    status: OutOfDate
    upstream:
    - line/1.0
    includedBranches:
      feature/a:
        lastCommit: 0000000000000000000000000000000000000000
        meta: {}
    outputCommit: 0000000000000000000000000000000000000000
    meta:
      Owner: mdekrey
  feature/b:
    reserveType: Feature
    flowType: Manual
    status: OutOfDate
    upstream:
    - line/1.0
    includedBranches:
      feature/b:
        lastCommit: 0000000000000000000000000000000000000000
        meta: {}
    outputCommit: 0000000000000000000000000000000000000000
    meta: {}
  line/1.0:
    reserveType: ServiceLine
    flowType: Auto
    status: Stable
    upstream: []
    includedBranches:
      line/1.0:
        lastCommit: 0123456789012345678901234567890123456789
        meta: {}
    outputCommit: 0123456789012345678901234567890123456789
    meta: {}
  rc/1.0.1:
    reserveType: ReleaseCandidate
    flowType: Auto
    status: OutOfDate
    upstream:
    - feature/a
    - feature/b
    includedBranches:
      rc/1.0.1:
        lastCommit: 0000000000000000000000000000000000000000
        meta: {}
      rc/1.0.1-1:
        lastCommit: 0000000000000000000000000000000000000000
        meta: {}
      rc/1.0.1-2:
        lastCommit: 0000000000000000000000000000000000000000
        meta: {}
    outputCommit: 0000000000000000000000000000000000000000
    meta: {}

".Trim();

        [TestMethod]
        public void TestSerializeRepository()
        {
            var serializer = Serialization.SerializationUtils.Serializer;
            var result = serializer.Serialize(testRepository);

            Assert.AreEqual(testYaml.FixLineEndings(), result.FixLineEndings().Trim());
        }

        [TestMethod]
        public void TestDeserializeRepository()
        {
            var deserializer = Serialization.SerializationUtils.Deserializer;
            var result = deserializer.Deserialize<RepositoryStructure.Builder>(testYaml).Build();

            Assert.AreEqual(4, result.BranchReserves.Count);
            Assert.AreEqual("ServiceLine", result.BranchReserves["line/1.0"].ReserveType);
            Assert.AreEqual("OutOfDate", result.BranchReserves["rc/1.0.1"].Status);
            Assert.AreEqual(2, result.BranchReserves["rc/1.0.1"].Upstream.Count);
            Assert.AreEqual("feature/a", result.BranchReserves["rc/1.0.1"].Upstream[0]);
            Assert.AreEqual("feature/b", result.BranchReserves["rc/1.0.1"].Upstream[1]);
            Assert.AreEqual("mdekrey", result.BranchReserves["feature/a"].Meta["Owner"]);
        }
    }
}
