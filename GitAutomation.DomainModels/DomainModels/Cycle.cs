using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace GitAutomation.DomainModels
{
    public sealed class Cycle
    {
        private readonly List<string> members;
        private bool IsComplete;

        public ReadOnlyCollection<string> Members { get; }


        internal Cycle(string firstMember, string secondMember)
        {
            this.members = new List<string> { firstMember, secondMember };
            this.Members = new ReadOnlyCollection<string>(this.members);
        }

        internal void Build(string member)
        {
            if (!IsComplete)
            {
                this.IsComplete = member == members[0];
                this.members.Add(member);
            }
        }

        public override string ToString()
        {
            return string.Join(" -> ", members);
        }


        public static IEnumerable<Cycle> FindAllCycles(RepositoryStructure repo)
        {
            HashSet<string> alreadyVisited = new HashSet<string>();
            var firstNode = repo.BranchReserves.Keys.First();
            alreadyVisited.Add(firstNode);
            return FindAllCycles(alreadyVisited, repo, firstNode);
        }

        private static IEnumerable<Cycle> FindAllCycles(HashSet<string> alreadyVisited, RepositoryStructure repo, string currentNode)
        {
            var upstreams = repo.BranchReserves[currentNode].Upstream.Keys.ToArray();
            for (int i = 0; i < upstreams.Length; i++)
            {
                var upstream = upstreams[i];
                if (alreadyVisited.Contains(upstream))
                {
                    yield return new Cycle(upstream, currentNode);
                }
                else
                {
                    var newSet = i == repo.BranchReserves[currentNode].Upstream.Count - 1
                        ? alreadyVisited // last one can use the existing hashset; it'd be GC'd otherwise
                        : new HashSet<string>(alreadyVisited);
                    newSet.Add(upstream);
                    foreach (Cycle c in FindAllCycles(newSet, repo, upstream))
                    {
                        c.Build(currentNode);
                        yield return c;
                    }
                }
            }
        }

    }
}
