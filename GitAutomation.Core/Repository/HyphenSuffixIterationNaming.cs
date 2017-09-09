using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;

namespace GitAutomation.Repository
{
    class HyphenSuffixIterationNaming : IBranchIterationNamingConvention
    {
        public IObservable<string> GetBranchNameIterations(string branchName, IEnumerable<string> existingNames)
        {
            var currentIteration = GetCurrentIteration(branchName, existingNames);
            var result = Observable.Range(
                currentIteration + 1,
                int.MaxValue
            ).Select(i => $"{branchName}-{i}");

            if (existingNames.Any())
            {
                return result;
            }
            else
            {
                return result.StartWith(branchName);
            }
        }

        private int GetCurrentIteration(string branchName, IEnumerable<string> existingNames)
        {
            return existingNames.Select(existingName =>
                TryGetIterationNumber(branchName, existingName, out var temp)
                    ? temp
                    : 0
            ).DefaultIfEmpty(0).Max();
        }

        public string GetLatestBranchNameIteration(string branchName, IEnumerable<string> existingNames)
        {
            return existingNames.OrderBy(existingName =>
                TryGetIterationNumber(branchName, existingName, out var temp)
                    ? temp
                    : 0
            ).FirstOrDefault();
        }

        public bool IsBranchIteration(string originalName, string candidateName)
        {
            return candidateName.StartsWith(originalName)
                && candidateName[originalName.Length] == '-'
                && int.TryParse(candidateName.Substring(originalName.Length + 1), out var temp);
        }

        private bool TryGetIterationNumber(string originalName, string candidateName, out int iterationNumber)
        {
            if (candidateName.StartsWith(originalName)
                && candidateName[originalName.Length] == '-'
                && int.TryParse(candidateName.Substring(originalName.Length + 1), out iterationNumber))
            {
                return true;
            }
            iterationNumber = 0;
            return false;
        }
    }
}
