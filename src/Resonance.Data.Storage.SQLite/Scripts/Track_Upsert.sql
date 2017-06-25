UPDATE [Track]
SET CollectionId = @CollectionId, Path = @Path, Name = @Name, Bitrate = @Bitrate, Channels = @Channels, Comment = @Comment, ContentType = @ContentType, Disc = @Disc, Duration = @Duration, Number = @Number, ReleaseDate = @ReleaseDate, SampleRate = @SampleRate
WHERE Id = @Id;

INSERT INTO [Track] (Id, CollectionId, Path, Name, Bitrate, Channels, Comment, ContentType, Disc, Duration, Number, ReleaseDate, SampleRate)
SELECT @Id, @CollectionId, @Path, @Name, @Bitrate, @Channels, @Comment, @ContentType, @Disc, @Duration, @Number, @ReleaseDate, @SampleRate
WHERE changes() = 0;