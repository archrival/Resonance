UPDATE [ReplayGain]
SET AlbumGain = @AlbumGain, AlbumPeak = @AlbumPeak, TrackGain = @TrackGain, TrackPeak = @TrackPeak
WHERE TrackId = @Id;

INSERT INTO [ReplayGain] (TrackId, AlbumGain, AlbumPeak, TrackGain, TrackPeak)
SELECT @Id, @AlbumGain, @AlbumPeak, @TrackGain, @TrackPeak
WHERE changes() = 0;