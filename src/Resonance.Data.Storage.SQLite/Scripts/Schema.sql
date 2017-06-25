CREATE TABLE [Album](
    [Id] GUID PRIMARY KEY ASC ON CONFLICT ABORT NOT NULL ON CONFLICT ABORT UNIQUE ON CONFLICT ABORT, 
    [Name] NVARCHAR NOT NULL ON CONFLICT ABORT, 
    [CollectionId] GUID NOT NULL ON CONFLICT ABORT REFERENCES Collection([Id]) ON DELETE CASCADE ON UPDATE NO ACTION, 
    [ArtistIds] NVARCHAR, 
    UNIQUE([CollectionId] ASC, [ArtistIds] ASC, [Name] ASC) ON CONFLICT ABORT);

CREATE TABLE [Artist](
    [Id] GUID PRIMARY KEY ASC ON CONFLICT ABORT NOT NULL ON CONFLICT ABORT UNIQUE ON CONFLICT ABORT, 
    [Name] NVARCHAR NOT NULL ON CONFLICT ABORT, 
    [CollectionId] GUID NOT NULL ON CONFLICT ABORT REFERENCES Collection([Id]) ON DELETE CASCADE ON UPDATE NO ACTION, 
    UNIQUE([CollectionId] ASC, [Name] ASC) ON CONFLICT ABORT);

CREATE TABLE [ArtistToAlbum](
    [ArtistId] GUID NOT NULL ON CONFLICT ABORT REFERENCES Artist([Id]) ON DELETE CASCADE ON UPDATE NO ACTION, 
    [AlbumId] GUID NOT NULL ON CONFLICT ABORT REFERENCES Album([Id]) ON DELETE CASCADE ON UPDATE NO ACTION, 
    UNIQUE([ArtistId] ASC, [AlbumId] ASC) ON CONFLICT ABORT);

CREATE TABLE [ArtistToTrack](
    [ArtistId] GUID NOT NULL ON CONFLICT ABORT REFERENCES Artist([Id]) ON DELETE CASCADE ON UPDATE NO ACTION, 
    [TrackId] GUID NOT NULL ON CONFLICT ABORT REFERENCES Track([Id]) ON DELETE CASCADE ON UPDATE NO ACTION, 
    UNIQUE([ArtistId] ASC, [TrackId] ASC) ON CONFLICT ABORT);

CREATE TABLE [Chat](
    [UserId] GUID NOT NULL ON CONFLICT ABORT REFERENCES User([Id]) ON DELETE CASCADE ON UPDATE NO ACTION, 
    [Timestamp] DATETIME NOT NULL ON CONFLICT ABORT, 
    [Message] NVARCHAR NOT NULL ON CONFLICT ABORT);

CREATE TABLE [Collection](
    [Id] GUID PRIMARY KEY ASC ON CONFLICT ABORT NOT NULL ON CONFLICT ABORT UNIQUE ON CONFLICT ABORT, 
    [Name] NVARCHAR NOT NULL ON CONFLICT ABORT UNIQUE ON CONFLICT ABORT, 
    [Filter] NVARCHAR NOT NULL ON CONFLICT ABORT, 
    [Path] NVARCHAR NOT NULL ON CONFLICT ABORT, 
    UNIQUE([Name] COLLATE [NOCASE] ASC) ON CONFLICT ABORT);

CREATE TABLE [ComposerToTrack](
    [ArtistId] GUID NOT NULL ON CONFLICT ABORT REFERENCES Artist([Id]) ON DELETE CASCADE ON UPDATE NO ACTION, 
    [TrackId] GUID NOT NULL ON CONFLICT ABORT REFERENCES Track([Id]) ON DELETE CASCADE ON UPDATE NO ACTION, 
    UNIQUE([ArtistId] ASC, [TrackId] ASC) ON CONFLICT ABORT);

CREATE TABLE [CoverArt](
    [TrackId] GUID NOT NULL ON CONFLICT ABORT REFERENCES Track([Id]) ON DELETE CASCADE ON UPDATE NO ACTION, 
    [CoverArtTypeId] INT NOT NULL ON CONFLICT ABORT REFERENCES CoverArtType([Id]) ON DELETE CASCADE ON UPDATE NO ACTION, 
    [MimeType] NVARCHAR NOT NULL ON CONFLICT ABORT, 
    [Size] INT NOT NULL ON CONFLICT ABORT);

