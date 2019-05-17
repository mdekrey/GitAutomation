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
        public AppState(RepositoryConfigurationState configuration, TargetRepositoryState target)
        {
            Configuration = configuration;
            Target = target;
        }

        public static AppState ZeroState { get; } = new AppState(RepositoryConfigurationState.ZeroState, TargetRepositoryState.ZeroState);
        public RepositoryConfigurationState Configuration { get; }
        public TargetRepositoryState Target { get; }

        public AppState With(
            RepositoryConfigurationState? configuration = null, 
            TargetRepositoryState? target = null)
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

        internal static AppState Reducer(AppState original, StandardAction action, IAgentSpecification agentSpecification)
        {
            return original.With(RepositoryConfigurationStateReducer.Reduce(original.Configuration, action, agentSpecification),
                TargetRepositoryReducer.Reduce(original.Target, action, agentSpecification));
        }
#nullable restore
    }
}
