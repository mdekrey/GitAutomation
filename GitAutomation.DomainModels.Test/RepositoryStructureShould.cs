using GitAutomation.DomainModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static GitAutomation.StringHelpers;

namespace GitAutomation
{
    [TestClass]
    public class RepositoryStructureShould
    {

        private string ValidationErrorComparable(ValidationError error) =>
            $"{error.ErrorCode}: {{{ string.Join(", ", ToComparableText(error.Arguments, ArgumentComparable)) }}}";

        private static string ArgumentComparable(KeyValuePair<string, string> arg) => $"{arg.Key}: {arg.Value}";

        [TestMethod]
        public void ReportUnknownUpstreamBranchAsValidationIssues()
        {
            var repo = new RepositoryStructure.Builder()
            {
                BranchReserves = new Dictionary<string, BranchReserve.Builder>
                {
                    { "a", new BranchReserve.Builder() { FlowType = "Flow", ReserveType = "Reserve", Status = "Status", Upstream = new HashSet<string> { "b" } } },
                    { "b", new BranchReserve.Builder() { FlowType = "Flow", ReserveType = "Reserve", Status = "Status", Upstream = new HashSet<string>{ "c" } } },
                }
            }.Build();

            var errors = ToComparableText(repo.GetValidationErrors(), ValidationErrorComparable);

            Assert.IsTrue(errors.SequenceEqual(new[]
            {
                "ReserveUpstreamInvalid: {reserve: b, upstream: c}"
            }));
        }

        [TestMethod]
        public void ReportCyclesAsValidationIssues()
        {
            var repo = new RepositoryStructure.Builder()
            {
                BranchReserves = new Dictionary<string, BranchReserve.Builder>
                {
                    { "a", new BranchReserve.Builder() { FlowType = "Flow", ReserveType = "Reserve", Status = "Status", Upstream = new HashSet<string> { "b" } } },
                    { "b", new BranchReserve.Builder() { FlowType = "Flow", ReserveType = "Reserve", Status = "Status", Upstream = new HashSet<string> { "c" } } },
                    { "c", new BranchReserve.Builder() { FlowType = "Flow", ReserveType = "Reserve", Status = "Status", Upstream = new HashSet<string> { "a", "d" } } },
                    { "d", new BranchReserve.Builder() { FlowType = "Flow", ReserveType = "Reserve", Status = "Status", Upstream = new HashSet<string> { "c", "a" } } },
                }
            }.Build();

            var errors = ToComparableText(repo.GetValidationErrors(), ValidationErrorComparable);

            Assert.IsTrue(errors.SequenceEqual(new[]
            {
                "ReserveCycleDetected: {cycle: a -> c -> b -> a}",
                "ReserveCycleDetected: {cycle: a -> d -> c -> b -> a}",
                "ReserveCycleDetected: {cycle: c -> d -> c}",
            }));
        }

        [TestMethod]
        public void DetectCycles()
        {
            var repo = new RepositoryStructure.Builder()
            {
                BranchReserves = new Dictionary<string, BranchReserve.Builder>
                {
                    { "a", new BranchReserve.Builder() { FlowType = "Flow", ReserveType = "Reserve", Status = "Status", Upstream = new HashSet<string> { "b" } } },
                    { "b", new BranchReserve.Builder() { FlowType = "Flow", ReserveType = "Reserve", Status = "Status", Upstream = new HashSet<string> { "c" } } },
                    { "c", new BranchReserve.Builder() { FlowType = "Flow", ReserveType = "Reserve", Status = "Status", Upstream = new HashSet<string> { "a", "d" } } },
                    { "d", new BranchReserve.Builder() { FlowType = "Flow", ReserveType = "Reserve", Status = "Status", Upstream = new HashSet<string> { "c", "a" } } },
                }
            }.Build();

            var cycles = ToComparableText(Cycle.FindAllCycles(repo), c => c.ToString());

            Assert.IsTrue(cycles.SequenceEqual(new[] {
                "a -> c -> b -> a",
                "a -> d -> c -> b -> a",
                "c -> d -> c"
            }));
        }
    }
}
