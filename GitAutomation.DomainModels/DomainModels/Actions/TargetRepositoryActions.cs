using System;
using System.Collections.Generic;
using System.Text;

namespace GitAutomation.DomainModels.Actions
{
    public struct DirectoryNotAccessibleAction : IStandardAction
    {
        public DateTimeOffset StartTimestamp { get; set; }
    }

    public struct GitNestedAction : IStandardAction
    {
        public DateTimeOffset StartTimestamp { get; set; }
        public string Path { get; set; }
    }

    public struct GitDirtyAction : IStandardAction
    {
        public DateTimeOffset StartTimestamp { get; set; }
    }

    public struct GitCouldNotBeInitializedAction : IStandardAction
    {
        public DateTimeOffset StartTimestamp { get; set; }
    }

    public struct GitPasswordIncorrectAction : IStandardAction
    {
        public DateTimeOffset StartTimestamp { get; set; }
        public string Remote { get; set; }
    }

    public struct FetchedAction : IStandardAction
    {
        public DateTimeOffset StartTimestamp { get; set; }
    }

    public struct RefsAction : IStandardAction
    {
        public DateTimeOffset StartTimestamp { get; set; }
        public RefEntry[] AllRefs { get; set; }

        public struct RefEntry
        {
            public string Commit;
            public string Name;
        }

    }

    public struct NeedFetchAction : IStandardAction
    {
    }


}
