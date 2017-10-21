using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading.Tasks;

namespace GitAutomation.BranchSettings
{
    public interface IBranchSettingsAccessor
    {
        Task<ImmutableList<BranchGroup>> GetAllBranchGroups();
        Task<ImmutableDictionary<string, BranchGroup>> GetBranchGroups(params string[] groupNames);
    }
}
