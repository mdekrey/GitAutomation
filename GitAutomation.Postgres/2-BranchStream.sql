CREATE TABLE BranchStream
(
    DownstreamBranch VARCHAR(255) NOT NULL,
    UpstreamBranch VARCHAR(255) NOT NULL,
    CONSTRAINT PK_DownstreamBranch PRIMARY KEY (DownstreamBranch, UpstreamBranch),
    CONSTRAINT FK_BranchStream_ToDownstreamBranchGroup FOREIGN KEY (DownstreamBranch) REFERENCES BranchGroup(GroupName),
    CONSTRAINT FK_BranchStream_ToUpstreamBranchGroup FOREIGN KEY (UpstreamBranch) REFERENCES BranchGroup(GroupName)
);

CREATE INDEX IX_BranchStream_UpstreamBranch ON BranchStream (UpstreamBranch)
