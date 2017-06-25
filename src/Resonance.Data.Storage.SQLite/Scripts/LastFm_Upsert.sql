UPDATE [LastFm]
SET LastFmId = @LastFmId, Url = @Url, Details = @Details, SmallImageUrl = @SmallImageUrl, MediumImageUrl = @MediumImageUrl, LargeImageUrl = @LargeImageUrl, LargestImageUrl = @LargestImageUrl
WHERE (Id = @Id OR MusicBrainzId = @MusicBrainzId);

INSERT INTO [LastFm] (Id, LastFmId, MusicBrainzId, Url, Details, SmallImageUrl, MediumImageUrl, LargeImageUrl, LargestImageUrl)
SELECT @Id, @LastFmId, @MusicBrainzId, @Url, @Details, @SmallImageUrl, @MediumImageUrl, @LargeImageUrl, @LargestImageUrl
WHERE changes() = 0;