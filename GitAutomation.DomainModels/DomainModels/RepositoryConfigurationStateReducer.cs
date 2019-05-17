using System;
using GitAutomation.Optionals;
using static GitAutomation.DomainModels.RepositoryConfigurationState;
using static GitAutomation.DomainModels.RepositoryConfigurationState.ConfigurationTimestampType;

namespace GitAutomation.DomainModels
{
    public class RepositoryConfigurationStateReducer
    {
        public static RepositoryConfigurationState Reduce(RepositoryConfigurationState original, StandardAction action, IAgentSpecification agentSpecification) =>
            (action.Action switch
            {
                "ConfigurationDirectoryNotAccessible" => ConfigurationDirectoryNotAccessible(original, action),
                "ConfigurationReadyToLoad" => ConfigurationReadyToLoad(original, action),
                "ConfigurationRepositoryNested" => ConfigurationRepositoryNested(original, action),
                "ConfigurationRepositoryCouldNotBeCloned" => ConfigurationRepositoryCouldNotBeCloned(original, action),
                "ConfigurationRepositoryPasswordIncorrect" => ConfigurationRepositoryPasswordIncorrect(original, action),
                "ConfigurationRepositoryNoBranch" => ConfigurationRepositoryNoBranch(original, action),
                "ConfigurationRepositoryCouldNotCommit" => ConfigurationRepositoryCouldNotCommit(original, action),
                "ConfigurationRepositoryCouldNotPush" => ConfigurationRepositoryCouldNotPush(original, action),
                "ConfigurationPushSuccess" => ConfigurationPushSuccess(original, action),
                "ConfigurationLoaded" => ConfigurationLoaded(original, action),
                "ConfigurationWritten" => ConfigurationWritten(original, action),
                _ => original,
            }).With(structure: RepositoryStructureReducer.Reduce(original.Structure, action));

        private static RepositoryConfigurationState ConfigurationRepositoryNoBranch(RepositoryConfigurationState original, StandardAction action) =>
            original.Timestamps[NeedPull]
                    .IfStringMatch(action.Payload["startTimestamp"])
                    .Map(timestamp => original.With(timestampFunc: ts => ts.SetItem(Pulled, DateTimeOffset.Now)))
                    .OrElse(original);

        private static RepositoryConfigurationState ConfigurationRepositoryPasswordIncorrect(RepositoryConfigurationState original, StandardAction action) =>
            original.Timestamps[NeedPull].IfStringMatch(action.Payload["startTimestamp"])
                .Map(timestamp => original.With(timestampFunc: ts => ts.SetItem(NeedPull, DateTimeOffset.Now), lastError: RepositoryConfigurationLastError.Error_PasswordIncorrect))
            .OrElse(original);

        private static RepositoryConfigurationState ConfigurationRepositoryCouldNotBeCloned(RepositoryConfigurationState original, StandardAction action) =>
            original.Timestamps[NeedPull].IfStringMatch(action.Payload["startTimestamp"])
                .Map(timestamp => original.With(timestampFunc: ts => ts.SetItem(NeedPull, DateTimeOffset.Now), lastError: RepositoryConfigurationLastError.Error_FailedToClone))
            .OrElse(original);

        private static RepositoryConfigurationState ConfigurationDirectoryNotAccessible(RepositoryConfigurationState original, StandardAction action) =>
            original.Timestamps[NeedPull].IfStringMatch(action.Payload["startTimestamp"])
                .Map(timestamp => original.With(timestampFunc: ts => ts.SetItem(NeedPull, DateTimeOffset.Now), lastError: RepositoryConfigurationLastError.Error_DirectoryNotAccessible))
            .OrElse(original);

        private static RepositoryConfigurationState ConfigurationReadyToLoad(RepositoryConfigurationState original, StandardAction action) =>
            original.Timestamps[NeedPull].IfStringMatch(action.Payload["startTimestamp"])
                .Map(timestamp => original.With(timestampFunc: ts => ts.SetItem(Pulled, DateTimeOffset.Now)))
            .OrElse(original);

        private static RepositoryConfigurationState ConfigurationRepositoryNested(RepositoryConfigurationState original, StandardAction action) =>
            original.Timestamps[NeedPull].IfStringMatch(action.Payload["startTimestamp"])
                .Map(timestamp => original.With(lastError: RepositoryConfigurationLastError.Error_NestedGitRepository))
            .OrElse(original);

        private static RepositoryConfigurationState ConfigurationRepositoryCouldNotCommit(RepositoryConfigurationState original, StandardAction action) =>
            original.Timestamps[StoredFieldModified].IfStringMatch(action.Payload["startTimestamp"])
                .Map(timestamp => original.With(lastError: RepositoryConfigurationLastError.Error_FailedToCommit))
            .OrElse(original);

        private static RepositoryConfigurationState ConfigurationRepositoryCouldNotPush(RepositoryConfigurationState original, StandardAction action) =>
            original.Timestamps[StoredFieldModified].IfStringMatch(action.Payload["startTimestamp"])
                .Map(timestamp => original.With(lastError: RepositoryConfigurationLastError.Error_FailedToPush))
            .OrElse(original);

        private static RepositoryConfigurationState ConfigurationPushSuccess(RepositoryConfigurationState original, StandardAction action) =>
            original.Timestamps[StoredFieldModified].IfStringMatch(action.Payload["startTimestamp"])
                .Map(timestamp => original.With(timestampFunc: ts => ts.SetItem(Pushed, DateTimeOffset.Now)))
            .OrElse(original);

        private static RepositoryConfigurationState ConfigurationLoaded(RepositoryConfigurationState original, StandardAction action)
        {
            var configuration = (RepositoryConfiguration)action.Payload["configuration"];
            var structure = (RepositoryStructure)action.Payload["structure"];

            return original.Timestamps[Pulled].IfStringMatch(action.Payload["startTimestamp"])
                .Map(timestamp => original.With(timestampFunc: ts => ts.SetItem(LoadedFromDisk, DateTimeOffset.Now), configuration: configuration, structure: structure))
                .OrElse(original);
        }

        private static RepositoryConfigurationState ConfigurationWritten(RepositoryConfigurationState original, StandardAction action) =>
            original.With(timestampFunc: ts => ts.SetItem(StoredFieldModified, DateTimeOffset.Now));

    }
}
