using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace GitAutomation.DomainModels
{
    public class TargetRepositoryState
    {
        public enum TimestampType
        {
            NeededFetch,
            Fetched,
            LoadedFromDisk,
        }

        public enum TargetLastError
        {
            None,
            Error_DirectoryNotAccessible,
            Error_FailedToClone,
            Error_PasswordIncorrect,
            Error_DirectoryNested,
            Error_DirectoryDirty,
        }

        public TargetRepositoryState(
            ImmutableSortedDictionary<TimestampType, DateTimeOffset> timestamps,
            TargetLastError lastError,
            ImmutableSortedDictionary<string, string> branches)
        {
            Timestamps = timestamps;
            LastError = lastError;
            Branches = branches;
        }

        public ImmutableSortedDictionary<TimestampType, DateTimeOffset> Timestamps { get; }
        public TargetLastError LastError { get; }
        public ImmutableSortedDictionary<string, string> Branches { get; }

        public static TargetRepositoryState ZeroState { get; } = new TargetRepositoryState(
            timestamps: ImmutableSortedDictionary.CreateRange(new Dictionary<TimestampType, DateTimeOffset>
            {
                { TimestampType.LoadedFromDisk, DateTimeOffset.MinValue },
                { TimestampType.NeededFetch, DateTimeOffset.Now },
                { TimestampType.Fetched, DateTimeOffset.MinValue },
            }),
            lastError: TargetLastError.None,
            branches: ImmutableSortedDictionary<string, string>.Empty);

        public TargetRepositoryState With(
            Func<ImmutableSortedDictionary<TimestampType, DateTimeOffset>, ImmutableSortedDictionary<TimestampType, DateTimeOffset>>? timestampFunc = null,
            TargetLastError? lastError = null,
            ImmutableSortedDictionary<string, string>? branches = null)
        {
            var timestamps = timestampFunc?.Invoke(Timestamps) ?? Timestamps;
            if ((timestamps ?? Timestamps) != Timestamps
                || (lastError ?? LastError) != LastError
                || (branches ?? Branches) != Branches)
            {
                return new TargetRepositoryState(
                    timestamps: timestamps ?? Timestamps,
                    lastError: lastError ?? LastError,
                    branches: branches ?? Branches);
            }
            return this;
        }
    }
}