CREATE TABLE [CoverArtType](
    [Id] INT PRIMARY KEY ASC ON CONFLICT ABORT NOT NULL ON CONFLICT ABORT UNIQUE ON CONFLICT ABORT, 
    [Name] NVARCHAR NOT NULL ON CONFLICT ABORT UNIQUE ON CONFLICT ABORT, 
    UNIQUE([Name] ASC) ON CONFLICT ABORT);

CREATE TABLE [Disposition](
    [Id] GUID PRIMARY KEY ASC ON CONFLICT ABORT NOT NULL ON CONFLICT ABORT UNIQUE ON CONFLICT ABORT, 
    [CollectionId] GUID NOT NULL ON CONFLICT ABORT REFERENCES Collection([Id]) ON DELETE CASCADE, 
    [MediaTypeId] INT NOT NULL ON CONFLICT ABORT REFERENCES MediaType([Id]) ON DELETE CASCADE, 
    [Favorited] DATETIME, 
    [MediaId] GUID NOT NULL ON CONFLICT ABORT, 
    [UserId] GUID NOT NULL ON CONFLICT ABORT REFERENCES User([Id]) ON DELETE CASCADE, 
    [Rating] SMALLINT, 
    UNIQUE([UserId] ASC, [MediaId] ASC) ON CONFLICT ABORT);

CREATE TABLE [FileInfo](
    [TrackId] GUID NOT NULL ON CONFLICT ABORT UNIQUE ON CONFLICT ABORT REFERENCES Track([Id]) ON DELETE CASCADE ON UPDATE NO ACTION, 
    [DateCreated] DATETIME NOT NULL ON CONFLICT ABORT, 
    [DateModified] DATETIME NOT NULL ON CONFLICT ABORT, 
    [Size] INT NOT NULL ON CONFLICT ABORT, 
    [Visible] BOOL NOT NULL ON CONFLICT ABORT DEFAULT 1, 
    UNIQUE([TrackId] ASC) ON CONFLICT ABORT);

CREATE TABLE [Genre](
    [Id] GUID PRIMARY KEY ASC ON CONFLICT ABORT NOT NULL ON CONFLICT ABORT UNIQUE ON CONFLICT ABORT, 
    [Name] NVARCHAR NOT NULL ON CONFLICT ABORT, 
    [CollectionId] GUID NOT NULL ON CONFLICT ABORT REFERENCES Collection([Id]) ON DELETE CASCADE ON UPDATE NO ACTION, 
    UNIQUE([CollectionId] ASC, [Name] ASC) ON CONFLICT ABORT);

CREATE TABLE [GenreToArtist](
    [GenreId] GUID NOT NULL ON CONFLICT ABORT REFERENCES Genre([Id]) ON DELETE CASCADE ON UPDATE NO ACTION, 
    [ArtistId] GUID NOT NULL ON CONFLICT ABORT REFERENCES Artist([Id]) ON DELETE CASCADE ON UPDATE NO ACTION, 
    UNIQUE([GenreId] COLLATE [BINARY], [ArtistId] COLLATE [BINARY]) ON CONFLICT ABORT);

CREATE TABLE [GenreToTrack](
    [GenreId] GUID NOT NULL ON CONFLICT ABORT REFERENCES Genre([Id]) ON DELETE CASCADE ON UPDATE NO ACTION, 
    [TrackId] GUID NOT NULL ON CONFLICT ABORT REFERENCES Track([Id]) ON DELETE CASCADE ON UPDATE NO ACTION, 
    UNIQUE([GenreId] COLLATE [BINARY], [TrackId] COLLATE [BINARY]) ON CONFLICT ABORT);

CREATE TABLE [Hash](
    [TrackId] GUID NOT NULL ON CONFLICT ABORT UNIQUE ON CONFLICT ABORT REFERENCES Track([Id]) ON DELETE CASCADE ON UPDATE NO ACTION, 
    [HashTypeId] INT NOT NULL ON CONFLICT ABORT REFERENCES HashType([Id]) ON DELETE CASCADE ON UPDATE NO ACTION, 
    [Hash] NVARCHAR NOT NULL ON CONFLICT ABORT, 
    UNIQUE([TrackId] ASC, [HashTypeId] ASC) ON CONFLICT ABORT);

