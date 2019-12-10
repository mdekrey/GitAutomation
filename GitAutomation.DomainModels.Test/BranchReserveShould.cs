using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace GitAutomation.DomainModels.Test
{
    [TestClass]
    public class BranchReserveShould
    {
        [TestMethod]
        public void SupportBuildingFromScratch()
        {
            var builder = new BranchReserve.Builder()
            {
                ReserveType = "Normal",
                FlowType = "Auto",
                Status = "Stable"
            };

            var actual = builder.Build();
            Assert.AreEqual("Normal", actual.ReserveType);
            Assert.AreEqual("Auto", actual.FlowType);
            Assert.AreEqual("Stable", actual.Status);
        }

        [TestMethod]
        public void SupportUpdatingViaABuilder()
        {
            var original = new BranchReserve.Builder()
            {
                ReserveType = "Normal",
                FlowType = "Auto",
                Status = "Stable",
                Upstream = new Dictionary<string, UpstreamReserve.Builder>(),
                OutputCommit = BranchReserve.EmptyCommit,
                Meta = new Dictionary<string, string>()
            }.Build();
            var builder = original.ToBuilder();
            builder.ReserveType = ("ReleaseCandidate");
            builder.FlowType = ("Manual");
            builder.Status = ("OutOfDate");
            builder.Meta = new Dictionary<string, string> { { "Owner", "mdekrey" } };
            builder.Upstream = new Dictionary<string, UpstreamReserve.Builder> { { "foo", new UpstreamReserve.Builder { LastOutput = "0123456789ABCDEF", Role = "Source" } } };

            var actual = builder.Build();
            Assert.AreEqual("ReleaseCandidate", actual.ReserveType);
            Assert.AreEqual("Manual", actual.FlowType);
            Assert.AreEqual("OutOfDate", actual.Status);
            Assert.AreEqual("mdekrey", actual.Meta["Owner"]);
            Assert.IsTrue(actual.Upstream.Keys.SequenceEqual(new[] { "foo" }));
            Assert.AreEqual("Normal", original.ReserveType);
            Assert.AreEqual("Auto", original.FlowType);
            Assert.AreEqual("Stable", original.Status);
            Assert.AreEqual(0, original.Meta.Count);
            Assert.AreEqual(0, original.Upstream.Count);
        }

        [TestMethod]
        public void PreventInvalidParameters()
        {
            Assert.ThrowsException<ArgumentException>(() => new BranchReserve(
                reserveType: "Normal",
                flowType: "Auto",
                status: "Stable",
                upstream: null!, // This causes the error
                includedBranches: ImmutableSortedDictionary<string, BranchReserveBranch>.Empty,
                outputCommit: BranchReserve.EmptyCommit,
                meta: ImmutableSortedDictionary<string, string>.Empty));
            Assert.ThrowsException<ArgumentException>(() => new BranchReserve(
                reserveType: "Normal",
                flowType: "Auto",
                status: "Stable",
                upstream: ImmutableSortedDictionary<string, UpstreamReserve>.Empty,
                includedBranches: null!, // This causes the error
                outputCommit: BranchReserve.EmptyCommit,
                meta: ImmutableSortedDictionary<string, string>.Empty));
            Assert.ThrowsException<ArgumentException>(() => new BranchReserve(
                reserveType: "Normal",
                flowType: "Auto",
                status: "Stable",
                upstream: ImmutableSortedDictionary<string, UpstreamReserve>.Empty,
                includedBranches: ImmutableSortedDictionary<string, BranchReserveBranch>.Empty,
                outputCommit: "", // This causes the error
                meta: ImmutableSortedDictionary<string, string>.Empty));
        }
    }
}
