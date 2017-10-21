using DataLoader;
using GitAutomation.BranchSettings;
using System;
using System.Collections.Generic;
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
            return loadContext.Factory.GetOrCreateLoader<string, BranchGroup>("GetBranchGroups", async keys => {
                var result = await branchSettings.GetBranchGroups(keys.ToArray());
                return result.ToDictionary(e => e.Key, e => e.Value);
            }).LoadAsync(name);
        }
    }
}
