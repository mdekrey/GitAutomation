using GitAutomation.DomainModels;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace GitAutomation
{
    public class IntegrationReserveUtilities : IIntegrationReserveUtilities
    {
        public IEnumerable<IStandardAction> AddUpstreamConflicts(string name, List<(string, string)> conflictingUpstream, ImmutableDictionary<string, BranchReserve> reserves)
        {
            var sorted = (from entry in conflictingUpstream
                          select entry.Item1.CompareTo(entry.Item2) < 0 ? (entry.Item1, entry.Item2) : (entry.Item2, entry.Item1) into entry
                          orderby entry.Item1, entry.Item2
                          select entry).ToArray();
            var reverseLookup = new Dictionary<string, string>();
            var conflictSubgraphs = new Dictionary<string, HashSet<string>>();
            foreach (var conflict in sorted)
            {
                var knownKey = reverseLookup.ContainsKey(conflict.Item1) ? reverseLookup[conflict.Item1] : conflict.Item1;
                conflictSubgraphs[knownKey] = conflictSubgraphs.ContainsKey(knownKey) ? conflictSubgraphs[knownKey] : new HashSet<string>();
                conflictSubgraphs[knownKey].Add(conflict.Item1);
                conflictSubgraphs[knownKey].Add(conflict.Item2);
                reverseLookup[conflict.Item2] = knownKey;
            }
            var branchGraph = new IntegrationGraph(reserves);
            foreach (var subgraph in conflictSubgraphs.Values)
            {
                var reserve = branchGraph.FindIntegrationReserve(subgraph);
                if (reserve == null)
                {
                    reserve = GetIntegrationReserveName(subgraph, reserves);
                    yield return new DomainModels.Actions.CreateReserveAction { FlowType = "Automatic", Upstream = subgraph.ToArray(), Type = "integration", Name = reserve };
                }
                yield return new DomainModels.Actions.AddUpstreamReserveAction { Upstream = reserve, Target = name };
            }
        }

        protected virtual string GetIntegrationReserveName(HashSet<string> subgraph, ImmutableDictionary<string, BranchReserve> reserves) =>
            $"integ/{string.Join('_', subgraph.OrderBy(v => v))}";

        class IntegrationGraph
        {
            private Lazy<ImmutableDictionary<string, ImmutableHashSet<string>>> integrationFeatures;

            public IntegrationGraph(ImmutableDictionary<string, BranchReserve> reserves)
            {
                this.integrationFeatures = new Lazy<ImmutableDictionary<string, ImmutableHashSet<string>>>(() =>
                {
                    var queue = new Queue<string>(from kvp in reserves
                                                  where kvp.Value.ReserveType == "Integration"
                                                  select kvp.Key);
                    var result = new Dictionary<string, ImmutableHashSet<string>>();
                    while (queue.Count > 0)
                    {
                        var current = queue.Dequeue();
                        var reserve = reserves[current];
                        var realFeatures = (from upstream in reserve.Upstream.Keys
                                           from entry in upstream switch
                                           {
                                               var u when result.ContainsKey(u) => (IEnumerable<string?>)result[u],
                                               var u when reserves.ContainsKey(u) && reserves[u].ReserveType != "Integration" => new[] { u },
                                               _ => new string?[] { null }
                                           }
                                           select entry).Distinct().ToArray();
                        if (realFeatures.Any(f => f == null))
                        {
                            queue.Enqueue(current);
                        }
                        else
                        {
                            result.Add(current, realFeatures.ToImmutableHashSet());
                        }
                    }
                    return result.ToImmutableDictionary();
                });
            }


            internal string? FindIntegrationReserve(HashSet<string> subgraph)
            {
                return integrationFeatures.Value.FirstOrDefault(kvp => kvp.Value.SetEquals(subgraph)).Key;
            }
        }
    }
}
