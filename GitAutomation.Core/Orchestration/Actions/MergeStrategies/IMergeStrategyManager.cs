using GitAutomation.BranchSettings;

namespace GitAutomation.Orchestration.Actions.MergeStrategies
{
    public interface IMergeStrategyManager
    {
        IMergeStrategy GetMergeStrategy(BranchGroup branchGroup);
    }
}