UPDATE [PlayQueue]
SET UserId = @UserId, ClientName = @ClientName, CurrentTrackId = @CurrentTrackId, Position = @Position
WHERE Id = @Id;

INSERT INTO [PlayQueue] (Id, UserId, ClientName, CurrentTrackId, Position)
SELECT @Id, @UserId, @ClientName, @CurrentTrackId, @Position
WHERE changes() = 0;