CREATE TABLE UserRole
(
	ClaimName VARCHAR(256) NOT NULL,
    Role VARCHAR(50) NOT NULL,
    CONSTRAINT FK_UserRole_ToUser FOREIGN KEY (ClaimName) REFERENCES ClaimedUser(ClaimName),
    CONSTRAINT PK_UserRole PRIMARY KEY (ClaimName, Role)
)
