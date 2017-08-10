CREATE TABLE [dbo].[BaseBranch]
(
    [TargetBranch] NVARCHAR(255) NOT NULL,
    [BranchName] NVARCHAR(255) NOT NULL, 
    CONSTRAINT [PK_BaseBranch] PRIMARY KEY ([TargetBranch], [BranchName])
)