CREATE TABLE [HashType](
    [Id] INT PRIMARY KEY ASC NOT NULL ON CONFLICT ABORT UNIQUE ON CONFLICT ABORT, 
    [Name] NVARCHAR NOT NULL ON CONFLICT ABORT UNIQUE ON CONFLICT ABORT, 
    UNIQUE([Name] ASC) ON CONFLICT ABORT);

CREATE TABLE [History](
    [Id] GUID NOT NULL ON CONFLICT ABORT, 
    [Timestamp] DATETIME NOT NULL ON CONFLICT ABORT, 
    [HistoryTypeId] INT NOT NULL ON CONFLICT ABORT REFERENCES HistoryType([Id]) ON DELETE CASCADE ON UPDATE NO ACTION);

CREATE TABLE [HistoryType](
    [Id] INT PRIMARY KEY ASC ON CONFLICT ABORT NOT NULL ON CONFLICT ABORT UNIQUE ON CONFLICT ABORT, 
    [Name] NVARCHAR NOT NULL ON CONFLICT ABORT UNIQUE ON CONFLICT ABORT, 
    UNIQUE([Name] ASC) ON CONFLICT ABORT);

CREATE TABLE [LastFm](
    [Id] GUID PRIMARY KEY ASC ON CONFLICT ABORT NOT NULL ON CONFLICT ABORT UNIQUE ON CONFLICT ABORT COLLATE BINARY, 
    [LastFmId] NVARCHAR, 
    [MusicBrainzId] NVARCHAR UNIQUE ON CONFLICT ABORT, 
    [Url] NVARCHAR, 
    [Details] NVARCHAR, 
    [SmallImageUrl] NVARCHAR, 
    [MediumImageUrl] NVARCHAR, 
    [LargeImageUrl] NVARCHAR, 
    [LargestImageUrl] NVARCHAR);

CREATE TABLE [Marker](
    [TrackId] GUID NOT NULL ON CONFLICT ABORT REFERENCES Track([Id]) ON DELETE CASCADE, 
    [UserId] GUID NOT NULL ON CONFLICT ABORT REFERENCES User([Id]) ON DELETE CASCADE, 
    [Position] INT NOT NULL ON CONFLICT ABORT, 
    [Comment] NVARCHAR, 
    UNIQUE([TrackId], [UserId]) ON CONFLICT ABORT);

CREATE TABLE [MediaInfo](
    [Id] GUID PRIMARY KEY ASC ON CONFLICT ABORT NOT NULL ON CONFLICT ABORT UNIQUE ON CONFLICT ABORT, 
    [MediaId] GUID NOT NULL ON CONFLICT ABORT UNIQUE ON CONFLICT ABORT, 
    [MusicBrainzId] NVARCHAR, 
    [LastFmId] NVARCHAR, 
    UNIQUE([MediaId] COLLATE [BINARY]) ON CONFLICT ABORT);

CREATE TABLE [MediaType](
    [Id] INT PRIMARY KEY ASC ON CONFLICT ABORT NOT NULL ON CONFLICT ABORT UNIQUE ON CONFLICT ABORT, 
    [Name] NVARCHAR NOT NULL ON CONFLICT ABORT UNIQUE ON CONFLICT ABORT, 
    UNIQUE([Name]) ON CONFLICT ABORT);

CREATE TABLE [Playback](
    [Address] NVARCHAR, 
    [ClientId] NVARCHAR, 
    [TrackId] GUID NOT NULL ON CONFLICT ABORT REFERENCES Track([Id]) ON DELETE CASCADE ON UPDATE NO ACTION, 
    [Timestamp] DATETIME NOT NULL ON CONFLICT ABORT, 
    [UserId] GUID NOT NULL ON CONFLICT ABORT REFERENCES User([Id]) ON DELETE CASCADE ON UPDATE NO ACTION);

CREATE TABLE [Playlist](
    [Id] GUID PRIMARY KEY ASC ON CONFLICT ABORT NOT NULL ON CONFLICT ABORT UNIQUE ON CONFLICT ABORT, 
    [UserId] GUID NOT NULL ON CONFLICT ABORT REFERENCES User([Id]) ON DELETE CASCADE ON UPDATE NO ACTION, 
    [Name] NVARCHAR NOT NULL ON CONFLICT ABORT, 
    [Comment] NVARCHAR, 
    [Accessibility] INT NOT NULL ON CONFLICT ABORT, 
    UNIQUE([UserId], [Name]) ON CONFLICT ABORT);

