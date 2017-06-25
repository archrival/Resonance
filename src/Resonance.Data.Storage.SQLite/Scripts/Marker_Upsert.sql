UPDATE [Marker]
SET TrackId = @TrackId, UserId = @UserId, Position = @Position, Comment = @Comment
WHERE TrackId = @TrackId AND UserId = @UserId;

INSERT INTO [Marker] (TrackId, UserId, Position, Comment)
SELECT @TrackId, @UserId, @Position, @Comment
WHERE changes() = 0;