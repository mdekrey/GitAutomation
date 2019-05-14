using GitAutomation.DomainModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using GitAutomation.Optionals;
using static GitAutomation.Web.RepositoryConfigurationState;

namespace GitAutomation.Web.State
{
    public class StateMachine : IDispatcher, IStateMachine
    {
        private readonly BehaviorSubject<RepositoryConfigurationState> state = new BehaviorSubject<RepositoryConfigurationState>(RepositoryConfigurationState.ZeroState);

        public RepositoryConfigurationState State => state.Value;
        public IObservable<RepositoryConfigurationState> StateUpdates => state.AsObservable();

        public void Dispatch(StandardAction action)
        {
            lock (state)
            {
                var original = state.Value;
                state.OnNext(
                    (action.Action switch
                    {
                        "ConfigurationDirectoryNotAccessible" => ConfigurationDirectoryNotAccessible(original, action),
                        "ConfigurationReadyToLoad" => ConfigurationReadyToLoad(original, action),
                        "ConfigurationRepositoryCouldNotBeCloned" => ConfigurationRepositoryCouldNotBeCloned(original, action),
                        "ConfigurationRepositoryPasswordIncorrect" => ConfigurationRepositoryPasswordIncorrect(original, action),
                        "ConfigurationRepositoryNoBranch" => ConfigurationRepositoryNoBranch(original, action),
                        "ConfigurationRepositoryCouldNotCommit" => ConfigurationRepositoryCouldNotCommit(original, action),
                        "ConfigurationRepositoryCouldNotPush" => ConfigurationRepositoryCouldNotPush(original, action),
                        "ConfigurationPushSuccess" => ConfigurationPushSuccess(original, action),
                        "ConfigurationLoaded" => ConfigurationLoaded(original, action),
                        "ConfigurationWritten" => ConfigurationWritten(original, action),
                        _ => original,
                    }).With(structure: RepositoryStructureReducer.Reduce(original.Structure, action))
                );
            }
        }

        private RepositoryConfigurationState ConfigurationRepositoryNoBranch(RepositoryConfigurationState original, StandardAction action) =>
            original.NeedPullTimestamp.IfStringMatch(action.Payload["startTimestamp"])
                .Map(timestamp =>
            original.With(pulledTimestamp: DateTimeOffset.Now))
            .OrElse(original);

        private RepositoryConfigurationState ConfigurationRepositoryPasswordIncorrect(RepositoryConfigurationState original, StandardAction action) =>
            original.NeedPullTimestamp.IfStringMatch(action.Payload["startTimestamp"])
                .Map(timestamp => original.With(needPullTimestamp: DateTimeOffset.Now, lastError: RepositoryConfigurationLastError.Error_PasswordIncorrect))
            .OrElse(original);

        private RepositoryConfigurationState ConfigurationRepositoryCouldNotBeCloned(RepositoryConfigurationState original, StandardAction action) =>
            original.NeedPullTimestamp.IfStringMatch(action.Payload["startTimestamp"])
                .Map(timestamp => original.With(needPullTimestamp: DateTimeOffset.Now, lastError: RepositoryConfigurationLastError.Error_FailedToClone))
            .OrElse(original);

        private RepositoryConfigurationState ConfigurationDirectoryNotAccessible(RepositoryConfigurationState original, StandardAction action) =>
            original.NeedPullTimestamp.IfStringMatch(action.Payload["startTimestamp"])
                .Map(timestamp => original.With(needPullTimestamp: DateTimeOffset.Now, lastError: RepositoryConfigurationLastError.Error_DirectoryNotAccessible))
            .OrElse(original);

        private RepositoryConfigurationState ConfigurationReadyToLoad(RepositoryConfigurationState original, StandardAction action) =>
            original.NeedPullTimestamp.IfStringMatch(action.Payload["startTimestamp"])
                .Map(timestamp => original.With(pulledTimestamp: DateTimeOffset.Now))
            .OrElse(original);

        private RepositoryConfigurationState ConfigurationRepositoryCouldNotCommit(RepositoryConfigurationState original, StandardAction action) =>
            original.StoredFieldModifiedTimestamp.IfStringMatch(action.Payload["startTimestamp"])
                .Map(timestamp => original.With(lastError: RepositoryConfigurationLastError.Error_FailedToCommit))
            .OrElse(original);

        private RepositoryConfigurationState ConfigurationRepositoryCouldNotPush(RepositoryConfigurationState original, StandardAction action) =>
            original.StoredFieldModifiedTimestamp.IfStringMatch(action.Payload["startTimestamp"])
                .Map(timestamp => original.With(lastError: RepositoryConfigurationLastError.Error_FailedToPush))
            .OrElse(original);

        private RepositoryConfigurationState ConfigurationPushSuccess(RepositoryConfigurationState original, StandardAction action) =>
            original.StoredFieldModifiedTimestamp.IfStringMatch(action.Payload["startTimestamp"])
                .Map(timestamp => original.With(pushedTimestamp: DateTimeOffset.Now))
            .OrElse(original);

        private RepositoryConfigurationState ConfigurationLoaded(RepositoryConfigurationState original, StandardAction action)
        {
            var configuration = (RepositoryConfiguration)action.Payload["configuration"];
            var structure = (RepositoryStructure)action.Payload["structure"];

            return original.PulledTimestamp.IfStringMatch(action.Payload["startTimestamp"])
                .Map(timestamp => original.With(loadedFromDiskTimestamp: DateTimeOffset.Now, configuration: configuration, structure: structure))
                .OrElse(original);
        }

        private RepositoryConfigurationState ConfigurationWritten(RepositoryConfigurationState original, StandardAction action)
        {

            return original.With(storedFieldModifiedTimestamp: DateTimeOffset.Now);
        }

    }
}