CREATE TABLE [PlayQueue](
    [Id] GUID PRIMARY KEY ASC ON CONFLICT ABORT NOT NULL ON CONFLICT ABORT UNIQUE ON CONFLICT ABORT, 
    [UserId] GUID NOT NULL ON CONFLICT ABORT UNIQUE ON CONFLICT ABORT REFERENCES User([Id]) ON DELETE CASCADE ON UPDATE NO ACTION, 
    [ClientName] NVARCHAR NOT NULL ON CONFLICT ABORT, 
    [CurrentTrackId] GUID REFERENCES Track([Id]) ON DELETE SET NULL ON UPDATE NO ACTION, 
    [Position] INT, 
    UNIQUE([UserId] ASC) ON CONFLICT ABORT);

CREATE TABLE [ProducerToTrack](
    [ArtistId] GUID NOT NULL ON CONFLICT ABORT REFERENCES Artist([Id]) ON DELETE CASCADE ON UPDATE NO ACTION, 
    [TrackId] GUID NOT NULL ON CONFLICT ABORT REFERENCES Track([Id]) ON DELETE CASCADE ON UPDATE NO ACTION, 
    UNIQUE([TrackId] COLLATE [BINARY], [ArtistId] COLLATE [BINARY]) ON CONFLICT ABORT);

CREATE TABLE [ReplayGain](
    [TrackId] GUID NOT NULL ON CONFLICT ABORT UNIQUE ON CONFLICT ABORT REFERENCES Track([Id]) ON DELETE CASCADE ON UPDATE NO ACTION, 
    [AlbumGain] DOUBLE, 
    [AlbumPeak] DOUBLE, 
    [TrackGain] DOUBLE, 
    [TrackPeak] DOUBLE, 
    UNIQUE([TrackId] ASC) ON CONFLICT ABORT);

CREATE TABLE [Role](
    [Id] INT PRIMARY KEY ASC ON CONFLICT ABORT NOT NULL ON CONFLICT ABORT UNIQUE ON CONFLICT ABORT, 
    [Name] NVARCHAR NOT NULL ON CONFLICT ABORT UNIQUE ON CONFLICT ABORT, 
    UNIQUE([Name] ASC) ON CONFLICT ABORT);

CREATE TABLE [Schema](
    [Version] INT NOT NULL ON CONFLICT ABORT);

CREATE TABLE [Status](
    [Id] GUID PRIMARY KEY ASC ON CONFLICT ABORT NOT NULL ON CONFLICT ABORT UNIQUE ON CONFLICT ABORT, 
    [Enabled] BOOLEAN NOT NULL ON CONFLICT ABORT DEFAULT 1, 
    UNIQUE([Id]) ON CONFLICT ABORT);

CREATE TABLE [Style](
    [Id] GUID PRIMARY KEY ASC ON CONFLICT ABORT NOT NULL ON CONFLICT ABORT UNIQUE ON CONFLICT ABORT, 
    [CollectionId] GUID NOT NULL ON CONFLICT ABORT REFERENCES Collection([Id]) ON DELETE CASCADE ON UPDATE NO ACTION, 
    [Name] NVARCHAR NOT NULL ON CONFLICT ABORT, 
    UNIQUE([CollectionId] ASC, [Name] ASC) ON CONFLICT ABORT);

CREATE TABLE [StyleToArtist](
    [StyleId] GUID NOT NULL ON CONFLICT ABORT REFERENCES Style([Id]) ON DELETE CASCADE ON UPDATE NO ACTION, 
    [ArtistId] GUID NOT NULL ON CONFLICT ABORT REFERENCES Artist([Id]) ON DELETE CASCADE ON UPDATE NO ACTION, 
    UNIQUE([StyleId] ASC, [ArtistId] ASC) ON CONFLICT ABORT);

CREATE TABLE [StyleToTrack](
    [StyleId] GUID NOT NULL ON CONFLICT ABORT REFERENCES Style([Id]) ON DELETE CASCADE ON UPDATE NO ACTION, 
    [TrackId] GUID NOT NULL ON CONFLICT ABORT REFERENCES Track([Id]) ON DELETE CASCADE ON UPDATE NO ACTION, 
    UNIQUE([StyleId] ASC, [TrackId] ASC) ON CONFLICT ABORT);

