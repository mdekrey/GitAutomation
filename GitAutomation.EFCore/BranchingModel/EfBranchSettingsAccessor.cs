using GitAutomation.BranchSettings;
using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;

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
            if (branch == null)
            {
                return null;
            }
            return new BranchSettings.BranchGroup
            {
                GroupName = branch.GroupName,
                RecreateFromUpstream = branch.RecreateFromUpstream,
                BranchType = Enum.TryParse<BranchGroupType>(branch.BranchType, out var branchType)
                    ? branchType
                    : BranchGroupType.Feature,
            };
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
    }
}
