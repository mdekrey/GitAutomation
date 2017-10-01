CREATE TABLE [dbo].[BranchStream]
(
    [DownstreamBranch] NVARCHAR(255) NOT NULL,
    [GroupName] NVARCHAR(255) NOT NULL, 
    CONSTRAINT [PK_DownstreamBranch] PRIMARY KEY ([DownstreamBranch], [GroupName]), 
    CONSTRAINT [FK_UpstreamBranch_ToDownstreamBranch] FOREIGN KEY ([DownstreamBranch]) REFERENCES [BranchGroup]([GroupName])
)

GO

CREATE INDEX [IX_BranchStream_GroupName] ON [dbo].[BranchStream] ([GroupName])
