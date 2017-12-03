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
                int.MaxValue - currentIteration
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
            return existingNames.OrderByDescending(existingName =>
                TryGetIterationNumber(branchName, existingName, out var temp)
                    ? temp
                    : 0
            ).FirstOrDefault();
        }

        public bool IsBranchIteration(string originalName, string candidateName)
        {
            return TryGetIterationNumber(originalName, candidateName, out var temp);
        }

        private static bool TryGetIterationNumber(string originalName, string candidateName, out int iterationNumber)
        {
            if (candidateName == originalName)
            {
                iterationNumber = 0;
                return true;
            }
            if (candidateName.StartsWith(originalName + "-")
                && int.TryParse(candidateName.Substring(originalName.Length + 1), out iterationNumber))
            {
                return true;
            }
            iterationNumber = 0;
            return false;
        }

        public IComparer<string> GetIterationNameComparer(string name)
        {
            return new BranchIterationComparer(name);
        }

        private class BranchIterationComparer : IComparer<string>
        {
            private string name;

            public BranchIterationComparer(string name)
            {
                this.name = name;
            }

            public int Compare(string x, string y)
            {
                return (TryGetIterationNumber(name, x, out var xIndex)
                    && TryGetIterationNumber(name, y, out var yIndex))
                    ? xIndex - yIndex
                    : 0;
            }
        }
    }
}
