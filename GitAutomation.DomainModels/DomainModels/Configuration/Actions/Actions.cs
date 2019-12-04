using System;
using System.Collections.Generic;
using System.Text;

namespace GitAutomation.DomainModels.Configuration.Actions
{
    public struct DirectoryNotAccessibleAction : IStandardAction
    {
        public DateTimeOffset StartTimestamp { get; set; }
    }

    public struct ReadyToLoadAction : IStandardAction
    {
        public DateTimeOffset StartTimestamp { get; set; }
    }

    public struct GitNestedAction : IStandardAction
    {
        public DateTimeOffset StartTimestamp { get; set; }
        public string Path { get; set; }
    }

    public struct GitCouldNotCloneAction : IStandardAction
    {
        public DateTimeOffset StartTimestamp { get; set; }
    }

    public struct GitPasswordIncorrectAction : IStandardAction
    {
        public DateTimeOffset StartTimestamp { get; set; }
    }

    public struct GitNoBranchAction : IStandardAction
    {
        public DateTimeOffset StartTimestamp { get; set; }
    }

    public struct GitCouldNotCommitAction : IStandardAction
    {
        public DateTimeOffset StartTimestamp { get; set; }
    }

    public struct GitCouldNotPushAction : IStandardAction
    {
        public DateTimeOffset StartTimestamp { get; set; }
    }

    public struct GitPushSuccessAction : IStandardAction
    {
        public DateTimeOffset StartTimestamp { get; set; }
    }

    public struct ConfigurationLoadedAction : IStandardAction
    {
        public ConfigurationRepository Configuration { get; set; }
        public RepositoryStructure Structure { get; set; }
        public DateTimeOffset StartTimestamp { get; set; }
    }

    public struct ConfigurationWrittenAction : IStandardAction
    {
        public DateTimeOffset StartTimestamp { get; set; }
    }

}