CREATE TABLE [Track](
    [Id] GUID PRIMARY KEY ASC ON CONFLICT ABORT NOT NULL ON CONFLICT ABORT UNIQUE ON CONFLICT ABORT, 
    [Name] NVARCHAR, 
    [Comment] NVARCHAR, 
    [Disc] INT, 
    [Number] INT, 
    [ReleaseDate] INT, 
    [Duration] INT NOT NULL ON CONFLICT ABORT, 
    [Bitrate] INT NOT NULL, 
    [Channels] INT NOT NULL ON CONFLICT ABORT, 
    [SampleRate] INT NOT NULL ON CONFLICT ABORT, 
    [ContentType] NVARCHAR, 
    [Path] NVARCHAR NOT NULL, 
    [CollectionId] GUID NOT NULL ON CONFLICT ABORT REFERENCES Collection([Id]) ON DELETE CASCADE ON UPDATE NO ACTION, 
    UNIQUE([CollectionId] ASC, [Path] ASC) ON CONFLICT ABORT);

CREATE TABLE [TrackToAlbum](
    [TrackId] GUID NOT NULL ON CONFLICT ABORT REFERENCES Track([Id]) ON DELETE CASCADE ON UPDATE NO ACTION, 
    [AlbumId] GUID NOT NULL ON CONFLICT ABORT REFERENCES Album([Id]) ON DELETE CASCADE ON UPDATE NO ACTION, 
    UNIQUE([TrackId] ASC, [AlbumId] ASC) ON CONFLICT ABORT);

CREATE TABLE [TrackToPlaylist](
    [PlaylistId] GUID NOT NULL ON CONFLICT ABORT REFERENCES Playlist([Id]) ON DELETE CASCADE ON UPDATE NO ACTION, 
    [TrackId] GUID NOT NULL ON CONFLICT ABORT REFERENCES Track([Id]) ON DELETE CASCADE ON UPDATE NO ACTION, 
    [Position] INT NOT NULL ON CONFLICT ABORT, 
    UNIQUE([PlaylistId] ASC, [TrackId] ASC, [Position] ASC) ON CONFLICT ABORT);

CREATE TABLE [TrackToPlayQueue](
    [PlayQueueId] GUID NOT NULL ON CONFLICT ABORT COLLATE BINARY REFERENCES PlayQueue([Id]) ON DELETE CASCADE ON UPDATE NO ACTION, 
    [TrackId] GUID NOT NULL ON CONFLICT ABORT COLLATE BINARY REFERENCES Track([Id]) ON DELETE CASCADE ON UPDATE NO ACTION, 
    [Position] INT NOT NULL ON CONFLICT ABORT, 
    UNIQUE([PlayQueueId] ASC, [Position] ASC) ON CONFLICT ABORT);

CREATE TABLE [User](
    [Id] GUID PRIMARY KEY ASC NOT NULL ON CONFLICT ABORT UNIQUE ON CONFLICT ABORT COLLATE BINARY, 
    [Name] NVARCHAR NOT NULL ON CONFLICT ABORT UNIQUE ON CONFLICT ABORT COLLATE NOCASE, 
    [EmailAddress] NVARCHAR, 
    [Password] NVARCHAR NOT NULL ON CONFLICT ABORT, 
    UNIQUE([Name] ASC) ON CONFLICT ABORT);

CREATE TABLE [UserToCollection](
    [UserId] GUID NOT NULL ON CONFLICT ABORT COLLATE BINARY REFERENCES User([Id]) ON DELETE CASCADE, 
    [CollectionId] GUID NOT NULL ON CONFLICT ABORT COLLATE BINARY REFERENCES Collection([Id]) ON DELETE CASCADE, 
    UNIQUE([UserId] ASC, [CollectionId] ASC) ON CONFLICT ABORT);

CREATE TABLE [UserToRole](
    [UserId] GUID NOT NULL ON CONFLICT ABORT COLLATE BINARY REFERENCES User([Id]) ON DELETE CASCADE, 
    [RoleId] INT NOT NULL ON CONFLICT ABORT REFERENCES Role([Id]) ON DELETE CASCADE, 
    UNIQUE([UserId] ASC, [RoleId] ASC) ON CONFLICT ABORT);

CREATE INDEX [Album_ArtistIds]
ON [Album](
    [ArtistIds] ASC);

CREATE INDEX [Album_CollectionId]
ON [Album](
    [CollectionId] COLLATE [BINARY]);

CREATE INDEX [Album_Name]
ON [Album](
    [Name] ASC);

CREATE INDEX [ArtistToAlbum_AlbumId]
ON [ArtistToAlbum](
    [AlbumId]);

