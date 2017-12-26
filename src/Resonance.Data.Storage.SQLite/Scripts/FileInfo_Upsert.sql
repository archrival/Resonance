UPDATE [FileInfo]
SET DateCreated = STRFTIME ('%Y-%m-%dT%H:%M:%fZ', @DateCreated), DateModified = STRFTIME ('%Y-%m-%dT%H:%M:%fZ', @DateModified), Size = @Size, Visible = @Visible
WHERE TrackId = @Id;

INSERT INTO [FileInfo] (TrackId, DateCreated, DateModified, Size, Visible)
SELECT @Id, STRFTIME ('%Y-%m-%dT%H:%M:%fZ', @DateCreated), STRFTIME ('%Y-%m-%dT%H:%M:%fZ', @DateModified), @Size, @Visible
WHERE changes() = 0;