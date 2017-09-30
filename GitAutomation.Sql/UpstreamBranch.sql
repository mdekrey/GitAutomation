CREATE TABLE [dbo].[UpstreamBranch]
(
    [DownstreamBranch] NVARCHAR(255) NOT NULL,
    [GroupName] NVARCHAR(255) NOT NULL, 
    CONSTRAINT [PK_DownstreamBranch] PRIMARY KEY ([DownstreamBranch], [GroupName]), 
    CONSTRAINT [FK_UpstreamBranch_ToDownstreamBranch] FOREIGN KEY ([DownstreamBranch]) REFERENCES [DownstreamBranch]([GroupName])
)

GO

CREATE INDEX [IX_UpstreamBranch_GroupName] ON [dbo].[UpstreamBranch] ([GroupName])