CREATE INDEX [ArtistToAlbum_ArtistId]
ON [ArtistToAlbum](
    [ArtistId] ASC);

CREATE INDEX [ArtistToTrack_ArtistId]
ON [ArtistToTrack](
    [ArtistId] ASC);

CREATE INDEX [ArtistToTrack_TrackId]
ON [ArtistToTrack](
    [TrackId] ASC);

CREATE INDEX [Artist_CollectionId]
ON [Artist](
    [CollectionId] ASC);

CREATE INDEX [Artist_Name]
ON [Artist](
    [Name] ASC);

CREATE INDEX [Chat_Timestamp]
ON [Chat](
    [Timestamp] ASC);

CREATE INDEX [Chat_UserId]
ON [Chat](
    [UserId] ASC);

CREATE INDEX [ComposerToTrack_ArtistId]
ON [ComposerToTrack](
    [ArtistId] ASC);

CREATE INDEX [ComposerToTrack_TrackId]
ON [ComposerToTrack](
    [TrackId] ASC);

CREATE INDEX [CoverArt_CoverArtTypeId]
ON [CoverArt](
    [CoverArtTypeId] ASC);

CREATE INDEX [CoverArt_TrackId]
ON [CoverArt](
    [TrackId] ASC);

CREATE INDEX [Disposition_MediaId]
ON [Disposition](
    [MediaId] ASC);

CREATE INDEX [Disposition_UserId]
ON [Disposition](
    [UserId] ASC);

CREATE INDEX [GenreToArtist_ArtistId]
ON [GenreToArtist](
    [ArtistId] ASC);

CREATE INDEX [GenreToArtist_GenreId]
ON [GenreToArtist](
    [GenreId] ASC);

CREATE INDEX [GenreToTrack_GenreId]
ON [GenreToTrack](
    [GenreId] ASC);

CREATE INDEX [GenreToTrack_TrackId]
ON [GenreToTrack](
    [TrackId] ASC);

CREATE INDEX [Genre_CollectionId]
ON [Genre](
    [CollectionId] ASC);

CREATE INDEX [Genre_Name]
ON [Genre](
    [Name] ASC);

CREATE INDEX [History_HistoryTypeId]
ON [History](
    [HistoryTypeId] ASC);

CREATE INDEX [History_Id]
ON [History](
    [Id] ASC);

CREATE INDEX [LastFm_MusicBrainzId]
ON [LastFm](
    [MusicBrainzId] ASC);

CREATE INDEX [MediaInfo_LastFmId]
ON [MediaInfo](
    [LastFmId] ASC);

CREATE INDEX [MediaInfo_MusicBrainzId]
ON [MediaInfo](
    [MusicBrainzId] ASC);

CREATE INDEX [Playback_TrackId]
ON [Playback](
    [TrackId] ASC);

CREATE INDEX [Playback_UserId]
ON [Playback](
    [UserId] ASC);

CREATE INDEX [Playlist_Accessibility]
ON [Playlist](
    [Accessibility] ASC);

CREATE INDEX [Playlist_Name]
ON [Playlist](
    [Name] ASC);

CREATE INDEX [Playlist_UserId]
ON [Playlist](
    [UserId] ASC);

CREATE INDEX [ProducerToTrack_ArtistId]
ON [ProducerToTrack](
    [ArtistId] ASC);

CREATE INDEX [ProducerToTrack_TrackId]
ON [ProducerToTrack](
    [TrackId] ASC);

CREATE INDEX [StyleToArtist_ArtistId]
ON [StyleToArtist](
    [ArtistId] ASC);

CREATE INDEX [StyleToArtist_StyleId]
ON [StyleToArtist](
    [StyleId] ASC);

CREATE INDEX [StyleToTrack_StyleId]
ON [StyleToTrack](
    [StyleId] ASC);

CREATE INDEX [StyleToTrack_TrackId]
ON [StyleToTrack](
    [TrackId] ASC);

CREATE INDEX [Style_CollectionId]
ON [Style](
    [CollectionId] COLLATE [BINARY]);

CREATE INDEX [Style_Name]
ON [Style](
    [Name] ASC);

CREATE INDEX [TrackToAlbum_AlbumId]
ON [TrackToAlbum](
    [AlbumId] ASC);

CREATE INDEX [TrackToAlbum_TrackId]
ON [TrackToAlbum](
    [TrackId] ASC);

