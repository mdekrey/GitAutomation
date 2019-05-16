using GitAutomation.Optionals;
using System;
using System.Collections.Generic;
using System.Text;
using static GitAutomation.DomainModels.TargetBranchesState.TimestampType;

namespace GitAutomation.DomainModels
{
    public class TargetBranchesReducer
    {
        public static TargetBranchesState Reduce(TargetBranchesState original, StandardAction action) =>
            action.Action switch
        {
            "TargetDirectoryNotAccessible" => TargetDirectoryNotAccessible(original, action),
            "TargetRepositoryNested" => TargetRepositoryNested(original, action),
            "TargetRepositoryDirty" => TargetRepositoryDirty(original, action),
            "TargetRepositoryCouldNotBeInitialized" => TargetRepositoryCouldNotBeInitialized(original, action),
            "TargetRepositoryPasswordIncorrect" => TargetRepositoryPasswordIncorrect(original, action),
            "TargetFetched" => TargetFetched(original, action),
            _ => original
        };

        private static TargetBranchesState TargetDirectoryNotAccessible(TargetBranchesState original, StandardAction action) =>
            original.Timestamps[NeededFetch].IfStringMatch(action.Payload["startTimestamp"])
                .Map(timestamp => original.With(lastError: TargetBranchesState.TargetBranchesLastError.Error_DirectoryNotAccessible))
                .OrElse(original);

        private static TargetBranchesState TargetRepositoryNested(TargetBranchesState original, StandardAction action) =>
            original.Timestamps[NeededFetch].IfStringMatch(action.Payload["startTimestamp"])
                .Map(timestamp => original.With(lastError: TargetBranchesState.TargetBranchesLastError.Error_DirectoryNested))
                .OrElse(original);

        private static TargetBranchesState TargetRepositoryDirty(TargetBranchesState original, StandardAction action) =>
            original.Timestamps[NeededFetch].IfStringMatch(action.Payload["startTimestamp"])
                .Map(timestamp => original.With(lastError: TargetBranchesState.TargetBranchesLastError.Error_DirectoryDirty))
                .OrElse(original);

        private static TargetBranchesState TargetRepositoryCouldNotBeInitialized(TargetBranchesState original, StandardAction action) =>
            original.Timestamps[NeededFetch].IfStringMatch(action.Payload["startTimestamp"])
                .Map(timestamp => original.With(lastError: TargetBranchesState.TargetBranchesLastError.Error_FailedToClone))
                .OrElse(original);

        private static TargetBranchesState TargetRepositoryPasswordIncorrect(TargetBranchesState original, StandardAction action) =>
            original.Timestamps[NeededFetch].IfStringMatch(action.Payload["startTimestamp"])
                .Map(timestamp => original.With(lastError: TargetBranchesState.TargetBranchesLastError.Error_PasswordIncorrect))
                .OrElse(original);

        private static TargetBranchesState TargetFetched(TargetBranchesState original, StandardAction action) =>
            original.Timestamps[NeededFetch].IfStringMatch(action.Payload["startTimestamp"])
                .Map(timestamp => original.With(timestampFunc: t => t.SetItem(Fetched, DateTimeOffset.Now)))
                .OrElse(original);
    }
}
