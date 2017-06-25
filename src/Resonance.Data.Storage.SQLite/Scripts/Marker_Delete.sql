DELETE FROM [Marker]
WHERE TrackId = @TrackId
AND UserId = @UserId;