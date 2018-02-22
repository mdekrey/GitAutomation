using GitAutomation.BranchSettings;
using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Reactive.Linq;

namespace GitAutomation.EFCore.BranchingModel
{
    class EfBranchSettingsAccessor : IBranchSettingsAccessor
    {
        private readonly BranchingContext context;

        public EfBranchSettingsAccessor(BranchingContext context)
        {
            this.context = context;
        }

        private IQueryable<BranchGroup> BranchGroups => context.BranchGroup.AsNoTracking();
        private IQueryable<BranchStream> BranchConnections => context.BranchStream.AsNoTracking();

        public Task<ImmutableList<BranchSettings.BranchGroup>> GetAllBranchGroups()
        {
            return BranchGroups.ToArrayAsync()
                .ContinueWith(result => result.Result.Select(EfBranchGroupToModel).ToImmutableList());
        }


        public Task<ImmutableDictionary<string, BranchSettings.BranchGroup>> GetBranchGroups(params string[] groupNames)
        {
            return (from branch in BranchGroups
                    where groupNames.Contains(branch.GroupName)
                    select branch).ToDictionaryAsync(b => b.GroupName, EfBranchGroupToModel)
                .ContinueWith(t => t.Result.ToImmutableDictionary());
        }


        private static BranchSettings.BranchGroup EfBranchGroupToModel(BranchGroup branch)
        {
            return branch.ToModel();
        }

        public Task<ImmutableDictionary<string, ImmutableList<string>>> GetDownstreamBranchGroups(params string[] groupNames)
        {
            return (from connection in BranchConnections
                    where groupNames.Contains(connection.UpstreamBranch)
                    group connection.DownstreamBranch by connection.UpstreamBranch)
                .ToDictionaryAsync(e => e.Key, e => e.ToImmutableList())
                .ContinueWith(t => t.Result.ToImmutableDictionary());
        }

        public Task<ImmutableDictionary<string, ImmutableList<string>>> GetUpstreamBranchGroups(params string[] groupNames)
        {
            return (from connection in BranchConnections
                    where groupNames.Contains(connection.DownstreamBranch)
                    group connection.UpstreamBranch by connection.DownstreamBranch)
                .ToDictionaryAsync(e => e.Key, e => e.ToImmutableList())
                .ContinueWith(t => t.Result.ToImmutableDictionary());
        }
        
        public async Task<Consolidation> CalculateConsolidation(IEnumerable<string> originalBranchesToRemove, string targetBranch)
        {
            var branchesToRemove = new HashSet<string>(originalBranchesToRemove);
            // Biggest issue here will be is if there is a hole (for some reason) in the middle of branchesToRemove.
            // I think we should detect the issue and block in that case, waiting for human intervention

            var connections = await BranchConnections.AsNoTracking().ToArrayAsync();

            // Key is the parent being lost
            var losingAParent = (from connection in connections
                                 where branchesToRemove.Contains(connection.UpstreamBranch)
                                 group connection.DownstreamBranch by connection.UpstreamBranch).ToDictionary(group => group.Key, group => group.ToImmutableList());

            var losingAChild = (from connection in connections
                                where branchesToRemove.Contains(connection.DownstreamBranch)
                                group connection.UpstreamBranch by connection.DownstreamBranch).ToDictionary(group => group.Key, group => group.ToImmutableList());

            var allDownstreamFromTarget = GetAllDownstreamFrom(targetBranch, connections);

            var result = new Consolidation
            {
                RemoveFromLinks = (from entry in (from connection in connections
                                                  where branchesToRemove.Contains(connection.DownstreamBranch) || branchesToRemove.Contains(connection.UpstreamBranch)
                                                  select connection)
                                   group entry.DownstreamBranch by entry.UpstreamBranch).ToImmutableDictionary(group => group.Key, group => group.ToImmutableList()),
                AddToLinks = (from entry in losingAParent
                              from downstream in entry.Value
                              where !branchesToRemove.Contains(downstream)
                              group downstream by targetBranch).ToImmutableDictionary(group => group.Key, group => group.ToImmutableList())
            };

            result.Disallow = 
                // Disallow if anything was added downstream that was already upstream
                result.AddToLinks.Any(e => GetAllUpstreamFrom(e.Key, connections).Intersect(e.Value).Any())
                // Disallow if anything is added downstream of itself
                || result.AddToLinks.Any(e => e.Value.Contains(e.Key))
                // Disallow if anything was removed AND added
                || result.RemoveFromLinks.Any(r => result.AddToLinks.ContainsKey(r.Key) && result.AddToLinks[r.Key].Intersect(r.Value).Any());

            return result;
            //var allDownstream = await (from b in branchesToRemove.ToObservable()
            //                           from d in GetAllDownstreamBranchesOnce(b)(context)
            //                           from branch in d
            //                           select branch.GroupName).Distinct().ToArray();

            //var newDownstream = (await (from branch in context.BranchStream
            //                            where branchesToRemove.Contains(branch.UpstreamBranch) // where there's something flowing from an upstream branch
            //                              && !branchesToRemove.Contains(branch.DownstreamBranch) // that is being deleted
            //                            group branch.DownstreamBranchNavigation by branch.DownstreamBranch into downstreamBranches
            //                            select downstreamBranches.Key).ToArrayAsync())
            //    .Except(allDownstream)
            //    .Distinct();

            //var allUpstream = await (from b in branchesToRemove.ToObservable()
            //                         from d in GetAllUpstreamBranchesOnce(b)(context)
            //                         from branch in d
            //                         select branch.GroupName).Distinct().ToArray();

            //var newUpstream = (await (from branch in context.BranchStream
            //                          where branchesToRemove.Contains(branch.DownstreamBranch) // where there's something flowing from an upstream branch
            //                            && !branchesToRemove.Contains(branch.UpstreamBranch) // that is being deleted
            //                          group branch.UpstreamBranchNavigation by branch.UpstreamBranch into upstreamBranches
            //                          select upstreamBranches.Key).ToArrayAsync())
            //    .Except(allUpstream)
            //    .Distinct();

        }

        private ImmutableHashSet<string> GetAllDownstreamFrom(string targetBranch, BranchStream[] connections)
        {
            var result = new HashSet<string>();
            var queue = new Queue<string>(new[] { targetBranch });

            while (queue.Any())
            {
                var current = queue.Dequeue();
                if (result.Contains(current))
                {
                    continue;
                }
                result.Add(current);
                foreach (var connection in connections.Where(s => s.UpstreamBranch == current))
                {
                    queue.Enqueue(connection.DownstreamBranch);
                }
            }
            return result.ToImmutableHashSet();
        }

        private ImmutableHashSet<string> GetAllUpstreamFrom(string targetBranch, BranchStream[] connections)
        {
            var result = new HashSet<string>();
            var queue = new Queue<string>(new[] { targetBranch });

            while (queue.Any())
            {
                var current = queue.Dequeue();
                if (result.Contains(current))
                {
                    continue;
                }
                result.Add(current);
                foreach (var connection in connections.Where(s => s.UpstreamBranch == current))
                {
                    queue.Enqueue(connection.UpstreamBranch);
                }
            }
            return result.ToImmutableHashSet();
        }
    }
}
