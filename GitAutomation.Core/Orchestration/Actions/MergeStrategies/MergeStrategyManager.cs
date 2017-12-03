using GitAutomation.BranchSettings;
using System;
using System.Collections.Generic;
using System.Text;

namespace GitAutomation.Orchestration.Actions.MergeStrategies
{
    class MergeStrategyManager : IMergeStrategyManager
    {
        private readonly NormalMergeStrategy normal;
        private readonly MergeNextIterationMergeStrategy mergeNextIteration;
        private readonly ForceFreshMergeStrategy forceFresh;

        public MergeStrategyManager(NormalMergeStrategy normal, MergeNextIterationMergeStrategy mergeNextIteration, ForceFreshMergeStrategy forceFresh)
        {
            this.normal = normal;
            this.mergeNextIteration = mergeNextIteration;
            this.forceFresh = forceFresh;
        }

        public IMergeStrategy GetMergeStrategy(BranchGroup branchGroup)
        {
            switch (branchGroup.UpstreamMergePolicy)
            {
                case UpstreamMergePolicy.None:
                    return normal;
                case UpstreamMergePolicy.MergeNextIteration:
                    return mergeNextIteration;
                case UpstreamMergePolicy.ForceFresh:
                    return forceFresh;
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
