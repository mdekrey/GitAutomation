using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
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
                Upstream = new HashSet<string>(),
                LastCommit = BranchReserve.EmptyCommit,
                Meta = new Dictionary<string, object>()
            }.Build();
            var builder = original.ToBuilder();
            builder.ReserveType = ("ReleaseCandidate");
            builder.FlowType = ("Manual");
            builder.Status = ("OutOfDate");

            var actual = builder.Build();
            Assert.AreEqual("ReleaseCandidate", actual.ReserveType);
            Assert.AreEqual("Manual", actual.FlowType);
            Assert.AreEqual("OutOfDate", actual.Status);
            Assert.AreEqual("Normal", original.ReserveType);
            Assert.AreEqual("Auto", original.FlowType);
            Assert.AreEqual("Stable", original.Status);
        }

        [TestMethod]
        public void PreventInvalidParameters()
        {
            Assert.ThrowsException<ArgumentException>(() => new BranchReserve("", "Auto", "Stable", ImmutableSortedSet<string>.Empty, BranchReserve.EmptyCommit, ImmutableSortedDictionary<string, object>.Empty));
            Assert.ThrowsException<ArgumentException>(() => new BranchReserve("Normal", "", "Stable", ImmutableSortedSet<string>.Empty, BranchReserve.EmptyCommit, ImmutableSortedDictionary<string, object>.Empty));
            Assert.ThrowsException<ArgumentException>(() => new BranchReserve("Normal", "Auto", "", ImmutableSortedSet<string>.Empty, BranchReserve.EmptyCommit, ImmutableSortedDictionary<string, object>.Empty));
            Assert.ThrowsException<ArgumentException>(() => new BranchReserve("Normal", "Auto", "Stable", null, BranchReserve.EmptyCommit, ImmutableSortedDictionary<string, object>.Empty));
            Assert.ThrowsException<ArgumentException>(() => new BranchReserve("Normal", "Auto", "Stable", ImmutableSortedSet<string>.Empty, "", ImmutableSortedDictionary<string, object>.Empty));
        }
    }
}
