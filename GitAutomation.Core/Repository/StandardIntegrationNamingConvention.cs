using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;

namespace GitAutomation.Repository
{
    class StandardIntegrationNamingConvention : IIntegrationNamingConvention
    {
        public IObservable<string> GetIngtegrationBranchNameCandidates(string branchA, string branchB)
        {
            var branchAParts = branchA.Split('/');
            var branchBParts = branchB.Split('/');
            var sharedParts = branchAParts.Zip(branchBParts, (a, b) => a == b ? a : null).Where(v => v != null).ToArray();
            var remainingA = string.Join("/", branchAParts.Skip(sharedParts.Length));
            var remainingB = string.Join("/", branchBParts.Skip(sharedParts.Length));
            var shared = string.Join("/", sharedParts);
            var ideal = string.Join("/", new[] { "merge", shared, remainingA, remainingB }.Where(v => !string.IsNullOrEmpty(v)));
            var maxLength = ideal.Substring(0, Math.Min(100, ideal.Length));
            return Observable.Range(1, int.MaxValue).Select(i => $"{maxLength}-{i}").StartWith(maxLength);
        }
    }
}
