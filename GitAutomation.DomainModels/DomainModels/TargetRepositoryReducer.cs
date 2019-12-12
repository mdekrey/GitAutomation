using GitAutomation.DomainModels.Actions;
using GitAutomation.Optionals;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using static GitAutomation.DomainModels.TargetRepositoryState.TimestampType;

namespace GitAutomation.DomainModels
{
    public class TargetRepositoryReducer
    {
        public static TargetRepositoryState Reduce(TargetRepositoryState original, IStandardAction a) =>
            a switch
        {
            DirectoryNotAccessibleAction action => TargetDirectoryNotAccessible(original, action),
            GitNestedAction action => TargetRepositoryNested(original, action),
            GitDirtyAction action => TargetRepositoryDirty(original, action),
            GitCouldNotBeInitializedAction action => TargetRepositoryCouldNotBeInitialized(original, action),
            GitPasswordIncorrectAction action => TargetRepositoryPasswordIncorrect(original, action),
            FetchedAction action => TargetFetched(original, action),
            RefsAction action => TargetRefs(original, action),
            NeedFetchAction _ => NeedFetch(original),
            StabilizePushedReserveAction _ => NeedFetch(original),
            Configuration.Actions.ConfigurationLoadedAction _ => NeedFetch(original),
            _ => original
        };

        private static TargetRepositoryState NeedFetch(TargetRepositoryState original) =>
            original.With(timestampFunc: t => t.SetItem(NeededFetch, DateTimeOffset.Now));

        private static TargetRepositoryState TargetDirectoryNotAccessible(TargetRepositoryState original, DirectoryNotAccessibleAction action) =>
            original.Timestamps[NeededFetch].IfApproximateMatch(action.StartTimestamp)
                .Map(timestamp => original.With(lastError: TargetRepositoryState.TargetLastError.Error_DirectoryNotAccessible))
                .OrElse(original);

        private static TargetRepositoryState TargetRepositoryNested(TargetRepositoryState original, GitNestedAction action) =>
            original.Timestamps[NeededFetch].IfApproximateMatch(action.StartTimestamp)
                .Map(timestamp => original.With(lastError: TargetRepositoryState.TargetLastError.Error_DirectoryNested))
                .OrElse(original);

        private static TargetRepositoryState TargetRepositoryDirty(TargetRepositoryState original, GitDirtyAction action) =>
            original.Timestamps[NeededFetch].IfApproximateMatch(action.StartTimestamp)
                .Map(timestamp => original.With(lastError: TargetRepositoryState.TargetLastError.Error_DirectoryDirty))
                .OrElse(original);

        private static TargetRepositoryState TargetRepositoryCouldNotBeInitialized(TargetRepositoryState original, GitCouldNotBeInitializedAction action) =>
            original.Timestamps[NeededFetch].IfApproximateMatch(action.StartTimestamp)
                .Map(timestamp => original.With(lastError: TargetRepositoryState.TargetLastError.Error_FailedToClone))
                .OrElse(original);

        private static TargetRepositoryState TargetRepositoryPasswordIncorrect(TargetRepositoryState original, GitPasswordIncorrectAction action) =>
            original.Timestamps[NeededFetch].IfApproximateMatch(action.StartTimestamp)
                .Map(timestamp => original.With(lastError: TargetRepositoryState.TargetLastError.Error_PasswordIncorrect))
                .OrElse(original);

        private static TargetRepositoryState TargetFetched(TargetRepositoryState original, FetchedAction action) =>
            original.Timestamps[NeededFetch].IfApproximateMatch(action.StartTimestamp)
                .Map(timestamp => original.With(timestampFunc: t => t.SetItem(Fetched, DateTimeOffset.Now)))
                .OrElse(original);

        private static TargetRepositoryState TargetRefs(TargetRepositoryState original, RefsAction action) =>
            original.Timestamps[Fetched].IfApproximateMatch(action.StartTimestamp)
                .Map(timestamp => original.With(
                    timestampFunc: t => t.SetItem(LoadedFromDisk, DateTimeOffset.Now), 
                    branches: action.AllRefs.ToImmutableSortedDictionary(k => k.Name, k => k.Commit)))
                .OrElse(original);

    }
}