CREATE INDEX [TrackToPlaylist_TrackId_PlaylistId]
ON [TrackToPlaylist](
    [TrackId] ASC, 
    [PlaylistId] ASC);

CREATE INDEX [TrackToPlayQueue_PlayQueueId]
ON [TrackToPlayQueue](
    [PlayQueueId] COLLATE [BINARY]);

CREATE INDEX [Track_Name]
ON [Track](
    [Name] COLLATE [NOCASE] ASC);

CREATE INDEX [Track_ReleaseDate]
ON [Track](
    [ReleaseDate] ASC);

CREATE INDEX [UserToCollection_CollectionId]
ON [UserToCollection](
    [CollectionId] COLLATE [BINARY]);

CREATE INDEX [UserToCollection_UserId]
ON [UserToCollection](
    [UserId] COLLATE [BINARY]);

CREATE INDEX [UserToRole_RoleId]
ON [UserToRole](
    [RoleId] ASC);

CREATE INDEX [UserToRole_UserId]
ON [UserToRole](
    [UserId] COLLATE [BINARY]);

CREATE TRIGGER [Album_History_Insert] AFTER INSERT ON [Album]
BEGIN
    INSERT INTO [History]
        ([Id], 
        [Timestamp], 
        [HistoryTypeId])
        VALUES ([New].[Id], STRFTIME ('%Y-%m-%dT%H:%M:%fZ', 'now'), 0);
END;

CREATE TRIGGER [Album_History_Update] AFTER UPDATE ON [Album]
BEGIN
    INSERT INTO [History]
        ([Id], 
        [Timestamp], 
        [HistoryTypeId])
        VALUES ([New].[Id], STRFTIME ('%Y-%m-%dT%H:%M:%fZ', 'now'), 1);
END;

CREATE TRIGGER [Artist_History_Insert] AFTER INSERT ON [Artist]
BEGIN
    INSERT INTO [History]
        ([Id], 
        [Timestamp], 
        [HistoryTypeId])
        VALUES ([New].[Id], STRFTIME ('%Y-%m-%dT%H:%M:%fZ', 'now'), 0);
END;

CREATE TRIGGER [Artist_History_Update] AFTER UPDATE ON [Artist]
BEGIN
    INSERT INTO [History]
        ([Id], 
        [Timestamp], 
        [HistoryTypeId])
        VALUES ([New].[Id], STRFTIME ('%Y-%m-%dT%H:%M:%fZ', 'now'), 1);
END;

CREATE TRIGGER [Collection_History_Insert] AFTER INSERT ON [Collection]
BEGIN
    INSERT INTO [History]
        ([Id], 
        [Timestamp], 
        [HistoryTypeId])
        VALUES ([New].[Id], STRFTIME ('%Y-%m-%dT%H:%M:%fZ', 'now'), 0);
END;

CREATE TRIGGER [Collection_History_Update] AFTER UPDATE ON [Collection]
BEGIN
    INSERT INTO [History]
        ([Id], 
        [Timestamp], 
        [HistoryTypeId])
        VALUES ([New].[Id], STRFTIME ('%Y-%m-%dT%H:%M:%fZ', 'now'), 1);
END;

CREATE TRIGGER [Genre_History_Insert] AFTER INSERT ON [Genre]
BEGIN
    INSERT INTO [History]
        ([Id], 
        [Timestamp], 
        [HistoryTypeId])
        VALUES ([New].[Id], STRFTIME ('%Y-%m-%dT%H:%M:%fZ', 'now'), 0);
END;

CREATE TRIGGER [Genre_History_Update] AFTER UPDATE ON [Genre]
BEGIN
    INSERT INTO [History]
        ([Id], 
        [Timestamp], 
        [HistoryTypeId])
        VALUES ([New].[Id], STRFTIME ('%Y-%m-%dT%H:%M:%fZ', 'now'), 1);
END;

CREATE TRIGGER [LastFm_History_Insert] AFTER INSERT ON [LastFm]
BEGIN
    INSERT INTO [History]
        ([Id], 
        [Timestamp], 
        [HistoryTypeId])
        VALUES ([New].[Id], STRFTIME ('%Y-%m-%dT%H:%M:%fZ', 'now'), 0);
END;

CREATE TRIGGER [LastFm_History_Update] AFTER UPDATE ON [LastFm]
BEGIN
    INSERT INTO [History]
        ([Id], 
        [Timestamp], 
        [HistoryTypeId])
        VALUES ([New].[Id], STRFTIME ('%Y-%m-%dT%H:%M:%fZ', 'now'), 1);
