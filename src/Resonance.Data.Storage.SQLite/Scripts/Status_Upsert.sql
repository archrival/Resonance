UPDATE [Status]
SET Enabled = @Enabled
WHERE Id = @Id;

INSERT INTO [Status] (Id, Enabled)
SELECT @Id, @Enabled
WHERE changes() = 0;