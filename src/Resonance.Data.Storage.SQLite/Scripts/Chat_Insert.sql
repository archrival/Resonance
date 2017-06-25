INSERT INTO [Chat] (UserId, Timestamp, Message)
VALUES (@UserId, STRFTIME ('%Y-%m-%dT%H:%M:%fZ', @Timestamp), @Message);