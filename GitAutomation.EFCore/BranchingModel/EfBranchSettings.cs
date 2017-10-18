﻿using GitAutomation.BranchSettings;
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

        public IObservable<ImmutableList<BranchGroupDetails>> GetConfiguredBranches()
        {
            return notifiers.GetAnyNotification().StartWith(Unit.Default)
                .SelectMany(_ => WithContext(GetConfiguredBranchesOnce));
        }

        private Task<ImmutableList<BranchGroupDetails>> GetConfiguredBranchesOnce(BranchingContext context)
        {
            return (from branch in context.BranchGroup
                    select branch).AsNoTracking().ToArrayAsync()
                .ContinueWith(result => result.Result.Select(EfBranchGroupToModel).ToImmutableList());
        }

        private static BranchGroupDetails EfBranchGroupToModel(BranchGroup branch)
        {
            if (branch == null)
            {
                return null;
            }
            return new BranchGroupDetails
            {
                GroupName = branch.GroupName,
                RecreateFromUpstream = branch.RecreateFromUpstream,
                BranchType = Enum.TryParse<BranchGroupType>(branch.BranchType, out var branchType)
                    ? branchType
                    : BranchGroupType.Feature,
            };
        }

        public IObservable<BranchGroupDetails> GetBranchBasicDetails(string branchName)
        {
            // TODO - better notification
            return notifiers.GetAnyNotification().StartWith(Unit.Default)
                .SelectMany(_ => WithContext(GetBranchDetailOnce(branchName)));
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

        private BranchGroupCompleteData BuildCompleteData(string branchName, BranchGroupDetails settings, ImmutableDictionary<string, ImmutableList<string>> downstream, ImmutableDictionary<string, ImmutableList<string>> upstream)
        {
            return new BranchGroupCompleteData
            {
                GroupName = branchName,
                RecreateFromUpstream = settings.RecreateFromUpstream,
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

        private Func<BranchingContext, Task<BranchGroupDetails>> GetBranchDetailOnce(string branchName)
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
                RecreateFromUpstream = false,
                BranchType = BranchGroupType.Feature.ToString("g"),
            };
        }

        private BranchGroupDetails DefaultBranchGroup(string branchName)
        {
            return new BranchGroupDetails
            {
                GroupName = branchName,
                RecreateFromUpstream = false,
                BranchType = BranchGroupType.Feature,
            };
        }
        
        public IObservable<ImmutableList<BranchGroupDetails>> GetDownstreamBranches(string branchName)
        {
            return notifiers.GetDownstreamBranchesChangedNotifier(upstreamBranch: branchName).StartWith(Unit.Default)
                .SelectMany(_ => WithContext(GetDownstreamBranchesOnce(branchName)));
        }

        private Func<BranchingContext, Task<ImmutableList<BranchGroupDetails>>> GetDownstreamBranchesOnce(string branchName)
        {
            return context =>
            {
                return (from branch in context.BranchGroup
                        where branch.GroupName == branchName
                        from downstream in branch.BranchStreamUpstreamBranchNavigation
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

        public IObservable<ImmutableList<BranchGroupDetails>> GetAllDownstreamBranches(string branchName)
        {
            // TODO - better notifications
            return notifiers.GetAnyNotification().StartWith(Unit.Default)
                .SelectMany(_ => WithContext(GetAllDownstreamBranchesOnce(branchName)));
        }

        private Func<BranchingContext, Task<ImmutableList<BranchGroupDetails>>> GetAllDownstreamBranchesOnce(string branchName)
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

        public IObservable<ImmutableList<BranchGroupDetails>> GetUpstreamBranches(string branchName)
        {
            return notifiers.GetUpstreamBranchesChangedNotifier(downstreamBranch: branchName).StartWith(Unit.Default)
                .SelectMany(_ => WithContext(GetUpstreamBranchesOnce(branchName)));
        }

        private Func<BranchingContext, Task<ImmutableList<BranchGroupDetails>>> GetUpstreamBranchesOnce(string branchName)
        {
            return context =>
            {
                return (from branch in context.BranchGroup
                        where branch.GroupName == branchName
                        from downstream in branch.BranchStreamDownstreamBranchNavigation
                        select downstream.UpstreamBranchNavigation).AsNoTracking().ToArrayAsync()
                .ContinueWith(task => task.Result.Select(EfBranchGroupToModel).ToImmutableList());
            };
        }

        public IObservable<ImmutableList<BranchGroupDetails>> GetAllUpstreamBranches(string branchName)
        {
            // TODO - better notifications
            return notifiers.GetAnyNotification().StartWith(Unit.Default)
                .SelectMany(_ => WithContext(GetAllUpstreamBranchesOnce(branchName)));
        }

        private Func<BranchingContext, Task<ImmutableList<BranchGroupDetails>>> GetAllUpstreamBranchesOnce(string branchName)
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
                           && integrationBranch.BranchStreamDownstreamBranchNavigation.Any(bs => bs.UpstreamBranch == branches[0])
                           && integrationBranch.BranchStreamDownstreamBranchNavigation.Any(bs => bs.UpstreamBranch == branches[1])
                           && integrationBranch.BranchStreamDownstreamBranchNavigation.Count == 2
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

        public void UpdateBranchSetting(string branchName, bool recreateFromUpstream, BranchGroupType branchType, IUnitOfWork work)
        {
            PrepareSecurityContextUnitOfWork(work);
            work.Defer(async sp =>
            {
                var context = GetContext(sp);
                var target = await context.BranchGroup.AddIfNotExists(new BranchGroup
                {
                    GroupName = branchName,
                    RecreateFromUpstream = recreateFromUpstream,
                    BranchType = branchType.ToString("g")
                }, branch => branch.GroupName == branchName);
                target.RecreateFromUpstream = recreateFromUpstream;
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
                    RecreateFromUpstream = false,
                    BranchType = BranchGroupType.ServiceLine.ToString("g"),
                }, branch => branch.GroupName == targetBranch);

                var newDownstream = (await (from branch in context.BranchStream
                                          where branchesToRemove.Contains(branch.UpstreamBranch) // where there's something flowing from an upstream branch
                                            && !branchesToRemove.Contains(branch.DownstreamBranch) // that is being deleted
                                          group branch.DownstreamBranchNavigation by branch.DownstreamBranch into downstreamBranches
                                          select downstreamBranches.Key).ToArrayAsync())
                    .Distinct();

                var oldDownstream = (await (from branch in context.BranchStream
                                            where branch.UpstreamBranch == targetBranch
                                            select branch.DownstreamBranch).ToArrayAsync());
                context.BranchStream.AddRange(
                    from downstream in newDownstream
                    where downstream != targetBranch
                    where !oldDownstream.Contains(downstream)
                    select new BranchStream { DownstreamBranch = downstream, UpstreamBranch = targetBranch }
                );
                
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

                context.BranchGroup.Remove(await (from branch in context.BranchGroup
                                                  where branch.GroupName == deletingBranch
                                                  select branch
                                                  ).FirstOrDefaultAsync());
            });
        }

        public void CreateIntegrationBranch(string branchA, string branchB, string integrationGroupName, IUnitOfWork work)
        {
            UpdateBranchSetting(integrationGroupName, false, BranchGroupType.Integration, work);
            AddBranchPropagation(branchA, integrationGroupName, work);
            AddBranchPropagation(branchB, integrationGroupName, work);
        }

        private void PrepareSecurityContextUnitOfWork(IUnitOfWork work)
        {
            work.PrepareAndFinalize<ConnectionManagement<BranchingContext>>();
        }

        private BranchingContext GetContext(IServiceProvider scope) =>
            scope.GetRequiredService<BranchingContext>();

        private async Task<T> WithContext<T>(Func<BranchingContext, Task<T>> target)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                return await target(GetContext(scope.ServiceProvider));
            }
        }
    }
}