using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using GitAutomation.DomainModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using YamlDotNet.Serialization;
using static GitAutomation.StringHelpers;

namespace GitAutomation
{
    [TestClass]
    public class RepositoryStructureReducerShould
    {
        private static readonly RepositoryStructure testRepository = new RepositoryStructure.Builder()
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
                    Upstream = { { "line/1.0", new UpstreamReserve("0123456789012345678901234567890000000000").ToBuilder()  } },
                    IncludedBranches = { { "feature/a", new BranchReserveBranch.Builder { LastCommit = BranchReserve.EmptyCommit } } },
                    OutputCommit = BranchReserve.EmptyCommit,
                    Meta = new Dictionary<string, object> { { "Owner", "mdekrey" } } }
                },
                { "feature/b", new BranchReserve.Builder() {
                    ReserveType = "Feature",
                    FlowType = "Manual",
                    Status = "OutOfDate",
                    Upstream = { { "line/1.0", new UpstreamReserve("0123456789012345678901234567890000000000").ToBuilder() } },
                    IncludedBranches = { { "feature/b", new BranchReserveBranch.Builder { LastCommit = BranchReserve.EmptyCommit } } },
                    OutputCommit = BranchReserve.EmptyCommit } },
                { "rc/1.0.1", new BranchReserve.Builder() {
                    ReserveType = "ReleaseCandidate",
                    FlowType = "Auto",
                    Status = "OutOfDate",
                    Upstream = { { "feature/b", new UpstreamReserve(BranchReserve.EmptyCommit).ToBuilder() }, { "feature/a", new UpstreamReserve(BranchReserve.EmptyCommit).ToBuilder() } },
                    IncludedBranches = {
                        { "rc/1.0.1", new BranchReserveBranch.Builder { LastCommit = BranchReserve.EmptyCommit } },
                        { "rc/1.0.1-1", new BranchReserveBranch.Builder { LastCommit = BranchReserve.EmptyCommit } },
                        { "rc/1.0.1-2", new BranchReserveBranch.Builder { LastCommit = BranchReserve.EmptyCommit } }
                    },
                    OutputCommit = BranchReserve.EmptyCommit } },
            }
        }.Build();
        private static readonly ISerializer serializer = Serialization.SerializationUtils.Serializer;
        private static readonly string originalYaml = serializer.Serialize(testRepository);
        private static readonly InlineDiffBuilder diffBuilder = new InlineDiffBuilder(new Differ());

        [TestMethod]
        public void StabilizeBranches()
        {
            var actual = testRepository.Reduce(new StandardAction("RepositoryStructure:StabilizeReserve", new { Reserve = "feature/a" }));
            var result = GetPatch(actual);
            Assert.AreEqual(Clean(@"
-      status: OutOfDate
+      status: Stable").Trim(), result);
            Assert.AreEqual("Stable", actual.BranchReserves["feature/a"].Status);
        }

        [TestMethod]
        public void SetBranchState()
        {
            var actual = testRepository.Reduce(new StandardAction("RepositoryStructure:SetReserveState", new { Reserve = "feature/b" , State = "Bananas" }));
            var result = GetPatch(actual);
            Assert.AreEqual(Clean(@"
-      status: OutOfDate
+      status: Bananas").Trim(), result);
            Assert.AreEqual("Bananas", actual.BranchReserves["feature/b"].Status);
        }

        [TestMethod]
        public void SetOutputCommit()
        {
            var actual = testRepository.Reduce(new StandardAction("RepositoryStructure:SetOutputCommit", new
            {
                Reserve= "feature/a",
                testRepository.BranchReserves["line/1.0"].OutputCommit
            }));
            var result = GetPatch(actual);
            Assert.AreEqual(Clean(@"
-      outputCommit: 0000000000000000000000000000000000000000
+      outputCommit: 0123456789012345678901234567890123456789").Trim(), result);
            Assert.AreEqual("0123456789012345678901234567890123456789", actual.BranchReserves["feature/a"].OutputCommit);
        }

        [TestMethod]
        public void SetMeta()
        {
            var actual = testRepository.Reduce(new StandardAction("RepositoryStructure:SetMeta",
                new {
                Reserve = "feature/a",
                Meta = new Dictionary<string, object> { { "Owner", "anonymous" }, { "OriginalOwner", "mdekrey" } }
            }));
            var result = GetPatch(actual);
            Assert.AreEqual(Clean(@"
-        Owner: mdekrey
+        OriginalOwner: mdekrey
+        Owner: anonymous").Trim(), result);
            Assert.AreEqual("anonymous", actual.BranchReserves["feature/a"].Meta["Owner"]);
            Assert.AreEqual("mdekrey", actual.BranchReserves["feature/a"].Meta["OriginalOwner"]);
        }

        [TestMethod]
        public void RemoveReserve()
        {
            var actual = testRepository.Reduce(new StandardAction("RepositoryStructure:RemoveReserve",
                new {
                Reserve = "feature/a"
            }));
            var result = GetPatch(actual);
            Assert.AreEqual(Clean(@"
-    feature/a:
-      reserveType: Feature
-      flowType: Manual
-      status: OutOfDate
-      upstream:
-        line/1.0:
-          lastOutput: 0123456789012345678901234567890000000000
-          role: Source
-          meta: {}
-      includedBranches:
-        feature/a:
-          lastCommit: 0000000000000000000000000000000000000000
-          meta: {}
-      outputCommit: 0000000000000000000000000000000000000000
-      meta:
-        Owner: mdekrey
-        feature/a:
-          lastOutput: 0000000000000000000000000000000000000000
-          role: Source
-          meta: {}").Trim(), result);
            Assert.AreEqual(3, actual.BranchReserves.Count);
        }

        private string GetPatch(RepositoryStructure actual)
        {
            var actualYaml = serializer.Serialize(actual);
            var diff = diffBuilder.BuildDiffModel(originalYaml, actualYaml);

            var returnValue = from line in diff.Lines
                              where line.Type != ChangeType.Unchanged
                              select $"{Indicator(line.Type)} {line.Text}";
            return string.Join('\n', returnValue).Trim();
        }

        private string Clean(string target) => target.FixLineEndings();

        private string Indicator(ChangeType type) =>
            type switch
        {
            ChangeType.Deleted => "- ",
            ChangeType.Inserted => "+ ",
            ChangeType.Imaginary => "? ",
            ChangeType.Modified => "* ",
            _ => "  "
        };
    }
}
