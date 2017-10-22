using DataLoader;
using GitAutomation.BranchSettings;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitAutomation.GraphQL
{
    class Loaders
    {
        private readonly DataLoaderContext loadContext;
        private readonly IBranchSettingsAccessor branchSettings;

        public Loaders(IDataLoaderContextAccessor loadContextAccessor, IBranchSettingsAccessor branchSettings)
        {
            this.loadContext = loadContextAccessor.LoadContext;
            this.branchSettings = branchSettings;
        }

        public Task<BranchGroup> LoadBranchGroup(string name)
        {
            return loadContext.Factory.GetOrCreateLoader<string, BranchGroup>("GetBranchGroup", async keys => {
                var result = await branchSettings.GetBranchGroups(keys.ToArray());
                return result.ToDictionary(e => e.Key, e => e.Value);
            }).LoadAsync(name);
        }

        public Task<ImmutableList<string>> LoadBranchGroups()
        {
            return loadContext.Factory.GetOrCreateLoader("GetBranchGroups", async () => {
                var result = await branchSettings.GetAllBranchGroups();
                return result.Select(group => group.GroupName).ToImmutableList();
            }).LoadAsync();
        }

        internal Task<ImmutableList<string>> LoadDownstreamBranches(string name)
        {
            return loadContext.Factory.GetOrCreateLoader<string, ImmutableList<string>>("GetDownstreamBranchGroups", async keys => {
                var result = await branchSettings.GetDownstreamBranchGroups(keys.ToArray());
                return result.ToDictionary(e => e.Key, e => e.Value);
            }).LoadAsync(name);
        }

        internal Task<ImmutableList<string>> LoadUpstreamBranches(string name)
        {
            return loadContext.Factory.GetOrCreateLoader<string, ImmutableList<string>>("GetUpstreamBranchGroups", async keys => {
                var result = await branchSettings.GetUpstreamBranchGroups(keys.ToArray());
                return result.ToDictionary(e => e.Key, e => e.Value);
            }).LoadAsync(name);
        }
    }
}
