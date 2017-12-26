INSERT INTO [Playback] (Address, ClientId, TrackId, Timestamp, UserId)
VALUES (@Address, @ClientId, @TrackId, STRFTIME ('%Y-%m-%dT%H:%M:%fZ', @Timestamp), @UserId);