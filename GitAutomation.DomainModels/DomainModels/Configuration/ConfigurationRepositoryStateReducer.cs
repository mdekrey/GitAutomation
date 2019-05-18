using System;
using GitAutomation.Optionals;
using static GitAutomation.DomainModels.Configuration.ConfigurationRepositoryState;
using static GitAutomation.DomainModels.Configuration.ConfigurationRepositoryState.ConfigurationTimestampType;

namespace GitAutomation.DomainModels.Configuration
{
    public class ConfigurationRepositoryStateReducer
    {
        public static ConfigurationRepositoryState Reduce(ConfigurationRepositoryState original, StandardAction action) =>
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

        private static ConfigurationRepositoryState ConfigurationRepositoryNoBranch(ConfigurationRepositoryState original, StandardAction action) =>
            original.Timestamps[NeedPull]
                    .IfStringMatch(action.Payload["startTimestamp"])
                    .Map(timestamp => original.With(timestampFunc: ts => ts.SetItem(Pulled, DateTimeOffset.Now)))
                    .OrElse(original);

        private static ConfigurationRepositoryState ConfigurationRepositoryPasswordIncorrect(ConfigurationRepositoryState original, StandardAction action) =>
            original.Timestamps[NeedPull].IfStringMatch(action.Payload["startTimestamp"])
                .Map(timestamp => original.With(timestampFunc: ts => ts.SetItem(NeedPull, DateTimeOffset.Now), lastError: RepositoryConfigurationLastError.Error_PasswordIncorrect))
            .OrElse(original);

        private static ConfigurationRepositoryState ConfigurationRepositoryCouldNotBeCloned(ConfigurationRepositoryState original, StandardAction action) =>
            original.Timestamps[NeedPull].IfStringMatch(action.Payload["startTimestamp"])
                .Map(timestamp => original.With(timestampFunc: ts => ts.SetItem(NeedPull, DateTimeOffset.Now), lastError: RepositoryConfigurationLastError.Error_FailedToClone))
            .OrElse(original);

        private static ConfigurationRepositoryState ConfigurationDirectoryNotAccessible(ConfigurationRepositoryState original, StandardAction action) =>
            original.Timestamps[NeedPull].IfStringMatch(action.Payload["startTimestamp"])
                .Map(timestamp => original.With(timestampFunc: ts => ts.SetItem(NeedPull, DateTimeOffset.Now), lastError: RepositoryConfigurationLastError.Error_DirectoryNotAccessible))
            .OrElse(original);

        private static ConfigurationRepositoryState ConfigurationReadyToLoad(ConfigurationRepositoryState original, StandardAction action) =>
            original.Timestamps[NeedPull].IfStringMatch(action.Payload["startTimestamp"])
                .Map(timestamp => original.With(timestampFunc: ts => ts.SetItem(Pulled, DateTimeOffset.Now)))
            .OrElse(original);

        private static ConfigurationRepositoryState ConfigurationRepositoryNested(ConfigurationRepositoryState original, StandardAction action) =>
            original.Timestamps[NeedPull].IfStringMatch(action.Payload["startTimestamp"])
                .Map(timestamp => original.With(lastError: RepositoryConfigurationLastError.Error_NestedGitRepository))
            .OrElse(original);

        private static ConfigurationRepositoryState ConfigurationRepositoryCouldNotCommit(ConfigurationRepositoryState original, StandardAction action) =>
            original.Timestamps[StoredFieldModified].IfStringMatch(action.Payload["startTimestamp"])
                .Map(timestamp => original.With(lastError: RepositoryConfigurationLastError.Error_FailedToCommit))
            .OrElse(original);

        private static ConfigurationRepositoryState ConfigurationRepositoryCouldNotPush(ConfigurationRepositoryState original, StandardAction action) =>
            original.Timestamps[StoredFieldModified].IfStringMatch(action.Payload["startTimestamp"])
                .Map(timestamp => original.With(lastError: RepositoryConfigurationLastError.Error_FailedToPush))
            .OrElse(original);

        private static ConfigurationRepositoryState ConfigurationPushSuccess(ConfigurationRepositoryState original, StandardAction action) =>
            original.Timestamps[StoredFieldModified].IfStringMatch(action.Payload["startTimestamp"])
                .Map(timestamp => original.With(timestampFunc: ts => ts.SetItem(Pushed, DateTimeOffset.Now)))
            .OrElse(original);

        private static ConfigurationRepositoryState ConfigurationLoaded(ConfigurationRepositoryState original, StandardAction action)
        {
            var configuration = (ConfigurationRepository)action.Payload["configuration"];
            var structure = (RepositoryStructure)action.Payload["structure"];

            return original.Timestamps[Pulled].IfStringMatch(action.Payload["startTimestamp"])
                .Map(timestamp => original.With(timestampFunc: ts => ts.SetItem(LoadedFromDisk, DateTimeOffset.Now), configuration: configuration, structure: structure))
                .OrElse(original);
        }

        private static ConfigurationRepositoryState ConfigurationWritten(ConfigurationRepositoryState original, StandardAction action) =>
            original.With(timestampFunc: ts => ts.SetItem(StoredFieldModified, DateTimeOffset.Now));

    }
}
