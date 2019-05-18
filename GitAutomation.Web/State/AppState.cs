using GitAutomation.DomainModels;
using GitAutomation.DomainModels.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GitAutomation.Web.State
{
    public class AppState
    {
#nullable enable
        public AppState(ConfigurationRepositoryState configuration, TargetRepositoryState target)
        {
            Configuration = configuration;
            Target = target;
        }

        public static AppState ZeroState { get; } = new AppState(ConfigurationRepositoryState.ZeroState, TargetRepositoryState.ZeroState);
        public ConfigurationRepositoryState Configuration { get; }
        public TargetRepositoryState Target { get; }

        public AppState With(
            ConfigurationRepositoryState? configuration = null, 
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

        internal static AppState Reducer(AppState original, StandardAction action)
        {
            return original.With(ConfigurationRepositoryStateReducer.Reduce(original.Configuration, action),
                TargetRepositoryReducer.Reduce(original.Target, action));
        }
#nullable restore
    }
}
