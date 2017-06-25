UPDATE [MediaInfo]
SET MusicBrainzId = @MusicBrainzId, LastFmId = @LastFmId, MediaId = @MediaId
WHERE Id = @Id;

INSERT INTO [MediaInfo] (Id, MediaId, MusicBrainzId, LastFmId)
SELECT @Id, @MediaId, @MusicBrainzId, @LastFmId
WHERE changes() = 0;