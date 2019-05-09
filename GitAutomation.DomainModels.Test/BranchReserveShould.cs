using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Immutable;

namespace GitAutomation.DomainModels.Test
{
    [TestClass]
    public class BranchReserveShould
    {
        [TestMethod]
        public void SupportBuildingFromScratch()
        {
            var builder = new BranchReserve.Builder()
                .SetReserveType("Normal")
                .SetFlowType("Auto")
                .SetStatus("Stable");

            var actual = builder.Build();
            Assert.AreEqual("Normal", actual.ReserveType);
            Assert.AreEqual("Auto", actual.FlowType);
            Assert.AreEqual("Stable", actual.Status);
        }

        [TestMethod]
        public void SupportUpdatingViaABuilder()
        {
            var original = new BranchReserve("Normal", "Auto", "Stable", ImmutableSortedSet<string>.Empty, BranchReserve.EmptyCommit);
            var builder = original.ToBuilder()
                .SetReserveType("ReleaseCandidate")
                .SetFlowType("Manual")
                .SetStatus("OutOfDate");

            var actual = builder.Build();
            Assert.AreEqual("ReleaseCandidate", actual.ReserveType);
            Assert.AreEqual("Manual", actual.FlowType);
            Assert.AreEqual("OutOfDate", actual.Status);
        }

        [TestMethod]
        public void PreventInvalidParameters()
        {
            Assert.ThrowsException<ArgumentException>(() => new BranchReserve("", "Auto", "Stable", ImmutableSortedSet<string>.Empty, BranchReserve.EmptyCommit));
            Assert.ThrowsException<ArgumentException>(() => new BranchReserve("Normal", "", "Stable", ImmutableSortedSet<string>.Empty, BranchReserve.EmptyCommit));
            Assert.ThrowsException<ArgumentException>(() => new BranchReserve("Normal", "Auto", "", ImmutableSortedSet<string>.Empty, BranchReserve.EmptyCommit));
            Assert.ThrowsException<ArgumentException>(() => new BranchReserve("Normal", "Auto", "Stable", null, BranchReserve.EmptyCommit));
            Assert.ThrowsException<ArgumentException>(() => new BranchReserve("Normal", "Auto", "Stable", ImmutableSortedSet<string>.Empty, ""));
        }
    }
}
