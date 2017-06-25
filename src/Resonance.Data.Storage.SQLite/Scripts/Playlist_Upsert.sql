UPDATE [Playlist]
SET UserId = @UserId, Name = @Name, Comment = @Comment, Accessibility = @Accessibility
WHERE Id = @Id;

INSERT INTO [Playlist] (Id, UserId, Name, Comment, Accessibility)
SELECT @Id, @UserId, @Name, @Comment, @Accessibility
WHERE changes() = 0;