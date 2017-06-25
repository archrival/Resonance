UPDATE [FileInfo]
SET DateCreated = @DateCreated, DateModified = @DateModified, Size = @Size, Visible = @Visible
WHERE TrackId = @Id;

INSERT INTO [FileInfo] (TrackId, DateCreated, DateModified, Size, Visible)
SELECT @Id, @DateCreated, @DateModified, @Size, @Visible
WHERE changes() = 0;