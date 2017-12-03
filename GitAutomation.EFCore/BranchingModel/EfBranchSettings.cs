using GitAutomation.BranchSettings;
using GitAutomation.Work;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitAutomation.EFCore.BranchingModel
{
    class EfBranchSettings : IBranchSettings
    {
        private readonly IBranchSettingsNotifiers notifiers;
        private readonly IServiceProvider serviceProvider;

        public EfBranchSettings(IBranchSettingsNotifiers notifiers, IServiceProvider serviceProvider)
        {
            this.notifiers = notifiers;
            this.serviceProvider = serviceProvider;
        }

        public IObservable<ImmutableList<BranchSettings.BranchGroup>> GetConfiguredBranches()
        {
            return notifiers.GetAnyNotification().StartWith(Unit.Default)
                .SelectMany(_ => WithAccessor(a => a.GetAllBranchGroups()));
        }

        private Task<ImmutableList<BranchSettings.BranchGroup>> GetConfiguredBranchesOnce(BranchingContext context)
        {
            return (from branch in context.BranchGroup
                    select branch).AsNoTracking().ToArrayAsync()
                .ContinueWith(result => result.Result.Select(EfBranchGroupToModel).ToImmutableList());
        }

        private static BranchSettings.BranchGroup EfBranchGroupToModel(BranchGroup branch)
        {
            return branch.ToModel();
        }

        public IObservable<BranchSettings.BranchGroup> GetBranchBasicDetails(string branchName)
        {
            // TODO - better notification
            return notifiers.GetAnyNotification().StartWith(Unit.Default)
                .SelectMany(_ => WithAccessor(a => a.GetBranchGroups(branchName)))
                .Select(grouped => grouped.ContainsKey(branchName) ? grouped[branchName] : null);
        }

        public IObservable<BranchGroupCompleteData> GetBranchDetails(string branchName)
        {
            // TODO - better notification
            return notifiers.GetAnyNotification().StartWith(Unit.Default)
                .SelectMany(_ => WithContext(async context =>
                {
                    var settings = (await GetBranchDetailOnce(branchName)(context).ConfigureAwait(false))
                        ?? DefaultBranchGroup(branchName);
                    var hierarchies = GetBranchHierarchies(await (from entry in context.BranchStream
                                                                  select entry).ToArrayAsync().ConfigureAwait(false));
                    return BuildCompleteData(branchName, settings, hierarchies.downstream, hierarchies.upstream);
                }));
        }

        private static (ImmutableDictionary<string, ImmutableList<string>> downstream, ImmutableDictionary<string, ImmutableList<string>> upstream) GetBranchHierarchies(BranchStream[] branches)
        {
            var downstream = (from entry in branches
                          group entry.DownstreamBranch by entry.UpstreamBranch)
                .ToImmutableDictionary(e => e.Key, e => e.ToImmutableList());
            var upstream = (from entry in branches
                        group entry.UpstreamBranch by entry.DownstreamBranch)
                .ToImmutableDictionary(e => e.Key, e => e.ToImmutableList());
            return (downstream, upstream);
        }

        private BranchGroupCompleteData BuildCompleteData(string branchName, BranchSettings.BranchGroup settings, ImmutableDictionary<string, ImmutableList<string>> downstream, ImmutableDictionary<string, ImmutableList<string>> upstream)
        {
            return new BranchGroupCompleteData
            {
                GroupName = branchName,
                UpstreamMergePolicy = settings.UpstreamMergePolicy,
                BranchType = settings.BranchType,
                DirectDownstreamBranchGroups = downstream.ContainsKey(branchName) ? downstream[branchName] : ImmutableList<string>.Empty,
                DirectUpstreamBranchGroups = upstream.ContainsKey(branchName) ? upstream[branchName] : ImmutableList<string>.Empty,
                DownstreamBranchGroups = TraverseHierarchy(downstream, branchName, out var downstreamDepth).ToImmutableList(),
                UpstreamBranchGroups = TraverseHierarchy(upstream, branchName, out var upstreamDepth).ToImmutableList(),
                HierarchyDepth = upstreamDepth,
            };
        }

        private HashSet<string> TraverseHierarchy(ImmutableDictionary<string, ImmutableList<string>> source, string key, out int depth)
        {
            depth = 0;
            var accumulator = new HashSet<string>();
            var newEntries = new Queue<(string key, int depth)>(new[] { (key, 0) });
            while (newEntries.Count > 0)
            {
                var entry = newEntries.Dequeue();
                accumulator.Add(entry.key);
                depth = Math.Max(entry.depth, depth);
                if (source.ContainsKey(entry.key))
                {
                    foreach (var newEntry in source[entry.key])
                    {
                        newEntries.Enqueue((newEntry, entry.depth + 1));
                    }
                }
            }
            accumulator.Remove(key);
            return accumulator;
        }

        private Func<BranchingContext, Task<BranchSettings.BranchGroup>> GetBranchDetailOnce(string branchName)
        {
            return context =>
            {
                return (from branch in context.BranchGroup
                        where branch.GroupName == branchName
                        select branch).FirstOrDefaultAsync()
                    .ContinueWith(task => EfBranchGroupToModel(task.Result));
            };
        }

        private BranchGroup DefaultEfBranchGroup(string branchName)
        {
            return new BranchGroup
            {
                GroupName = branchName,
                UpstreamMergePolicy = UpstreamMergePolicy.None.ToString("g"),
                BranchType = BranchGroupType.Feature.ToString("g"),
            };
        }

        private BranchSettings.BranchGroup DefaultBranchGroup(string branchName)
        {
            return new BranchSettings.BranchGroup
            {
                GroupName = branchName,
                UpstreamMergePolicy = UpstreamMergePolicy.None,
                BranchType = BranchGroupType.Feature,
            };
        }

        public IObservable<ImmutableList<BranchSettings.BranchGroup>> GetDownstreamBranches(string branchName)
        {
            return notifiers.GetDownstreamBranchesChangedNotifier(upstreamBranch: branchName).StartWith(Unit.Default)
                .SelectMany(_ => WithContext(GetDownstreamBranchesOnce(branchName)));
        }

        private Func<BranchingContext, Task<ImmutableList<BranchSettings.BranchGroup>>> GetDownstreamBranchesOnce(string branchName)
        {
            return context =>
            {
                return (from branch in context.BranchGroup
                        where branch.GroupName == branchName
                        from downstream in branch.DownstreamBranchConnections
                        select downstream.DownstreamBranchNavigation).AsNoTracking().ToArrayAsync()
                .ContinueWith(task => task.Result.Select(EfBranchGroupToModel).ToImmutableList());
            };
        }

        public IObservable<ImmutableList<BranchGroupCompleteData>> GetAllDownstreamBranches()
        {
            // TODO - better notifications
            return notifiers.GetAnyNotification().StartWith(Unit.Default)
                .SelectMany(_ => WithContext(GetAllDownstreamBranchesOnce()));
        }

        private Func<BranchingContext, Task<ImmutableList<BranchGroupCompleteData>>> GetAllDownstreamBranchesOnce()
        {
            return async context =>
            {
                var hierarchies = GetBranchHierarchies(await (from entry in context.BranchStream
                                                              select entry).ToArrayAsync().ConfigureAwait(false));
                var configured = await GetConfiguredBranchesOnce(context).ConfigureAwait(false);

                return configured.Select(bg =>
                {
                    return BuildCompleteData(bg.GroupName, bg, hierarchies.downstream, hierarchies.upstream);
                }).ToImmutableList();
            };
        }

        public IObservable<ImmutableList<BranchSettings.BranchGroup>> GetAllDownstreamBranches(string branchName)
        {
            // TODO - better notifications
            return notifiers.GetAnyNotification().StartWith(Unit.Default)
                .SelectMany(_ => WithContext(GetAllDownstreamBranchesOnce(branchName)));
        }

        private Func<BranchingContext, Task<ImmutableList<BranchSettings.BranchGroup>>> GetAllDownstreamBranchesOnce(string branchName)
        {
            return async context =>
            {
                var hierarchies = GetBranchHierarchies(await (from entry in context.BranchStream
                                                              select entry).ToArrayAsync().ConfigureAwait(false));
                var allDownstream = TraverseHierarchy(hierarchies.downstream, branchName, out var depth);
                return (await (from branch in context.BranchGroup
                               where allDownstream.Contains(branch.GroupName)
                               select branch).ToArrayAsync().ConfigureAwait(false))
                    .Select(EfBranchGroupToModel).ToImmutableList();
            };
        }

        public IObservable<ImmutableList<BranchSettings.BranchGroup>> GetUpstreamBranches(string branchName)
        {
            return notifiers.GetUpstreamBranchesChangedNotifier(downstreamBranch: branchName).StartWith(Unit.Default)
                .SelectMany(_ => WithContext(GetUpstreamBranchesOnce(branchName)));
        }

        private Func<BranchingContext, Task<ImmutableList<BranchSettings.BranchGroup>>> GetUpstreamBranchesOnce(string branchName)
        {
            return context =>
            {
                return (from branch in context.BranchGroup
                        where branch.GroupName == branchName
                        from downstream in branch.UpstreamBranchConnections
                        select downstream.UpstreamBranchNavigation).AsNoTracking().ToArrayAsync()
                .ContinueWith(task => task.Result.Select(EfBranchGroupToModel).ToImmutableList());
            };
        }

        public IObservable<ImmutableList<BranchSettings.BranchGroup>> GetAllUpstreamBranches(string branchName)
        {
            // TODO - better notifications
            return notifiers.GetAnyNotification().StartWith(Unit.Default)
                .SelectMany(_ => WithContext(GetAllUpstreamBranchesOnce(branchName)));
        }

        private Func<BranchingContext, Task<ImmutableList<BranchSettings.BranchGroup>>> GetAllUpstreamBranchesOnce(string branchName)
        {
            return async context =>
            {
                var hierarchies = GetBranchHierarchies(await (from entry in context.BranchStream
                                                              select entry).ToArrayAsync().ConfigureAwait(false));
                var allDownstream = TraverseHierarchy(hierarchies.upstream, branchName, out var depth);
                return (await (from branch in context.BranchGroup
                               where allDownstream.Contains(branch.GroupName)
                               select branch).ToArrayAsync().ConfigureAwait(false))
                    .Select(EfBranchGroupToModel).ToImmutableList();
            };
        }

        public IObservable<ImmutableList<string>> GetAllUpstreamRemovableBranches(string branchName)
        {
            // TODO - better notifications
            return notifiers.GetAnyNotification().StartWith(Unit.Default)
                .SelectMany(_ => WithContext(GetAllUpstreamRemovableBranchesOnce(branchName)));
        }

        public Task<string> GetIntegrationBranch(string branchA, string branchB)
        {
            var branches = new[] { branchA, branchB }.OrderBy(a => a).ToArray();
            return WithContext(context =>
            {
                return (from integrationBranch in context.BranchGroup
                        where integrationBranch.BranchType == BranchGroupType.Integration.ToString("g")
                           && integrationBranch.UpstreamBranchConnections.Any(bs => bs.UpstreamBranch == branches[0])
                           && integrationBranch.UpstreamBranchConnections.Any(bs => bs.UpstreamBranch == branches[1])
                           && integrationBranch.UpstreamBranchConnections.Count == 2
                        select integrationBranch.GroupName).FirstOrDefaultAsync();
            });
        }


        private Func<BranchingContext, Task<ImmutableList<string>>> GetAllUpstreamRemovableBranchesOnce(string branchName)
        {
            return async context =>
            {

                var hierarchies = GetBranchHierarchies(await (from entry in context.BranchStream
                                                              where entry.UpstreamBranchNavigation.BranchType != BranchGroupType.ServiceLine.ToString("g")
                                                              select entry).ToArrayAsync().ConfigureAwait(false));
                return TraverseHierarchy(hierarchies.upstream, branchName, out var depth).ToImmutableList();
            };
        }

        public void UpdateBranchSetting(string branchName, UpstreamMergePolicy upstreamMergePolicy, BranchGroupType branchType, IUnitOfWork work)
        {
            PrepareSecurityContextUnitOfWork(work);
            work.Defer(async sp =>
            {
                var context = GetContext(sp);
                var target = await context.BranchGroup.AddIfNotExists(new BranchGroup
                {
                    GroupName = branchName,
                    UpstreamMergePolicy = upstreamMergePolicy.ToString("g"),
                    BranchType = branchType.ToString("g")
                }, branch => branch.GroupName == branchName);
                target.UpstreamMergePolicy = upstreamMergePolicy.ToString("g");
                target.BranchType = branchType.ToString("g");
            });
            // TODO - onCommit, notify changes
        }

        public void AddBranchPropagation(string upstreamBranch, string downstreamBranch, IUnitOfWork work)
        {
            PrepareSecurityContextUnitOfWork(work);
            work.Defer(async sp =>
            {
                var context = GetContext(sp);
                await context.BranchGroup.AddIfNotExists(DefaultEfBranchGroup(upstreamBranch), branch => branch.GroupName == upstreamBranch);
                await context.BranchGroup.AddIfNotExists(DefaultEfBranchGroup(downstreamBranch), branch => branch.GroupName == downstreamBranch);
                await context.BranchStream.AddIfNotExists(new BranchStream { DownstreamBranch = downstreamBranch, UpstreamBranch = upstreamBranch }, bs => bs.DownstreamBranch == downstreamBranch && bs.UpstreamBranch == upstreamBranch);
            });
            // TODO - onCommit, notify changes
        }

        public void RemoveBranchPropagation(string upstreamBranch, string downstreamBranch, IUnitOfWork work)
        {
            PrepareSecurityContextUnitOfWork(work);
            work.Defer(async sp =>
            {
                var context = GetContext(sp);
                context.BranchStream.Remove(
                    await context.BranchStream.FirstOrDefaultAsync(bs => bs.DownstreamBranch == downstreamBranch && bs.UpstreamBranch == upstreamBranch)
                );
            });
            // TODO - onCommit, notify changes
        }

        public void ConsolidateBranches(IEnumerable<string> branchesToRemove, string targetBranch, IUnitOfWork work)
        {
            PrepareSecurityContextUnitOfWork(work);
            work.Defer(async sp =>
            {
                var context = GetContext(sp);
                var serviceLine = await context.BranchGroup.AddIfNotExists(new BranchGroup
                {
                    GroupName = targetBranch,
                    UpstreamMergePolicy = UpstreamMergePolicy.None.ToString("g"),
                    BranchType = BranchGroupType.ServiceLine.ToString("g"),
                }, branch => branch.GroupName == targetBranch);

                var newDownstream = (await (from branch in context.BranchStream
                                          where branchesToRemove.Contains(branch.UpstreamBranch) // where there's something flowing from an upstream branch
                                            && !branchesToRemove.Contains(branch.DownstreamBranch) // that is being deleted
                                          group branch.DownstreamBranchNavigation by branch.DownstreamBranch into downstreamBranches
                                          select downstreamBranches.Key).ToArrayAsync())
                    .Distinct();

                foreach (var upstream in newDownstream.Where(upstream => upstream != targetBranch))
                {
                    await context.BranchStream.AddIfNotExists(new BranchStream { DownstreamBranch = upstream, UpstreamBranch = targetBranch }, b => b.DownstreamBranch == upstream && b.UpstreamBranch == targetBranch);
                }

                context.BranchStream.RemoveRange(await (from branch in context.BranchStream
                                                        where branchesToRemove.Contains(branch.UpstreamBranch)
                                                          || branchesToRemove.Contains(branch.DownstreamBranch)
                                                        select branch
                                                        ).ToArrayAsync());

                context.BranchGroup.RemoveRange(await (from branch in context.BranchGroup
                                                       where branchesToRemove.Contains(branch.GroupName)
                                                       select branch
                                                        ).ToArrayAsync());
            });
        }

        public void DeleteBranchSettings(string deletingBranch, IUnitOfWork work)
        {
            PrepareSecurityContextUnitOfWork(work);
            work.Defer(async sp =>
            {
                var context = GetContext(sp);
                context.BranchStream.RemoveRange(await (from branch in context.BranchStream
                                                        where branch.UpstreamBranch == deletingBranch
                                                          || branch.DownstreamBranch == deletingBranch
                                                        select branch
                                                        ).ToArrayAsync());

                var group = await (from branch in context.BranchGroup
                                   where branch.GroupName == deletingBranch
                                   select branch
                                                  ).FirstOrDefaultAsync();
                if (group != null)
                {
                    context.BranchGroup.Remove(group);
                }
            });
        }

        public void CreateIntegrationBranch(string branchA, string branchB, string integrationGroupName, IUnitOfWork work)
        {
            UpdateBranchSetting(integrationGroupName, UpstreamMergePolicy.None, BranchGroupType.Integration, work);
            AddBranchPropagation(branchA, integrationGroupName, work);
            AddBranchPropagation(branchB, integrationGroupName, work);
        }

        private void PrepareSecurityContextUnitOfWork(IUnitOfWork work)
        {
            work.PrepareAndFinalize<ConnectionManagement<BranchingContext>>();
        }

        private BranchingContext GetContext(IServiceProvider scope) =>
            scope.GetRequiredService<BranchingContext>();

        private IBranchSettingsAccessor GetAccessor(IServiceProvider scope) =>
            scope.GetRequiredService<IBranchSettingsAccessor>();

        private async Task<T> WithContext<T>(Func<BranchingContext, Task<T>> target)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                return await target(GetContext(scope.ServiceProvider));
            }
        }
        private async Task<T> WithAccessor<T>(Func<IBranchSettingsAccessor, Task<T>> target)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                return await target(GetAccessor(scope.ServiceProvider));
            }
        }
    }
}
