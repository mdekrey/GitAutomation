using GitAutomation.DomainModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GitAutomation
{
    [TestClass]
    public class RepositoryStructureShould
    {
        private string[] ToComparableText<T>(IEnumerable<T> target, Func<T, String> toString) =>
            (from t in target
             let result = toString(t)
             orderby result
             select result).ToArray();

        private string ValidationErrorComparable(ValidationError error) =>
            $"{error.ErrorCode}: {{{ string.Join(", ", ToComparableText(error.Arguments, ArgumentComparable)) }}}";

        private static string ArgumentComparable(KeyValuePair<string, string> arg) => $"{arg.Key}: {arg.Value}";

        [TestMethod]
        public void ReportUnknownUpstreamBranchAsValidationIssues()
        {
            var repo = new RepositoryStructure.Builder()
            {
                BranchReserves =
                {
                    { "a", new BranchReserve.Builder() { FlowType = "Flow", ReserveType = "Reserve", Status = "Status", Upstream = { "b" } } },
                    { "b", new BranchReserve.Builder() { FlowType = "Flow", ReserveType = "Reserve", Status = "Status", Upstream = { "c" } } },
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
                BranchReserves =
                {
                    { "a", new BranchReserve.Builder() { FlowType = "Flow", ReserveType = "Reserve", Status = "Status", Upstream = { "b" } } },
                    { "b", new BranchReserve.Builder() { FlowType = "Flow", ReserveType = "Reserve", Status = "Status", Upstream = { "c" } } },
                    { "c", new BranchReserve.Builder() { FlowType = "Flow", ReserveType = "Reserve", Status = "Status", Upstream = { "a", "d" } } },
                    { "d", new BranchReserve.Builder() { FlowType = "Flow", ReserveType = "Reserve", Status = "Status", Upstream = { "c", "a" } } },
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
                BranchReserves =
                {
                    { "a", new BranchReserve.Builder() { FlowType = "Flow", ReserveType = "Reserve", Status = "Status", Upstream = { "b" } } },
                    { "b", new BranchReserve.Builder() { FlowType = "Flow", ReserveType = "Reserve", Status = "Status", Upstream = { "c" } } },
                    { "c", new BranchReserve.Builder() { FlowType = "Flow", ReserveType = "Reserve", Status = "Status", Upstream = { "a", "d" } } },
                    { "d", new BranchReserve.Builder() { FlowType = "Flow", ReserveType = "Reserve", Status = "Status", Upstream = { "c", "a" } } },
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
