UPDATE [Collection]
SET Name = @Name, Filter = @Filter, Path = @Path
WHERE Id = @Id;

INSERT INTO [Collection] (Id, Name, Filter, Path)
SELECT @Id, @Name, @Filter, @Path
WHERE changes() = 0