using System;
using GitAutomation.Optionals;
using static GitAutomation.DomainModels.Configuration.ConfigurationRepositoryState;
using static GitAutomation.DomainModels.Configuration.ConfigurationRepositoryState.ConfigurationTimestampType;

namespace GitAutomation.DomainModels.Configuration
{
    public class ConfigurationRepositoryStateReducer
    {
        public static ConfigurationRepositoryState Reduce(ConfigurationRepositoryState original, StandardAction action)
        {
            var result = original.With(structure: RepositoryStructureReducer.Reduce(original.Structure, action));
            if (result.Structure != original.Structure)
            {
                result = ConfigurationWritten(result);
            }

            return (action.Action switch
            {
                "ConfigurationRepository:DirectoryNotAccessible" => ConfigurationDirectoryNotAccessible(result, action),
                "ConfigurationRepository:ReadyToLoad" => ConfigurationReadyToLoad(result, action),
                "ConfigurationRepository:GitNested" => ConfigurationRepositoryNested(result, action),
                "ConfigurationRepository:GitCouldNotClone" => ConfigurationRepositoryCouldNotBeCloned(result, action),
                "ConfigurationRepository:GitPasswordIncorrect" => ConfigurationRepositoryPasswordIncorrect(result, action),
                "ConfigurationRepository:GitNoBranch" => ConfigurationRepositoryNoBranch(result, action),
                "ConfigurationRepository:GitCouldNotCommit" => ConfigurationRepositoryCouldNotCommit(result, action),
                "ConfigurationRepository:GitCouldNotPush" => ConfigurationRepositoryCouldNotPush(result, action),
                "ConfigurationRepository:GitPushSuccess" => ConfigurationPushSuccess(result, action),
                "ConfigurationRepository:Loaded" => ConfigurationLoaded(result, action),
                "ConfigurationRepository:Written" => ConfigurationWritten(result),
                _ => result,
            });
        }

        private static ConfigurationRepositoryState ConfigurationRepositoryNoBranch(ConfigurationRepositoryState original, StandardAction action) =>
            original.Timestamps[NeedPull]
                    .IfApproximateMatch(action.Payload["startTimestamp"])
                    .Map(timestamp => original.With(timestampFunc: ts => ts.SetItem(Pulled, DateTimeOffset.Now)))
                    .OrElse(original);

        private static ConfigurationRepositoryState ConfigurationRepositoryPasswordIncorrect(ConfigurationRepositoryState original, StandardAction action) =>
            original.Timestamps[NeedPull].IfApproximateMatch(action.Payload["startTimestamp"])
                .Map(timestamp => original.With(timestampFunc: ts => ts.SetItem(NeedPull, DateTimeOffset.Now), lastError: RepositoryConfigurationLastError.Error_PasswordIncorrect))
            .OrElse(original);

        private static ConfigurationRepositoryState ConfigurationRepositoryCouldNotBeCloned(ConfigurationRepositoryState original, StandardAction action) =>
            original.Timestamps[NeedPull].IfApproximateMatch(action.Payload["startTimestamp"])
                .Map(timestamp => original.With(timestampFunc: ts => ts.SetItem(NeedPull, DateTimeOffset.Now), lastError: RepositoryConfigurationLastError.Error_FailedToClone))
            .OrElse(original);

        private static ConfigurationRepositoryState ConfigurationDirectoryNotAccessible(ConfigurationRepositoryState original, StandardAction action) =>
            original.Timestamps[NeedPull].IfApproximateMatch(action.Payload["startTimestamp"])
                .Map(timestamp => original.With(timestampFunc: ts => ts.SetItem(NeedPull, DateTimeOffset.Now), lastError: RepositoryConfigurationLastError.Error_DirectoryNotAccessible))
            .OrElse(original);

        private static ConfigurationRepositoryState ConfigurationReadyToLoad(ConfigurationRepositoryState original, StandardAction action) =>
            original.Timestamps[NeedPull].IfApproximateMatch(action.Payload["startTimestamp"])
                .Map(timestamp => original.With(timestampFunc: ts => ts.SetItem(Pulled, DateTimeOffset.Now)))
            .OrElse(original);

        private static ConfigurationRepositoryState ConfigurationRepositoryNested(ConfigurationRepositoryState original, StandardAction action) =>
            original.Timestamps[NeedPull].IfApproximateMatch(action.Payload["startTimestamp"])
                .Map(timestamp => original.With(lastError: RepositoryConfigurationLastError.Error_NestedGitRepository))
            .OrElse(original);

        private static ConfigurationRepositoryState ConfigurationRepositoryCouldNotCommit(ConfigurationRepositoryState original, StandardAction action) =>
            original.Timestamps[StoredFieldModified].IfApproximateMatch(action.Payload["startTimestamp"])
                .Map(timestamp => original.With(lastError: RepositoryConfigurationLastError.Error_FailedToCommit))
            .OrElse(original);

        private static ConfigurationRepositoryState ConfigurationRepositoryCouldNotPush(ConfigurationRepositoryState original, StandardAction action) =>
            original.Timestamps[StoredFieldModified].IfApproximateMatch(action.Payload["startTimestamp"])
                .Map(timestamp => original.With(lastError: RepositoryConfigurationLastError.Error_FailedToPush))
            .OrElse(original);

        private static ConfigurationRepositoryState ConfigurationPushSuccess(ConfigurationRepositoryState original, StandardAction action) =>
            original.Timestamps[StoredFieldModified].IfApproximateMatch(action.Payload["startTimestamp"])
                .Map(timestamp => original.With(timestampFunc: ts => ts.SetItem(Pushed, DateTimeOffset.Now)))
            .OrElse(original);

        private static ConfigurationRepositoryState ConfigurationLoaded(ConfigurationRepositoryState original, StandardAction action)
        {
            var configuration = action.Payload["configuration"].ToObject<ConfigurationRepository>();
            var structure = action.Payload["structure"].ToObject<RepositoryStructure>();

            return original.Timestamps[Pulled].IfApproximateMatch(action.Payload["startTimestamp"])
                .Map(timestamp => original.With(timestampFunc: ts => ts.SetItem(LoadedFromDisk, DateTimeOffset.Now), configuration: configuration, structure: structure))
                .OrElse(original);
        }

        private static ConfigurationRepositoryState ConfigurationWritten(ConfigurationRepositoryState original) =>
            original.With(timestampFunc: ts => ts.SetItem(StoredFieldModified, DateTimeOffset.Now));

    }
}
