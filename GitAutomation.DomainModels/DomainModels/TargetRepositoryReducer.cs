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
        public static TargetRepositoryState Reduce(TargetRepositoryState original, StandardAction action) =>
            action.Action switch
        {
            "TargetRepository:DirectoryNotAccessible" => TargetDirectoryNotAccessible(original, action),
            "TargetRepository:GitNested" => TargetRepositoryNested(original, action),
            "TargetRepository:GitDirty" => TargetRepositoryDirty(original, action),
            "TargetRepository:GitCouldNotBeInitialized" => TargetRepositoryCouldNotBeInitialized(original, action),
            "TargetRepository:GitPasswordIncorrect" => TargetRepositoryPasswordIncorrect(original, action),
            "TargetRepository:Fetched" => TargetFetched(original, action),
            "TargetRepository:Refs" => TargetRefs(original, action),
            _ => original
        };

        private static TargetRepositoryState TargetDirectoryNotAccessible(TargetRepositoryState original, StandardAction action) =>
            original.Timestamps[NeededFetch].IfStringMatch(action.Payload["startTimestamp"])
                .Map(timestamp => original.With(lastError: TargetRepositoryState.TargetLastError.Error_DirectoryNotAccessible))
                .OrElse(original);

        private static TargetRepositoryState TargetRepositoryNested(TargetRepositoryState original, StandardAction action) =>
            original.Timestamps[NeededFetch].IfStringMatch(action.Payload["startTimestamp"])
                .Map(timestamp => original.With(lastError: TargetRepositoryState.TargetLastError.Error_DirectoryNested))
                .OrElse(original);

        private static TargetRepositoryState TargetRepositoryDirty(TargetRepositoryState original, StandardAction action) =>
            original.Timestamps[NeededFetch].IfStringMatch(action.Payload["startTimestamp"])
                .Map(timestamp => original.With(lastError: TargetRepositoryState.TargetLastError.Error_DirectoryDirty))
                .OrElse(original);

        private static TargetRepositoryState TargetRepositoryCouldNotBeInitialized(TargetRepositoryState original, StandardAction action) =>
            original.Timestamps[NeededFetch].IfStringMatch(action.Payload["startTimestamp"])
                .Map(timestamp => original.With(lastError: TargetRepositoryState.TargetLastError.Error_FailedToClone))
                .OrElse(original);

        private static TargetRepositoryState TargetRepositoryPasswordIncorrect(TargetRepositoryState original, StandardAction action) =>
            original.Timestamps[NeededFetch].IfStringMatch(action.Payload["startTimestamp"])
                .Map(timestamp => original.With(lastError: TargetRepositoryState.TargetLastError.Error_PasswordIncorrect))
                .OrElse(original);

        private static TargetRepositoryState TargetFetched(TargetRepositoryState original, StandardAction action) =>
            original.Timestamps[NeededFetch].IfStringMatch(action.Payload["startTimestamp"])
                .Map(timestamp => original.With(timestampFunc: t => t.SetItem(Fetched, DateTimeOffset.Now)))
                .OrElse(original);

        struct RefEntry
        {
#pragma warning disable CS0649 // These "unused" properties are for deserialization
            public string Commit;
            public string Name;
#pragma warning enable CS0649
        }

        private static TargetRepositoryState TargetRefs(TargetRepositoryState original, StandardAction action) =>
            original.Timestamps[Fetched].IfStringMatch(action.Payload["startTimestamp"])
                .Map(timestamp => original.With(
                    timestampFunc: t => t.SetItem(LoadedFromDisk, DateTimeOffset.Now), 
                    branches: JsonConvert.DeserializeObject<RefEntry[]>(action.Payload["allRefs"].ToString()).ToImmutableDictionary(k => k.Name, k => k.Commit)))
                .OrElse(original);

    }
}
