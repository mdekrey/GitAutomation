CREATE TABLE [dbo].[BranchStream]
(
    [DownstreamBranch] NVARCHAR(255) NOT NULL,
    [UpstreamBranch] NVARCHAR(255) NOT NULL, 
    CONSTRAINT [PK_BranchStream] PRIMARY KEY ([DownstreamBranch], [UpstreamBranch]), 
    CONSTRAINT [FK_BranchStream_ToUpstreamBranch] FOREIGN KEY ([UpstreamBranch]) REFERENCES [BranchGroup]([GroupName]), 
    CONSTRAINT [FK_BranchStream_ToDownstreamBranch] FOREIGN KEY ([DownstreamBranch]) REFERENCES [BranchGroup]([GroupName])
)

GO

CREATE INDEX [IX_BranchStream_UpstreamBranch] ON [dbo].[BranchStream] ([UpstreamBranch])
