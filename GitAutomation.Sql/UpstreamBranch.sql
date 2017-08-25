CREATE TABLE [dbo].[UpstreamBranch]
(
    [DownstreamBranch] NVARCHAR(255) NOT NULL,
    [BranchName] NVARCHAR(255) NOT NULL, 
    CONSTRAINT [PK_DownstreamBranch] PRIMARY KEY ([DownstreamBranch], [BranchName]), 
    CONSTRAINT [FK_UpstreamBranch_ToDownstreamBranch] FOREIGN KEY ([DownstreamBranch]) REFERENCES [DownstreamBranch]([BranchName])
)

GO

CREATE INDEX [IX_UpstreamBranch_BranchName] ON [dbo].[UpstreamBranch] ([BranchName])
