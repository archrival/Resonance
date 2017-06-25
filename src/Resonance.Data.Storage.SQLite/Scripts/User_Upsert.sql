UPDATE [User]
SET EmailAddress = @EmailAddress, Password = @Password
WHERE Id = @Id;

INSERT INTO [User] (Id, Name, EmailAddress, Password)
SELECT @Id, @Name, @EmailAddress, @Password
WHERE changes() = 0;