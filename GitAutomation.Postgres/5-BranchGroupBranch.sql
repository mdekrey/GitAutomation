CREATE TABLE BranchGroupBranch
(
    GroupName VARCHAR(255) NOT NULL,
    BranchName VARCHAR(255) NOT NULL,
    RefType VARCHAR(255) NOT NULL DEFAULT 'Primary',
    CONSTRAINT PK_BranchGroupBranch PRIMARY KEY (GroupName, BranchName)
);

CREATE INDEX IX_BranchGroupBranch_RefType ON BranchGroupBranch (GroupName, RefType)
