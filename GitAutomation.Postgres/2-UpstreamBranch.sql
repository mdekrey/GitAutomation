CREATE TABLE UpstreamBranch
(
    DownstreamBranch VARCHAR(255) NOT NULL,
    GroupName VARCHAR(255) NOT NULL,
    CONSTRAINT PK_DownstreamBranch PRIMARY KEY (DownstreamBranch, GroupName),
    CONSTRAINT FK_UpstreamBranch_ToDownstreamBranch FOREIGN KEY (DownstreamBranch) REFERENCES DownstreamBranch(GroupName)
);

CREATE INDEX IX_UpstreamBranch_BranchName ON UpstreamBranch (GroupName)