END;

CREATE TRIGGER [MediaInfo_History_Insert] AFTER INSERT ON [MediaInfo]
BEGIN
    INSERT INTO [History]
        ([Id], 
        [Timestamp], 
        [HistoryTypeId])
        VALUES ([New].[Id], STRFTIME ('%Y-%m-%dT%H:%M:%fZ', 'now'), 0);
END;

CREATE TRIGGER [MediaInfo_History_Update] AFTER UPDATE ON [MediaInfo]
BEGIN
    INSERT INTO [History]
        ([Id], 
        [Timestamp], 
        [HistoryTypeId])
        VALUES ([New].[Id], STRFTIME ('%Y-%m-%dT%H:%M:%fZ', 'now'), 1);
END;

CREATE TRIGGER [Playlist_History_Insert] AFTER INSERT ON [Playlist]
BEGIN
    INSERT INTO [History]
        ([Id], 
        [Timestamp], 
        [HistoryTypeId])
        VALUES ([New].[Id], STRFTIME ('%Y-%m-%dT%H:%M:%fZ', 'now'), 0);
END;

CREATE TRIGGER [Playlist_History_Update] AFTER UPDATE ON [Playlist]
BEGIN
    INSERT INTO [History]
        ([Id], 
        [Timestamp], 
        [HistoryTypeId])
        VALUES ([New].[Id], STRFTIME ('%Y-%m-%dT%H:%M:%fZ', 'now'), 1);
END;

CREATE TRIGGER [PlayQueue_History_Insert] AFTER INSERT ON [PlayQueue]
BEGIN
    INSERT INTO [History]
        ([Id], 
        [Timestamp], 
        [HistoryTypeId])
        VALUES ([New].[Id], STRFTIME ('%Y-%m-%dT%H:%M:%fZ', 'now'), 0);
END;

CREATE TRIGGER [PlayQueue_History_Update] AFTER UPDATE ON [PlayQueue]
BEGIN
    INSERT INTO [History]
        ([Id], 
        [Timestamp], 
        [HistoryTypeId])
        VALUES ([New].[Id], STRFTIME ('%Y-%m-%dT%H:%M:%fZ', 'now'), 1);
END;

CREATE TRIGGER [Style_History_Insert] AFTER INSERT ON [Style]
BEGIN
    INSERT INTO [History]
        ([Id], 
        [Timestamp], 
        [HistoryTypeId])
        VALUES ([New].[Id], STRFTIME ('%Y-%m-%dT%H:%M:%fZ', 'now'), 0);
END;

CREATE TRIGGER [Style_History_Update] AFTER UPDATE ON [Style]
BEGIN
    INSERT INTO [History]
        ([Id], 
        [Timestamp], 
        [HistoryTypeId])
        VALUES ([New].[Id], STRFTIME ('%Y-%m-%dT%H:%M:%fZ', 'now'), 1);
END;

CREATE TRIGGER [Track_History_Insert] AFTER INSERT ON [Track]
BEGIN
    INSERT INTO [History]
        ([Id], 
        [Timestamp], 
        [HistoryTypeId])
        VALUES ([New].[Id], STRFTIME ('%Y-%m-%dT%H:%M:%fZ', 'now'), 0);
END;

CREATE TRIGGER [Track_History_Update] AFTER UPDATE ON [Track]
BEGIN
    INSERT INTO [History]
        ([Id], 
        [Timestamp], 
        [HistoryTypeId])
        VALUES ([New].[Id], STRFTIME ('%Y-%m-%dT%H:%M:%fZ', 'now'), 1);
END;

CREATE TRIGGER [User_History_Insert] AFTER INSERT ON [User]
BEGIN
    INSERT INTO [History]
        ([Id], 
        [Timestamp], 
        [HistoryTypeId])
        VALUES ([New].[Id], STRFTIME ('%Y-%m-%dT%H:%M:%fZ', 'now'), 0);
END;

CREATE TRIGGER [User_History_Update] AFTER UPDATE ON [User]
BEGIN
    INSERT INTO [History]
        ([Id], 
        [Timestamp], 
        [HistoryTypeId])
        VALUES ([New].[Id], STRFTIME ('%Y-%m-%dT%H:%M:%fZ', 'now'), 1);
END;

