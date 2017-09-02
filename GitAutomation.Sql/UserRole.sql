CREATE TABLE [dbo].[UserRole]
(
	[ClaimName] NVARCHAR(256) NOT NULL, 
    [Role] VARCHAR(50) NOT NULL, 
    CONSTRAINT [FK_UserRole_ToUser] FOREIGN KEY (ClaimName) REFERENCES [User](ClaimName), 
    CONSTRAINT [PK_UserRole] PRIMARY KEY ([ClaimName], [Role]) 
)
