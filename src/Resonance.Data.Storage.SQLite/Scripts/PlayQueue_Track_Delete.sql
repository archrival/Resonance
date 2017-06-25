DELETE FROM [TrackToPlayQueue]
WHERE PlayQueueId IN
(SELECT Id FROM PlayQueue WHERE UserId = @UserId OR Id = @PlayQueueId);