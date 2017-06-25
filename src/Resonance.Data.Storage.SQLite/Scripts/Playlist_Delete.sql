DELETE FROM [Playlist]
WHERE Id = @PlaylistId
AND UserId = @UserId;