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

        public MergeStrategyManager(NormalMergeStrategy normal, MergeNextIterationMergeStrategy mergeNextIteration)
        {
            this.normal = normal;
            this.mergeNextIteration = mergeNextIteration;
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
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
