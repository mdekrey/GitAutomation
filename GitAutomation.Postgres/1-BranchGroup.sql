CREATE TABLE BranchGroup
(
    GroupName VARCHAR(255) PRIMARY KEY,
    RecreateFromUpstream BIT NOT NULL DEFAULT '0' ,
    BranchType VARCHAR(255) NOT NULL DEFAULT 'Feature'
)
