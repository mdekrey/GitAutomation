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
                { "line/1.0", new BranchReserve.Builder() { ReserveType = "ServiceLine", FlowType = "Auto", Status = "Stable", LastCommit = "0123456789012345678901234567890123456789" } },
                { "feature/a", new BranchReserve.Builder() {
                    ReserveType = "Feature",
                    FlowType = "Manual",
                    Status = "OutOfDate",
                    Upstream = new HashSet<string> { "line/1.0" },
                    LastCommit = BranchReserve.EmptyCommit,
                    Meta = new Dictionary<string, object> { { "Owner", "mdekrey" } } }
                },
                { "feature/b", new BranchReserve.Builder() { ReserveType = "Feature", FlowType = "Manual", Status = "OutOfDate", Upstream = new HashSet<string> { "line/1.0" }, LastCommit = BranchReserve.EmptyCommit } },
                { "rc/1.0.1", new BranchReserve.Builder() { ReserveType = "ReleaseCandidate", FlowType = "Auto", Status = "OutOfDate", Upstream = new HashSet<string> { "feature/b", "feature/a" }, LastCommit = BranchReserve.EmptyCommit } },
            }
        }.Build();
        private static readonly ISerializer serializer = Serialization.SerializationUtils.Serializer;
        private static readonly string originalYaml = serializer.Serialize(testRepository);
        private static readonly InlineDiffBuilder diffBuilder = new InlineDiffBuilder(new Differ());

        [TestMethod]
        public void StabilizeBranches()
        {
            var actual = testRepository.Reduce(new StandardAction("StabilizeBranch", new Dictionary<string, object> { { "Branch", "feature/a" } }));
            var result = GetPatch(actual);
            Assert.AreEqual(Clean(@"
-      status: OutOfDate
+      status: Stable").Trim(), result);
            Assert.AreEqual("Stable", actual.BranchReserves["feature/a"].Status);
        }

        [TestMethod]
        public void SetBranchState()
        {
            var actual = testRepository.Reduce(new StandardAction("SetBranchState", new Dictionary<string, object> { { "Branch", "feature/b" }, { "State", "Bananas" } }));
            var result = GetPatch(actual);
            Assert.AreEqual(Clean(@"
-      status: OutOfDate
+      status: Bananas").Trim(), result);
            Assert.AreEqual("Bananas", actual.BranchReserves["feature/b"].Status);
        }

        [TestMethod]
        public void SetLastCommit()
        {
            var actual = testRepository.Reduce(new StandardAction("SetLastCommit", new Dictionary<string, object> {
                { "Branch", "feature/a" },
                { "LastCommit", testRepository.BranchReserves["line/1.0"].LastCommit }
            }));
            var result = GetPatch(actual);
            Assert.AreEqual(Clean(@"
-      lastCommit: 0000000000000000000000000000000000000000
+      lastCommit: 0123456789012345678901234567890123456789").Trim(), result);
            Assert.AreEqual("0123456789012345678901234567890123456789", actual.BranchReserves["feature/a"].LastCommit);
        }

        [TestMethod]
        public void SetMeta()
        {
            var actual = testRepository.Reduce(new StandardAction("SetMeta",
                new Dictionary<string, object> {
                { "Branch", "feature/a" },
                { "Meta", new Dictionary<string, object> { { "Owner", "anonymous" }, { "OriginalOwner", "mdekrey" } } }
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
        public void RemoveBranch()
        {
            var actual = testRepository.Reduce(new StandardAction("RemoveBranch",
                new Dictionary<string, object> {
                { "Branch", "feature/a" }
            }));
            var result = GetPatch(actual);
            Assert.AreEqual(Clean(@"
-    feature/a:
-      reserveType: Feature
-      flowType: Manual
-      status: OutOfDate
-      upstream:
-      - line/1.0
-      lastCommit: 0000000000000000000000000000000000000000
-      meta:
-        Owner: mdekrey
-      - feature/a").Trim(), result);
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
