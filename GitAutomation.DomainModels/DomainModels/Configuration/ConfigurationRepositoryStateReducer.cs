using System;
using GitAutomation.DomainModels.Configuration.Actions;
using GitAutomation.Optionals;
using static GitAutomation.DomainModels.Configuration.ConfigurationRepositoryState;
using static GitAutomation.DomainModels.Configuration.ConfigurationRepositoryState.ConfigurationTimestampType;

namespace GitAutomation.DomainModels.Configuration
{
    public class ConfigurationRepositoryStateReducer
    {
        public static ConfigurationRepositoryState Reduce(ConfigurationRepositoryState original, IStandardAction action)
        {
            var result = original.With(structure: RepositoryStructureReducer.Reduce(original.Structure, action));
            result = action switch
            {
                DirectoryNotAccessibleAction a => ConfigurationDirectoryNotAccessible(result, a),
                ReadyToLoadAction a => ConfigurationReadyToLoad(result, a),
                GitNestedAction a => ConfigurationRepositoryNested(result, a),
                GitCouldNotCloneAction a => ConfigurationRepositoryCouldNotBeCloned(result, a),
                GitPasswordIncorrectAction a => ConfigurationRepositoryPasswordIncorrect(result, a),
                GitNoBranchAction a => ConfigurationRepositoryNoBranch(result, a),
                GitCouldNotCommitAction a => ConfigurationRepositoryCouldNotCommit(result, a),
                GitCouldNotPushAction a => ConfigurationRepositoryCouldNotPush(result, a),
                GitPushSuccessAction a => ConfigurationPushSuccess(result, a),
                ConfigurationLoadedAction a => ConfigurationLoaded(result, a),
                ConfigurationWrittenAction _ => ConfigurationWritten(result),
                _ => result,
            };
            if (result.Structure != original.Structure || result.Configuration != original.Configuration)
            {
                result = ConfigurationWritten(result);
            }

            return result;
        }

        private static ConfigurationRepositoryState ConfigurationRepositoryNoBranch(ConfigurationRepositoryState original, GitNoBranchAction action) =>
            original.Timestamps[NeedPull]
                    .IfApproximateMatch(action.StartTimestamp)
                    .Map(timestamp => original.With(timestampFunc: ts => ts.SetItem(Pulled, DateTimeOffset.Now)))
                    .OrElse(original);

        private static ConfigurationRepositoryState ConfigurationRepositoryPasswordIncorrect(ConfigurationRepositoryState original, GitPasswordIncorrectAction action) =>
            original.Timestamps[NeedPull].IfApproximateMatch(action.StartTimestamp)
                .Map(timestamp => original.With(timestampFunc: ts => ts.SetItem(NeedPull, DateTimeOffset.Now), lastError: RepositoryConfigurationLastError.Error_PasswordIncorrect))
            .OrElse(original);

        private static ConfigurationRepositoryState ConfigurationRepositoryCouldNotBeCloned(ConfigurationRepositoryState original, GitCouldNotCloneAction action) =>
            original.Timestamps[NeedPull].IfApproximateMatch(action.StartTimestamp)
                .Map(timestamp => original.With(timestampFunc: ts => ts.SetItem(NeedPull, DateTimeOffset.Now), lastError: RepositoryConfigurationLastError.Error_FailedToClone))
            .OrElse(original);

        private static ConfigurationRepositoryState ConfigurationDirectoryNotAccessible(ConfigurationRepositoryState original, DirectoryNotAccessibleAction action) =>
            original.Timestamps[NeedPull].IfApproximateMatch(action.StartTimestamp)
                .Map(timestamp => original.With(timestampFunc: ts => ts.SetItem(NeedPull, DateTimeOffset.Now), lastError: RepositoryConfigurationLastError.Error_DirectoryNotAccessible))
            .OrElse(original);

        private static ConfigurationRepositoryState ConfigurationReadyToLoad(ConfigurationRepositoryState original, ReadyToLoadAction action) =>
            original.Timestamps[NeedPull].IfApproximateMatch(action.StartTimestamp)
                .Map(timestamp => original.With(timestampFunc: ts => ts.SetItem(Pulled, DateTimeOffset.Now)))
            .OrElse(original);

        private static ConfigurationRepositoryState ConfigurationRepositoryNested(ConfigurationRepositoryState original, GitNestedAction action) =>
            original.Timestamps[NeedPull].IfApproximateMatch(action.StartTimestamp)
                .Map(timestamp => original.With(lastError: RepositoryConfigurationLastError.Error_NestedGitRepository))
            .OrElse(original);

        private static ConfigurationRepositoryState ConfigurationRepositoryCouldNotCommit(ConfigurationRepositoryState original, GitCouldNotCommitAction action) =>
            original.Timestamps[StoredFieldModified].IfApproximateMatch(action.StartTimestamp)
                .Map(timestamp => original.With(lastError: RepositoryConfigurationLastError.Error_FailedToCommit))
            .OrElse(original);

        private static ConfigurationRepositoryState ConfigurationRepositoryCouldNotPush(ConfigurationRepositoryState original, GitCouldNotPushAction action) =>
            original.Timestamps[StoredFieldModified].IfApproximateMatch(action.StartTimestamp)
                .Map(timestamp => original.With(lastError: RepositoryConfigurationLastError.Error_FailedToPush))
            .OrElse(original);

        private static ConfigurationRepositoryState ConfigurationPushSuccess(ConfigurationRepositoryState original, GitPushSuccessAction action) =>
            original.Timestamps[StoredFieldModified].IfApproximateMatch(action.StartTimestamp)
                .Map(timestamp => original.With(timestampFunc: ts => ts.SetItem(Pushed, DateTimeOffset.Now)))
            .OrElse(original);

        private static ConfigurationRepositoryState ConfigurationLoaded(ConfigurationRepositoryState original, ConfigurationLoadedAction action)
        {
            var configuration = action.Configuration;
            var structure = action.Structure;

            return original.Timestamps[Pulled].IfApproximateMatch(action.StartTimestamp)
                .Map(timestamp => original.With(timestampFunc: ts => ts.SetItem(LoadedFromDisk, DateTimeOffset.Now), configuration: configuration, structure: structure))
                .OrElse(original);
        }

        private static ConfigurationRepositoryState ConfigurationWritten(ConfigurationRepositoryState original) =>
            original.With(timestampFunc: ts => ts.SetItem(StoredFieldModified, DateTimeOffset.Now));

    }
}
