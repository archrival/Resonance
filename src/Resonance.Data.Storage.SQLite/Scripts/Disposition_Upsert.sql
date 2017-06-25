UPDATE [Disposition]
SET CollectionId = @CollectionId, MediaTypeId = @MediaTypeId, Favorited = @Favorited, MediaId = @MediaId, UserId = @UserId, Rating = @Rating
WHERE Id = @Id;

INSERT INTO [Disposition] (Id, CollectionId, MediaTypeId, Favorited, MediaId, UserId, Rating)
SELECT @Id, @CollectionId, @MediaTypeId, @Favorited, @MediaId, @UserId, @Rating
WHERE changes() = 0;