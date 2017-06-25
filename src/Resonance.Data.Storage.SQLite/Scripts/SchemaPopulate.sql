INSERT INTO [Schema] (Version)
SELECT 1
WHERE NOT EXISTS (SELECT 1 FROM [Schema] WHERE Version = 1);

INSERT INTO [MediaType] (Id, Name)
SELECT 0, 'Artist'
WHERE NOT EXISTS (SELECT 1 FROM [MediaType] WHERE Id = 0 AND Name = 'Artist');

INSERT INTO [MediaType] (Id, Name)
SELECT 1, 'Album'
WHERE NOT EXISTS (SELECT 1 FROM [MediaType] WHERE Id = 1 AND Name = 'Album');

INSERT INTO [MediaType] (Id, Name)
SELECT 2, 'Track'
WHERE NOT EXISTS (SELECT 1 FROM [MediaType] WHERE Id = 2 AND Name = 'Track');

INSERT INTO [CoverArtType] (Id, Name)
SELECT -1, 'Other'
WHERE NOT EXISTS (SELECT 1 FROM [CoverArtType] WHERE Id = -1 AND Name = 'Other');

INSERT INTO [CoverArtType] (Id, Name)
SELECT 0, 'Front'
WHERE NOT EXISTS (SELECT 1 FROM [CoverArtType] WHERE Id = 0 AND Name = 'Front');

INSERT INTO [CoverArtType] (Id, Name)
SELECT 1, 'Back'
WHERE NOT EXISTS (SELECT 1 FROM [CoverArtType] WHERE Id = 1 AND Name = 'Back');

INSERT INTO [CoverArtType] (Id, Name)
SELECT 2, 'Leaflet'
WHERE NOT EXISTS (SELECT 1 FROM [CoverArtType] WHERE Id = 2 AND Name = 'Leaflet');

INSERT INTO [CoverArtType] (Id, Name)
SELECT 3, 'Media'
WHERE NOT EXISTS (SELECT 1 FROM [CoverArtType] WHERE Id = 3 AND Name = 'Media');

INSERT INTO [CoverArtType] (Id, Name)
SELECT 4, 'LeadArtist'
WHERE NOT EXISTS (SELECT 1 FROM [CoverArtType] WHERE Id = 4 AND Name = 'LeadArtist');

INSERT INTO [CoverArtType] (Id, Name)
SELECT 5, 'Artist'
WHERE NOT EXISTS (SELECT 1 FROM [CoverArtType] WHERE Id = 5 AND Name = 'Artist');

INSERT INTO [CoverArtType] (Id, Name)
SELECT 6, 'Band'
WHERE NOT EXISTS (SELECT 1 FROM [CoverArtType] WHERE Id = 6 AND Name = 'Band');

INSERT INTO [CoverArtType] (Id, Name)
SELECT 7, 'BandLogo'
WHERE NOT EXISTS (SELECT 1 FROM [CoverArtType] WHERE Id = 7 AND Name = 'BandLogo');

INSERT INTO [HistoryType] (Id, Name)
SELECT 0, 'Added'
WHERE NOT EXISTS (SELECT 1 FROM [HistoryType] WHERE Id = 0 AND Name = 'Added');

INSERT INTO [HistoryType] (Id, Name)
SELECT 1, 'Modified'
WHERE NOT EXISTS (SELECT 1 FROM [HistoryType] WHERE Id = 1 AND Name = 'Modified');

INSERT INTO [User] (Id, Name, EmailAddress, Password)
SELECT X'00000000000000000000000000000000', 'Admin', 'admin@localhost', @Password
WHERE NOT EXISTS (SELECT 1 FROM [User] WHERE Name = 'Admin');

INSERT INTO [Status] (Id, Enabled)
SELECT X'00000000000000000000000000000000', 1
WHERE NOT EXISTS (SELECT 1 FROM [Status] WHERE Id = X'00000000000000000000000000000000');

INSERT INTO [Role] (Id, Name)
SELECT 0, 'Administrator'
WHERE NOT EXISTS (SELECT 1 FROM [Role] WHERE Id = 0);

INSERT INTO [Role] (Id, Name)
SELECT 1, 'Playback'
WHERE NOT EXISTS (SELECT 1 FROM [Role] WHERE Id = 1);

INSERT INTO [Role] (Id, Name)
SELECT 2, 'Settings'
WHERE NOT EXISTS (SELECT 1 FROM [Role] WHERE Id = 2);

INSERT INTO [Role] (Id, Name)
SELECT 3, 'Download'
WHERE NOT EXISTS (SELECT 1 FROM [Role] WHERE Id = 3);

INSERT INTO [UserToRole] (UserId, RoleId)
SELECT X'00000000000000000000000000000000', 0
WHERE NOT EXISTS (SELECT 1 FROM [UserToRole] WHERE UserId = X'00000000000000000000000000000000' AND RoleId = 0);