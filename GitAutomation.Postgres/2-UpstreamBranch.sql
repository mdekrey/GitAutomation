CREATE TABLE UpstreamBranch
(
    DownstreamBranch VARCHAR(255) NOT NULL,
    BranchName VARCHAR(255) NOT NULL,
    CONSTRAINT PK_DownstreamBranch PRIMARY KEY (DownstreamBranch, BranchName),
    CONSTRAINT FK_UpstreamBranch_ToDownstreamBranch FOREIGN KEY (DownstreamBranch) REFERENCES DownstreamBranch(BranchName)
);

CREATE INDEX IX_UpstreamBranch_BranchName ON UpstreamBranch (BranchName)
