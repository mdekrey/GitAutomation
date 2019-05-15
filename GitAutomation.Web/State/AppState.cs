using GitAutomation.DomainModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GitAutomation.Web.State
{
    public class AppState
    {
#nullable enable
        public AppState(RepositoryConfigurationState configuration, TargetBranchesState target)
        {
            Configuration = configuration;
            Target = target;
        }

        public static AppState ZeroState { get; } = new AppState(RepositoryConfigurationState.ZeroState, TargetBranchesState.ZeroState);
        public RepositoryConfigurationState Configuration { get; }
        public TargetBranchesState Target { get; }

        public AppState With(
            RepositoryConfigurationState? configuration = null, 
            TargetBranchesState? target = null)
        {
            if ((configuration ?? Configuration) != Configuration
                || (target ?? Target) != Target)
            {
                return new AppState(
                    configuration: configuration ?? Configuration,
                    target: target ?? Target);
            }
            return this;
        }

        internal static AppState Reducer(AppState original, StandardAction action)
        {
            return original.With(RepositoryConfigurationStateReducer.Reduce(original.Configuration, action),
                TargetBranchesReducer.Reduce(original.Target, action));
        }
#nullable restore
    }
}
