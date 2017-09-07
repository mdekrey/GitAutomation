using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Text;

namespace GitAutomation.Repository
{
    class HyphenSuffixIterationNaming : IBranchIterationNamingConvention
    {
        public IObservable<string> GetBranchNameIterations(string branchName)
        {
            return Observable.Range(1, int.MaxValue).Select(i => $"{branchName}-{i}").StartWith(branchName);
        }

        public bool IsBranchIteration(string originalName, string candidateName)
        {
            return candidateName.StartsWith(originalName)
                && candidateName[originalName.Length] == '-'
                && int.TryParse(candidateName.Substring(originalName.Length + 1), out var temp);
        }
    }
}
