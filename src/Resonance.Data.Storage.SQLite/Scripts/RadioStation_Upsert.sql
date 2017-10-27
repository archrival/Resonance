UPDATE [RadioStation]
SET Name = @Name, StreamUrl = @StreamUrl, HomepageUrl = @HomepageUrl
WHERE Id = @Id;

INSERT INTO [RadioStation] (Id, Name, StreamUrl, HomepageUrl)
SELECT @Id, @Name, @StreamUrl, HomepageUrl
WHERE changes() = 0;