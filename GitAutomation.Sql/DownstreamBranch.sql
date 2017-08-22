CREATE TABLE [dbo].[DownstreamBranch]
(
    [BranchName] NVARCHAR(255) PRIMARY KEY, 
    [RecreateFromUpstream] BIT NOT NULL DEFAULT 0 , 
    [IsServiceLine] BIT NOT NULL DEFAULT 0 ,
	[ConflictResolutionMode] NVARCHAR(255) NOT NULL DEFAULT 'PullRequest'
)
