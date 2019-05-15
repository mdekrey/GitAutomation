using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace GitAutomation.DomainModels
{
    public class TargetBranchesState
    {
        public enum TimestampType
        {
            NeededFetch,
            Fetched,
            LoadedFromDisk,
        }

        public enum TargetBranchesLastError
        {
            None,
            Error_DirectoryNotAccessible,
            Error_FailedToClone,
            Error_PasswordIncorrect,
        }

        public TargetBranchesState(
            ImmutableSortedDictionary<TimestampType, DateTimeOffset> timestamps,
            TargetBranchesLastError lastError,
            ImmutableDictionary<string, string> branches)
        {
            Timestamps = timestamps;
            LastError = lastError;
            Branches = branches;
        }

        public ImmutableSortedDictionary<TimestampType, DateTimeOffset> Timestamps { get; }
        public TargetBranchesLastError LastError { get; }
        public ImmutableDictionary<string, string> Branches { get; }

        public static TargetBranchesState ZeroState { get; } = new TargetBranchesState(
            timestamps: ImmutableSortedDictionary.CreateRange(new Dictionary<TimestampType, DateTimeOffset>
            {
                { TimestampType.LoadedFromDisk, DateTimeOffset.MinValue },
                { TimestampType.NeededFetch, DateTimeOffset.Now },
                { TimestampType.Fetched, DateTimeOffset.MinValue },
            }),
            lastError: TargetBranchesLastError.None,
            branches: ImmutableDictionary<string, string>.Empty);

        public TargetBranchesState With(
            Func<ImmutableSortedDictionary<TimestampType, DateTimeOffset>, ImmutableSortedDictionary<TimestampType, DateTimeOffset>>? timestampFunc = null,
            TargetBranchesLastError? lastError = null,
            ImmutableDictionary<string, string>? branches = null)
        {
            var timestamps = timestampFunc?.Invoke(Timestamps) ?? Timestamps;
            if ((timestamps ?? Timestamps) != Timestamps
                || (lastError ?? LastError) != LastError
                || (branches ?? Branches) != Branches)
            {
                return new TargetBranchesState(
                    timestamps: timestamps ?? Timestamps,
                    lastError: lastError ?? LastError,
                    branches: branches ?? Branches);
            }
            return this;
        }
    }
}
