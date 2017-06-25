DELETE FROM [UserToRole]
WHERE UserId = @UserId;

DELETE FROM [Disposition]
WHERE UserId = @UserId;

DELETE FROM [Playback]
WHERE UserId = @UserId;

DELETE FROM [Playlist]
WHERE UserID = @UserId;

DELETE FROM [Status]
WHERE Id = @UserId;

DELETE FROM [History]
WHERE Id = @UserId;

DELETE FROM [User]
WHERE Id = @UserId;