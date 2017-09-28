using Dapper;
using Microsoft.Data.Sqlite;
using Resonance.Common;
using Resonance.Data.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Resonance.Data.Storage.SQLite
{
    public class MetadataRepository : IMetadataRepository
    {
        private readonly Assembly _assembly;
        private readonly string _assemblyName;
        private readonly string _database;
        private readonly IDbConnection _dbConnection;
        private readonly string _path;
        private readonly ConcurrentDictionary<string, string> _scriptCache = new ConcurrentDictionary<string, string>();
        private IDbTransaction _transaction;

        public MetadataRepository(string path, string database)
        {
            _path = path;
            _database = database;
            _dbConnection = GetConnection();
            _dbConnection.Open();
            _dbConnection.ExecuteAsync("PRAGMA foreign_keys = ON; PRAGMA journal_mode = TRUNCATE; PRAGMA optimize;").ConfigureAwait(false).GetAwaiter().GetResult();
            _assembly = GetType().GetTypeInfo().Assembly;
            _assemblyName = _assembly.GetName().Name;

            CreateSchemaAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public async Task AddChatAsync(Chat chat, CancellationToken cancellationToken)
        {
            await InsertChatAsync(chat, cancellationToken).ConfigureAwait(false);
        }

        public async Task AddCollectionAsync(Collection collection, CancellationToken cancellationToken)
        {
            await InsertOrUpdateCollectionAsync(collection, cancellationToken).ConfigureAwait(false);
        }

        public async Task AddPlaybackAsync(Playback playback, CancellationToken cancellationToken)
        {
            await InsertPlaybackAsync(playback, cancellationToken).ConfigureAwait(false);
        }

        public async Task AddPlaylistAsync(Playlist playlist, CancellationToken cancellationToken)
        {
            await InsertOrUpdatePlaylistAsync(playlist, cancellationToken).ConfigureAwait(false);
        }

        public async Task AddPlayQueueAsync(PlayQueue playQueue, CancellationToken cancellationToken)
        {
            await InsertOrUpdatePlayQueueAsync(playQueue, cancellationToken).ConfigureAwait(false);
        }

        public async Task AddUserAsync(User user, CancellationToken cancellationToken)
        {
            await InsertOrUpdateUserAsync(user, cancellationToken).ConfigureAwait(false);
        }

        public void BeginTransaction(CancellationToken cancellationToken)
        {
            _transaction = _dbConnection.BeginTransaction();
        }

        public Task ClearCollectionAsync<T>(Guid? collectionId, CancellationToken cancellationToken) where T : ModelBase, ICollectionIdentifier
        {
            throw new NotImplementedException();
        }

        public async Task DeleteAlbumReferencesAsync(CancellationToken cancellationToken)
        {
            var transaction = _transaction ?? _dbConnection.BeginTransaction();

            try
            {
                var builder = new SqlBuilder();

                var query = builder.AddTemplate(GetScript("Album_Delete"));
                builder.Where(
                    @"Id IN
                    (
                        SELECT a.Id
                        FROM [Album] a
                        LEFT JOIN [TrackToAlbum] tta ON tta.AlbumId = a.Id
                        WHERE NOT EXISTS (SELECT NULL FROM [TrackToAlbum] tta WHERE tta.AlbumId = a.Id)
                        GROUP BY a.Id
                    )"
                );

                var commandDefinition = new CommandDefinition(query.RawSql, query.Parameters, transaction, cancellationToken: cancellationToken);

                await _dbConnection.ExecuteAsync(commandDefinition).ConfigureAwait(false);

                if (_transaction == null)
                {
                    transaction.Commit();
                }
            }
            catch
            {
                transaction.Rollback();
                throw;
            }

            if (_transaction == null)
            {
                transaction.Dispose();
            }
        }

        public async Task DeleteMarkerAsync(Guid userId, Guid trackId, CancellationToken cancellationToken)
        {
            var transaction = _transaction ?? _dbConnection.BeginTransaction();

            try
            {
                var commandDefinition = new CommandDefinition(GetScript("Marker_Delete"), new { TrackId = trackId, UserId = userId }, transaction, cancellationToken: cancellationToken);

                await _dbConnection.ExecuteAsync(commandDefinition).ConfigureAwait(false);

                if (_transaction == null)
                {
                    transaction.Commit();
                }
            }
            catch
            {
                transaction.Rollback();
                throw;
            }

            if (_transaction == null)
            {
                transaction.Dispose();
            }
        }

        public async Task DeletePlaylistAsync(Guid userId, Guid playlistId, CancellationToken cancellationToken)
        {
            var transaction = _transaction ?? _dbConnection.BeginTransaction();

            try
            {
                var commandDefinition = new CommandDefinition(GetScript("Playlist_Delete"), new { PlaylistId = playlistId, UserId = userId }, transaction, cancellationToken: cancellationToken);

                await _dbConnection.ExecuteAsync(commandDefinition).ConfigureAwait(false);

                var builder = new SqlBuilder();

                var query = builder.AddTemplate(GetScript("Playlist_Track_Delete"));
                builder.Where("PlaylistId = @PlaylistId", new { PlaylistId = playlistId });

                commandDefinition = new CommandDefinition(query.RawSql, query.Parameters, transaction, cancellationToken: cancellationToken);

                await _dbConnection.ExecuteAsync(commandDefinition).ConfigureAwait(false);

                if (_transaction == null)
                {
                    transaction.Commit();
                }
            }
            catch
            {
                transaction.Rollback();
                throw;
            }

            if (_transaction == null)
            {
                transaction.Dispose();
            }
        }

        public async Task DeletePlaylistTracksAsync(Guid playlistId, CancellationToken cancellationToken)
        {
            var transaction = _transaction ?? _dbConnection.BeginTransaction();

            try
            {
                var builder = new SqlBuilder();

                var query = builder.AddTemplate(GetScript("Playlist_Track_Delete"));
                builder.Where("PlaylistId = @PlaylistId", new { PlaylistId = playlistId });

                var commandDefinition = new CommandDefinition(query.RawSql, query.Parameters, transaction, cancellationToken: cancellationToken);

                await _dbConnection.ExecuteAsync(commandDefinition).ConfigureAwait(false);

                if (_transaction == null)
                {
                    transaction.Commit();
                }
            }
            catch
            {
                transaction.Rollback();
                throw;
            }

            if (_transaction == null)
            {
                transaction.Dispose();
            }
        }

        public async Task DeletePlayQueueAsync(Guid userId, CancellationToken cancellationToken)
        {
            var transaction = _transaction ?? _dbConnection.BeginTransaction();

            try
            {
                var commandDefinition = new CommandDefinition(GetScript("PlayQueue_Delete"), new { UserId = userId }, transaction, cancellationToken: cancellationToken);

                await _dbConnection.ExecuteAsync(commandDefinition).ConfigureAwait(false);

                commandDefinition = new CommandDefinition(GetScript("PlayQueue_Track_Delete"), new { UserId = userId, PlayQueueId = (Guid?)null }, transaction, cancellationToken: cancellationToken);

                await _dbConnection.ExecuteAsync(commandDefinition).ConfigureAwait(false);

                if (_transaction == null)
                {
                    transaction.Commit();
                }
            }
            catch
            {
                transaction.Rollback();
                throw;
            }

            if (_transaction == null)
            {
                transaction.Dispose();
            }
        }

        public async Task DeletePlayQueueTracksAsync(Guid playQueueId, CancellationToken cancellationToken)
        {
            var transaction = _transaction ?? _dbConnection.BeginTransaction();

            try
            {
                var commandDefinition = new CommandDefinition(GetScript("PlayQueue_Track_Delete"), new { UserId = (Guid?)null, PlayQueueId = playQueueId }, transaction, cancellationToken: cancellationToken);

                await _dbConnection.ExecuteAsync(commandDefinition).ConfigureAwait(false);

                if (_transaction == null)
                {
                    transaction.Commit();
                }
            }
            catch
            {
                transaction.Rollback();
                throw;
            }

            if (_transaction == null)
            {
                transaction.Dispose();
            }
        }

        public async Task DeleteTrackReferencesAsync(Track track, CancellationToken cancellationToken)
        {
            var transaction = _transaction ?? _dbConnection.BeginTransaction();

            try
            {
                var artistToTrackDeleteCommand = new CommandDefinition(GetScript("ArtistToTrack_Delete"), new { TrackId = track.Id }, transaction, cancellationToken: cancellationToken);

                await _dbConnection.ExecuteAsync(artistToTrackDeleteCommand).ConfigureAwait(false);

                var genreToTrackDeleteCommand = new CommandDefinition(GetScript("GenreToTrack_Delete"), new { TrackId = track.Id }, transaction, cancellationToken: cancellationToken);

                await _dbConnection.ExecuteAsync(genreToTrackDeleteCommand).ConfigureAwait(false);

                var trackToAlbumDeleteCommand = new CommandDefinition(GetScript("TrackToAlbum_Delete"), new { TrackId = track.Id }, transaction, cancellationToken: cancellationToken);

                await _dbConnection.ExecuteAsync(trackToAlbumDeleteCommand).ConfigureAwait(false);

                var builder = new SqlBuilder();

                var query = builder.AddTemplate(GetScript("Playlist_Track_Delete"));
                builder.Where("TrackId = @TrackId", new { TrackId = track.Id });

                var playlistTrackDeleteCommand = new CommandDefinition(query.RawSql, query.Parameters, transaction, cancellationToken: cancellationToken);

                await _dbConnection.ExecuteAsync(playlistTrackDeleteCommand).ConfigureAwait(false);

                if (_transaction == null)
                {
                    transaction.Commit();
                }
            }
            catch
            {
                transaction.Rollback();
                throw;
            }

            if (_transaction == null)
            {
                transaction.Dispose();
            }
        }

        public async Task DeleteUserAsync(Guid userId, CancellationToken cancellationToken)
        {
            var transaction = _transaction ?? _dbConnection.BeginTransaction();

            try
            {
                var commandDefinition = new CommandDefinition(GetScript("User_Delete"), new { UserId = userId }, transaction, cancellationToken: cancellationToken);

                await _dbConnection.ExecuteAsync(commandDefinition).ConfigureAwait(false);

                if (_transaction == null)
                {
                    transaction.Commit();
                }
            }
            catch
            {
                transaction.Rollback();
                throw;
            }

            if (_transaction == null)
            {
                transaction.Dispose();
            }
        }

        public void EndTransaction(bool commit, CancellationToken cancellationToken)
        {
            if (_transaction == null)
                return;

            try
            {
                if (commit)
                {
                    _transaction.Commit();
                }
                else
                {
                    _transaction.Rollback();
                }

                _transaction.Dispose();
            }
            catch
            {
                _transaction = null;
                return;
            }

            _transaction = null;
        }

        public async Task<MediaBundle<Album>> GetAlbumAsync(Guid userId, Guid id, bool populate, CancellationToken cancellationToken)
        {
            var builder = new SqlBuilder();

            var query = builder.AddTemplate(GetScript("Album_Select"), new { UserId = userId });
            builder.Where("a.Id = @AlbumId", new { AlbumId = id });

            var commandDefinition = new CommandDefinition(query.RawSql, transaction: _transaction, parameters: query.Parameters, cancellationToken: cancellationToken);

            var result = await _dbConnection.QueryFirstOrDefaultAsync(commandDefinition).ConfigureAwait(false);

            if (result == null)
            {
                return null;
            }

            MediaBundle<Album> mediaBundle = MediaBundle<Album>.FromDynamic(result, userId);

            if (populate)
            {
                await PopulateAlbumAsync(userId, mediaBundle, cancellationToken).ConfigureAwait(false);
            }

            return mediaBundle;
        }

        public async Task<MediaBundle<Album>> GetAlbumAsync(Guid userId, HashSet<Artist> artists, string name, Guid? collectionId, bool populate, CancellationToken cancellationToken)
        {
            var builder = new SqlBuilder();

            var query = builder.AddTemplate(GetScript("Album_Select"), new { UserId = userId });

            builder.Where("a.Name = @AlbumName", new { AlbumName = name });
            builder.Where("a.ArtistIds = @ArtistIds", new { ArtistIds = GetIds(artists) });

            if (collectionId.HasValue)
            {
                builder.Where("a.CollectionId = @CollectionId", new { CollectionId = collectionId });
            }

            var commandDefinition = new CommandDefinition(query.RawSql, transaction: _transaction, parameters: query.Parameters, cancellationToken: cancellationToken);

            var result = await _dbConnection.QueryFirstOrDefaultAsync(commandDefinition).ConfigureAwait(false);

            if (result == null)
            {
                return null;
            }

            MediaBundle<Album> mediaBundle = MediaBundle<Album>.FromDynamic(result, userId);

            if (populate)
            {
                await PopulateAlbumAsync(userId, mediaBundle, cancellationToken).ConfigureAwait(false);
            }

            return mediaBundle;
        }

        public async Task<IEnumerable<MediaBundle<Album>>> GetAlbumsAsync(Guid userId, Guid? collectionId, bool populate, CancellationToken cancellationToken)
        {
            var builder = new SqlBuilder();

            var query = builder.AddTemplate(GetScript("Album_Select"), new { UserId = userId });

            builder.AddClause("firstjoin", "LEFT JOIN [TrackToAlbum] tta ON tta.AlbumId = a.Id");
            builder.Where("tta.AlbumId IS NOT NULL");

            if (collectionId.HasValue)
            {
                builder.Where("a.CollectionId = @CollectionId", new { CollectionId = collectionId });
            }

            var commandDefinition = new CommandDefinition(query.RawSql, transaction: _transaction, parameters: query.Parameters, cancellationToken: cancellationToken);

            var results = await _dbConnection.QueryAsync(commandDefinition).ConfigureAwait(false);

            return await GetMediaBundleAsync<Album>(results, userId, populate, cancellationToken).ConfigureAwait(false);
        }

        public async Task<IEnumerable<MediaBundle<Album>>> GetAlbumsByArtistAsync(Guid userId, Guid artistId, bool populate, CancellationToken cancellationToken)
        {
            var builder = new SqlBuilder();

            var query = builder.AddTemplate(GetScript("Album_Select"), new { UserId = userId });

            builder.Join("[ArtistToAlbum] ata ON ata.AlbumId = a.Id");
            builder.Where("ata.ArtistId = @ArtistId", new { ArtistId = artistId });

            var commandDefinition = new CommandDefinition(query.RawSql, transaction: _transaction, parameters: query.Parameters, cancellationToken: cancellationToken);

            var trackArtistBuilder = new SqlBuilder();

            var trackArtistQuery = trackArtistBuilder.AddTemplate(GetScript("Album_Select"), new { UserId = userId });

            trackArtistBuilder.AddClause("firstjoin", "LEFT JOIN [TrackToAlbum] tta ON tta.AlbumId = a.Id");
            trackArtistBuilder.LeftJoin("[ArtistToTrack] att ON att.TrackId = tta.TrackId");
            trackArtistBuilder.Where("att.ArtistId = @ArtistId AND tta.AlbumId IS NOT NULL", new { ArtistId = artistId });

            var trackArtistCommandDefinition = new CommandDefinition(trackArtistQuery.RawSql, transaction: _transaction, parameters: trackArtistQuery.Parameters, cancellationToken: cancellationToken);

            var albumQueryTasks = new List<Task<IEnumerable<dynamic>>> {
                _dbConnection.QueryAsync(commandDefinition),
                _dbConnection.QueryAsync(trackArtistCommandDefinition)
            };

            var albumResults = await Task.WhenAll(albumQueryTasks);

            var uniqueResults = new Dictionary<Guid, dynamic>();

            foreach (var result in albumResults.SelectMany(a => a))
            {
                var id = DynamicExtensions.GetGuidFromDynamic(result.Id);

                if (uniqueResults.ContainsKey(id))
                {
                    continue;
                }

                uniqueResults[id] = result;
            }

            var mediaBundles = await GetMediaBundleAsync<Album>(uniqueResults.Values, userId, populate, cancellationToken).ConfigureAwait(false);

            return mediaBundles.OrderBy(m => m.Media.ReleaseDate);
        }

        public async Task<IEnumerable<MediaBundle<Album>>> GetAlbumsByGenreAsync(Guid userId, Guid genreId, bool populate, CancellationToken cancellationToken)
        {
            var builder = new SqlBuilder();

            var query = builder.AddTemplate(GetScript("Album_Select"), new { UserId = userId });

            builder.AddClause("firstjoin", "LEFT JOIN [TrackToAlbum] tta ON tta.AlbumId = a.Id");
            builder.Where("tta.AlbumId IS NOT NULL");
            builder.Join("[GenreToTrack] gtt ON gtt.TrackId = tta.TrackId");
            builder.Where("gtt.GenreId = @GenreId", new { GenreId = genreId });

            var commandDefinition = new CommandDefinition(query.RawSql, transaction: _transaction, parameters: query.Parameters, cancellationToken: cancellationToken);

            var results = await _dbConnection.QueryAsync(commandDefinition).ConfigureAwait(false);

            return await GetMediaBundleAsync<Album>(results, userId, populate, cancellationToken).ConfigureAwait(false);
        }

        public async Task<IEnumerable<MediaBundle<Album>>> GetAlphabeticalAlbumsAsync(Guid userId, int size, int offset, string genre, int? fromYear, int? toYear, Guid? collectionId, bool populate, CancellationToken cancellationToken)
        {
            var builder = new SqlBuilder();

            var query = builder.AddTemplate(GetScript("Album_Select"), new { UserId = userId });

            builder.AddClause("firstjoin", "LEFT JOIN [TrackToAlbum] tta ON tta.AlbumId = a.Id");
            builder.Where("tta.AlbumId IS NOT NULL");

            var yearProvided = fromYear.HasValue || toYear.HasValue;

            if (!string.IsNullOrWhiteSpace(genre) || yearProvided)
            {
                if (yearProvided)
                {
                    builder.Join("[Track] t ON t.Id = tta.TrackId");
                }
            }

            if (collectionId.HasValue)
            {
                builder.Where("a.CollectionId = @CollectionId", new { CollectionId = collectionId });
            }

            if (!string.IsNullOrWhiteSpace(genre))
            {
                builder.Join("[GenreToTrack] gtt ON gtt.TrackId = tta.TrackId");
                builder.Join("[Genre] g ON g.Id = gtt.GenreId");
                builder.Where("g.Name IN(@Genre)", new { Genre = genre });
            }

            var reverseYearSort = fromYear.GetValueOrDefault() > toYear.GetValueOrDefault();

            if (fromYear.HasValue)
            {
                builder.Where($"t.ReleaseDate {(reverseYearSort ? "<=" : ">=")} @FromYear", new { FromYear = fromYear });
            }

            if (toYear.HasValue)
            {
                builder.Where($"t.ReleaseDate {(reverseYearSort ? ">=" : "<=")} @ToYear", new { ToYear = toYear });
            }

            builder.OrderBy(yearProvided ? $"t.ReleaseDate {(reverseYearSort ? "DESC" : "ASC")}, a.Name ASC" : "a.Name ASC");

            builder.AddClause("limit", "LIMIT @Size OFFSET @Offset", new { Size = size, Offset = offset });

            var commandDefinition = new CommandDefinition(query.RawSql, transaction: _transaction, parameters: query.Parameters, cancellationToken: cancellationToken);

            var results = await _dbConnection.QueryAsync(commandDefinition).ConfigureAwait(false);

            return await GetMediaBundleAsync<Album>(results, userId, populate, cancellationToken).ConfigureAwait(false);
        }

        public async Task<IEnumerable<MediaBundle<Album>>> GetAlphabeticalByArtistAlbumsAsync(Guid userId, int size, int offset, string genre, int? fromYear, int? toYear, Guid? collectionId, bool populate, CancellationToken cancellationToken)
        {
            var builder = new SqlBuilder();

            var query = builder.AddTemplate(GetScript("Album_Select"), new { UserId = userId });

            var yearProvided = fromYear.HasValue || toYear.HasValue;

            builder.AddClause("firstjoin", "LEFT JOIN [TrackToAlbum] tta ON tta.AlbumId = a.Id");
            builder.Where("tta.AlbumId IS NOT NULL");
            builder.Join("[ArtistToTrack] att ON att.TrackId = tta.TrackId");
            builder.Join("[Artist] ar ON ar.Id = att.ArtistId");

            if (yearProvided)
            {
                builder.Join("[Track] t ON t.Id = tta.TrackId");
            }

            if (collectionId.HasValue)
            {
                builder.Where("a.CollectionId = @CollectionId", new { CollectionId = collectionId });
            }

            if (!string.IsNullOrWhiteSpace(genre))
            {
                builder.Join("[GenreToTrack] gtt ON gtt.TrackId = tta.TrackId");
                builder.Join("[Genre] g ON g.Id = gtt.GenreId");
                builder.Where("g.Name IN(@Genre)", new { Genre = genre });
            }

            var reverseYearSort = fromYear.GetValueOrDefault() > toYear.GetValueOrDefault();

            if (fromYear.HasValue)
            {
                builder.Where($"t.ReleaseDate {(reverseYearSort ? "<=" : ">=")} @FromYear", new { FromYear = fromYear });
            }

            if (toYear.HasValue)
            {
                builder.Where($"t.ReleaseDate {(reverseYearSort ? ">=" : "<=")} @ToYear", new { ToYear = toYear });
            }

            builder.OrderBy("ar.Name ASC");

            builder.AddClause("limit", "LIMIT @Size OFFSET @Offset", new { Size = size, Offset = offset });

            var commandDefinition = new CommandDefinition(query.RawSql, transaction: _transaction, parameters: query.Parameters, cancellationToken: cancellationToken);

            var results = await _dbConnection.QueryAsync(commandDefinition).ConfigureAwait(false);

            return await GetMediaBundleAsync<Album>(results, userId, populate, cancellationToken).ConfigureAwait(false);
        }

        public async Task<MediaBundle<Artist>> GetArtistAsync(Guid userId, Guid id, CancellationToken cancellationToken)
        {
            var builder = new SqlBuilder();

            var query = builder.AddTemplate(GetScript("Artist_Select"), new { UserId = userId });
            builder.Where("a.Id = @ArtistId", new { ArtistId = id });

            var commandDefinition = new CommandDefinition(query.RawSql, transaction: _transaction, parameters: query.Parameters, cancellationToken: cancellationToken);

            var result = await _dbConnection.QueryFirstOrDefaultAsync(commandDefinition).ConfigureAwait(false);

            return result == null ? null : MediaBundle<Artist>.FromDynamic(result, userId);
        }

        public async Task<MediaBundle<Artist>> GetArtistAsync(Guid userId, string artist, Guid? collectionId, CancellationToken cancellationToken)
        {
            var builder = new SqlBuilder();

            var query = builder.AddTemplate(GetScript("Artist_Select"), new { UserId = userId });
            builder.Where("a.Name = @ArtistName", new { ArtistName = artist });

            if (collectionId.HasValue)
            {
                builder.Where("a.CollectionId = @CollectionId", new { CollectionId = collectionId });
            }

            var commandDefinition = new CommandDefinition(query.RawSql, transaction: _transaction, parameters: query.Parameters, cancellationToken: cancellationToken);

            var result = await _dbConnection.QueryFirstOrDefaultAsync(commandDefinition).ConfigureAwait(false);

            return result == null ? null : MediaBundle<Artist>.FromDynamic(result, userId);
        }

        public async Task<IEnumerable<MediaBundle<Artist>>> GetArtistsAsync(Guid userId, Guid? collectionId, CancellationToken cancellationToken)
        {
            var builder = new SqlBuilder();

            var query = builder.AddTemplate(GetScript("Artist_Select"), new { UserId = userId });
            builder.Where(@"a.Id NOT IN (
                  SELECT ar.Id FROM [Artist] ar
                  WHERE
                  NOT EXISTS (SELECT NULL FROM [ArtistToTrack] att WHERE att.ArtistId = ar.Id)
                  AND
                  NOT EXISTS (SELECT NULL FROM [ArtistToAlbum] ata WHERE ata.ArtistId = ar.Id)

                  UNION

                  SELECT ar.Id FROM [Artist] ar
                  JOIN [ArtistToAlbum] ata ON ata.ArtistId = ar.Id
                  WHERE
                  NOT EXISTS (SELECT NULL FROM [TrackToAlbum] tta WHERE tta.AlbumId = ata.AlbumId)
            ) ");

            if (collectionId.HasValue)
            {
                builder.Where("a.CollectionId = @CollectionId", new { CollectionId = collectionId });
            }

            var commandDefinition = new CommandDefinition(query.RawSql, transaction: _transaction, parameters: query.Parameters, cancellationToken: cancellationToken);

            var results = await _dbConnection.QueryAsync(commandDefinition).ConfigureAwait(false);

            return await GetMediaBundleAsync<Artist>(results, userId, false, cancellationToken).ConfigureAwait(false);
        }

        public async Task<IEnumerable<MediaBundle<Artist>>> GetArtistsByAlbumAsync(Guid userId, Guid albumId, CancellationToken cancellationToken)
        {
            var builder = new SqlBuilder();

            var query = builder.AddTemplate(GetScript("Artist_Select"), new { UserId = userId });
            builder.Join("[ArtistToAlbum] ata ON ata.ArtistId = a.Id");
            builder.Where("ata.AlbumId = @AlbumId", new { AlbumId = albumId });
            builder.OrderBy("ata.[rowid]");

            var commandDefinition = new CommandDefinition(query.RawSql, transaction: _transaction, parameters: query.Parameters, cancellationToken: cancellationToken);

            var results = await _dbConnection.QueryAsync(commandDefinition).ConfigureAwait(false);

            return await GetMediaBundleAsync<Artist>(results, userId, false, cancellationToken).ConfigureAwait(false);
        }

        public async Task<IEnumerable<MediaBundle<Artist>>> GetArtistsByTrackAsync(Guid userId, Guid trackId, CancellationToken cancellationToken)
        {
            var builder = new SqlBuilder();

            var query = builder.AddTemplate(GetScript("Artist_Select"), new { UserId = userId });
            builder.Join("[ArtistToTrack] att ON att.ArtistId = a.Id");
            builder.Where("att.TrackId = @TrackId", new { TrackId = trackId });
            builder.OrderBy("att.[rowid]");

            var commandDefinition = new CommandDefinition(query.RawSql, transaction: _transaction, parameters: query.Parameters, cancellationToken: cancellationToken);

            var results = await _dbConnection.QueryAsync(commandDefinition).ConfigureAwait(false);

            return await GetMediaBundleAsync<Artist>(results, userId, false, cancellationToken).ConfigureAwait(false);
        }

        public async Task<double?> GetAverageRatingAsync(Guid mediaId, CancellationToken cancellationToken)
        {
            var commandDefinition = new CommandDefinition(GetScript("Disposition_AverageByMediaId_Select"), transaction: _transaction, parameters: new { MediaId = mediaId }, cancellationToken: cancellationToken);

            return await _dbConnection.ExecuteScalarAsync<double?>(commandDefinition).ConfigureAwait(false);
        }

        public async Task<IEnumerable<Chat>> GetChatAsync(DateTime? since, CancellationToken cancellationToken)
        {
            var builder = new SqlBuilder();

            var query = builder.AddTemplate(GetScript("Chat_Select"));

            if (since.HasValue)
            {
                builder.Where("STRFTIME ('%Y-%m-%dT%H:%M:%fZ', c.Timestamp) > STRFTIME ('%Y-%m-%dT%H:%M:%fZ', @Timestamp)", new { Timestamp = since.Value });
            }

            builder.OrderBy("c.Timestamp DESC");

            var commandDefinition = new CommandDefinition(query.RawSql, transaction: _transaction, parameters: query.Parameters, cancellationToken: cancellationToken);

            var chatMessages = new List<Chat>();

            var results = await _dbConnection.QueryAsync(commandDefinition).ConfigureAwait(false);

            foreach (var result in results)
            {
                Chat chat = Chat.FromDynamic(result);

                chat.User = await GetUserAsync(chat.User.Id, cancellationToken).ConfigureAwait(false);

                chatMessages.Add(chat);
            }

            return chatMessages;
        }

        public async Task<IEnumerable<Collection>> GetCollectionsAsync(CancellationToken cancellationToken)
        {
            var builder = new SqlBuilder();

            var query = builder.AddTemplate(GetScript("Collection_Select"));

            var commandDefinition = new CommandDefinition(query.RawSql, transaction: _transaction, cancellationToken: cancellationToken);

            var results = await _dbConnection.QueryAsync(commandDefinition).ConfigureAwait(false);

            return results.Select(result => (Collection)Collection.FromDynamic(result)).ToList();
        }

        public async Task<Disposition> GetDispositionAsync(Guid userId, Guid mediaId, CancellationToken cancellationToken)
        {
            var builder = new SqlBuilder();

            var query = builder.AddTemplate(GetScript("Disposition_Select"));
            builder.Where("d.MediaId = @MediaId", new { MediaId = mediaId });
            builder.Where("d.UserId = @UserId", new { UserId = userId });

            var commandDefinition = new CommandDefinition(query.RawSql, transaction: _transaction, parameters: query.Parameters, cancellationToken: cancellationToken);

            var result = await _dbConnection.QueryFirstOrDefaultAsync(commandDefinition).ConfigureAwait(false);

            return result == null ? null : Disposition.FromDynamic(result);
        }

        public Task<IEnumerable<Disposition>> GetDispositionsAsync(Guid userId, MediaType mediaType, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<MediaBundle<Album>>> GetFavoritedAlbumsAsync(Guid userId, int size, int offset, string genre, int? fromYear, int? toYear, Guid? collectionId, bool populate, CancellationToken cancellationToken)
        {
            var builder = new SqlBuilder();

            var query = builder.AddTemplate(GetScript("Album_Select"), new { UserId = userId });

            builder.AddClause("firstjoin", "LEFT JOIN [TrackToAlbum] tta ON tta.AlbumId = a.Id");
            builder.Where("tta.AlbumId IS NOT NULL");

            var yearProvided = fromYear.HasValue || toYear.HasValue;

            if (!string.IsNullOrWhiteSpace(genre) || yearProvided)
            {
                if (yearProvided)
                {
                    builder.Join("[Track] t ON t.Id = tta.TrackId");
                }
            }

            if (collectionId.HasValue)
            {
                builder.Where("a.CollectionId = @CollectionId", new { CollectionId = collectionId });
            }

            if (!string.IsNullOrWhiteSpace(genre))
            {
                builder.Join("[GenreToTrack] gtt ON gtt.TrackId = tta.TrackId");
                builder.Join("[Genre] g ON g.Id = gtt.GenreId");
                builder.Where("g.Name IN(@Genre)", new { Genre = genre });
            }

            var reverseYearSort = fromYear.GetValueOrDefault() > toYear.GetValueOrDefault();

            if (fromYear.HasValue)
            {
                builder.Where($"t.ReleaseDate {(reverseYearSort ? "<=" : ">=")} @FromYear", new { FromYear = fromYear });
            }

            if (toYear.HasValue)
            {
                builder.Where($"t.ReleaseDate {(reverseYearSort ? ">=" : "<=")} @ToYear", new { ToYear = toYear });
            }

            builder.Where("d.Favorited IS NOT NULL");

            builder.OrderBy("d.Favorited DESC, a.Name ASC");

            builder.AddClause("limit", "LIMIT @Size OFFSET @Offset", new { Size = size, Offset = offset });

            var commandDefinition = new CommandDefinition(query.RawSql, transaction: _transaction, parameters: query.Parameters, cancellationToken: cancellationToken);

            var results = await _dbConnection.QueryAsync(commandDefinition).ConfigureAwait(false);

            return await GetMediaBundleAsync<Album>(results, userId, populate, cancellationToken).ConfigureAwait(false);
        }

        public async Task<IEnumerable<MediaBundle<T>>> GetFavoritedAsync<T>(Guid userId, Guid? collectionId, bool populate, CancellationToken cancellationToken) where T : MediaBase, ISearchable, ICollectionIdentifier
        {
            var genericType = typeof(T);
            var builder = new SqlBuilder();
            Dapper.SqlBuilder.Template query;

            if (genericType == typeof(Artist))
            {
                query = builder.AddTemplate(GetScript("Artist_Select"), new { UserId = userId });
                builder.Where(@"a.Id NOT IN (
                  SELECT ar.Id FROM [Artist] ar
                  WHERE
                  NOT EXISTS (SELECT NULL FROM [ArtistToTrack] att WHERE att.ArtistId = ar.Id)
                  AND
                  NOT EXISTS (SELECT NULL FROM [ArtistToAlbum] ata WHERE ata.ArtistId = ar.Id)

                  UNION

                  SELECT ar.Id FROM [Artist] ar
                  JOIN [ArtistToAlbum] ata ON ata.ArtistId = ar.Id
                  WHERE
                  NOT EXISTS (SELECT NULL FROM [TrackToAlbum] tta WHERE tta.AlbumId = ata.AlbumId)
                ) ");

                if (collectionId.HasValue)
                {
                    builder.Where("a.CollectionId = @CollectionId", new { CollectionId = collectionId });
                }

                builder.OrderBy("a.Name");
            }
            else if (genericType == typeof(Album))
            {
                query = builder.AddTemplate(GetScript("Album_Select"), new { UserId = userId });

                builder.AddClause("firstjoin", "LEFT JOIN [TrackToAlbum] tta ON tta.AlbumId = a.Id");
                builder.Where("tta.AlbumId IS NOT NULL");

                if (collectionId.HasValue)
                {
                    builder.Where("a.CollectionId = @CollectionId", new { CollectionId = collectionId });
                }

                builder.OrderBy("a.Name");
            }
            else if (genericType == typeof(Track))
            {
                query = builder.AddTemplate(GetScript("Track_Select"), new { UserId = userId });

                if (collectionId.HasValue)
                {
                    builder.Where("t.CollectionId = @CollectionId", new { CollectionId = collectionId });
                }

                builder.OrderBy("t.Name");
            }
            else
            {
                return null;
            }

            builder.Where("d.Favorited IS NOT NULL");

            var commandDefinition = new CommandDefinition(query.RawSql, transaction: _transaction, parameters: query.Parameters, cancellationToken: cancellationToken);

            var results = await _dbConnection.QueryAsync(commandDefinition).ConfigureAwait(false);

            if (results == null)
            {
                return null;
            }

            return await GetMediaBundleAsync<T>(results, userId, populate, cancellationToken).ConfigureAwait(false);
        }

        public async Task<Genre> GetGenreAsync(string genre, Guid? collectionId, CancellationToken cancellationToken)
        {
            var builder = new SqlBuilder();

            var query = builder.AddTemplate(GetScript("Genre_Select"));

            if (collectionId.HasValue)
            {
                builder.Where("g.CollectionId = @CollectionId", new { CollectionId = collectionId });
            }

            builder.Where("g.Name = @GenreName", new { GenreName = genre });

            var commandDefinition = new CommandDefinition(query.RawSql, transaction: _transaction, parameters: query.Parameters, cancellationToken: cancellationToken);

            var result = await _dbConnection.QueryFirstOrDefaultAsync(commandDefinition).ConfigureAwait(false);

            return result == null ? null : Genre.FromDynamic(result);
        }

        public async Task<Dictionary<string, Tuple<int, int>>> GetGenreCountsAsync(Guid? collectionId, CancellationToken cancellationToken)
        {
            var builder = new SqlBuilder();

            var query = builder.AddTemplate(GetScript("GenreCounts_Select"));

            var commandDefinition = new CommandDefinition(query.RawSql, transaction: _transaction, parameters: query.Parameters, cancellationToken: cancellationToken);

            var results = await _dbConnection.QueryAsync(commandDefinition).ConfigureAwait(false);

            if (results == null)
            {
                return null;
            }

            var genreCounts = new Dictionary<string, Tuple<int, int>>();

            foreach (var result in results)
            {
                string genreName = result.Genre;
                int songCount = result.SongCount == null ? 0 : DynamicExtensions.GetIntFromDynamic(result.SongCount);
                int albumCount = result.AlbumCount == null ? 0 : DynamicExtensions.GetIntFromDynamic(result.AlbumCount);

                genreCounts[genreName] = new Tuple<int, int>(songCount, albumCount);
            }

            return genreCounts;
        }

        public async Task<IEnumerable<Genre>> GetGenresAsync(Guid? collectionId, CancellationToken cancellationToken)
        {
            var builder = new SqlBuilder();

            var query = builder.AddTemplate(GetScript("Genre_Select"));
            builder.LeftJoin("[GenreToTrack] gtt ON gtt.GenreId = g.Id");
            builder.Where("gtt.GenreId IS NOT NULL");

            if (collectionId.HasValue)
            {
                builder.Where("g.CollectionId = @CollectionId", new { CollectionId = collectionId });
            }

            var commandDefinition = new CommandDefinition(query.RawSql, transaction: _transaction, parameters: query.Parameters, cancellationToken: cancellationToken);

            var results = await _dbConnection.QueryAsync(commandDefinition).ConfigureAwait(false);

            return results.Select(result => (Genre)Genre.FromDynamic(result)).ToList();
        }

        public async Task<IEnumerable<Genre>> GetGenresByTrackAsync(Guid trackId, CancellationToken cancellationToken)
        {
            var builder = new SqlBuilder();

            var query = builder.AddTemplate(GetScript("Genre_Select"));
            builder.Join("[GenreToTrack] gtt ON gtt.GenreId = g.Id");
            builder.Where("gtt.TrackId = @TrackId", new { TrackId = trackId });
            builder.OrderBy("gtt.[rowid]");

            var commandDefinition = new CommandDefinition(query.RawSql, transaction: _transaction, parameters: query.Parameters, cancellationToken: cancellationToken);

            var results = await _dbConnection.QueryAsync(commandDefinition).ConfigureAwait(false);

            return results.Select(result => (Genre)Genre.FromDynamic(result)).ToList();
        }

        public async Task<IEnumerable<MediaBundle<Album>>> GetHighestRatedAlbumsAsync(Guid userId, int size, int offset, string genre, int? fromYear, int? toYear, Guid? collectionId, bool populate, CancellationToken cancellationToken)
        {
            var builder = new SqlBuilder();

            var query = builder.AddTemplate(GetScript("Album_Select"), new { UserId = userId });

            builder.AddClause("firstjoin", "LEFT JOIN [TrackToAlbum] tta ON tta.AlbumId = a.Id");
            builder.Where("tta.AlbumId IS NOT NULL");

            var yearProvided = fromYear.HasValue || toYear.HasValue;

            if (!string.IsNullOrWhiteSpace(genre) || yearProvided)
            {
                if (yearProvided)
                {
                    builder.Join("[Track] t ON t.Id = tta.TrackId");
                }
            }

            if (collectionId.HasValue)
            {
                builder.Where("a.CollectionId = @CollectionId", new { CollectionId = collectionId });
            }

            if (!string.IsNullOrWhiteSpace(genre))
            {
                builder.Join("[GenreToTrack] gtt ON gtt.TrackId = tta.TrackId");
                builder.Join("[Genre] g ON g.Id = gtt.GenreId");
                builder.Where("g.Name IN(@Genre)", new { Genre = genre });
            }

            var reverseYearSort = fromYear.GetValueOrDefault() > toYear.GetValueOrDefault();

            if (fromYear.HasValue)
            {
                builder.Where($"t.ReleaseDate {(reverseYearSort ? "<=" : ">=")} @FromYear", new { FromYear = fromYear });
            }

            if (toYear.HasValue)
            {
                builder.Where($"t.ReleaseDate {(reverseYearSort ? ">=" : "<=")} @ToYear", new { ToYear = toYear });
            }

            builder.Where("d.Rating IS NOT NULL");

            builder.OrderBy("d.Rating DESC, a.Name ASC");

            builder.AddClause("limit", "LIMIT @Size OFFSET @Offset", new { Size = size, Offset = offset });

            var commandDefinition = new CommandDefinition(query.RawSql, transaction: _transaction, parameters: query.Parameters, cancellationToken: cancellationToken);

            var results = await _dbConnection.QueryAsync(commandDefinition).ConfigureAwait(false);

            return await GetMediaBundleAsync<Album>(results, userId, populate, cancellationToken).ConfigureAwait(false);
        }

        public async Task<IEnumerable<Marker>> GetMarkersAsync(Guid userId, CancellationToken cancellationToken)
        {
            var builder = new SqlBuilder();

            var query = builder.AddTemplate(GetScript("Marker_Select"));
            builder.Where("m.UserId = @UserId", new { UserId = userId });

            var commandDefinition = new CommandDefinition(query.RawSql, transaction: _transaction, parameters: query.Parameters, cancellationToken: cancellationToken);

            var results = await _dbConnection.QueryAsync(commandDefinition).ConfigureAwait(false);

            return results.Select(result => (Marker)Marker.FromDynamic(result)).ToList();
        }

        public async Task<MediaInfo> GetMediaInfoAsync(Guid mediaId, CancellationToken cancellationToken)
        {
            var builder = new SqlBuilder();

            var query = builder.AddTemplate(GetScript("MediaInfo_Select"));
            builder.Where("mi.MediaId = @MediaId", new { MediaId = mediaId });

            var commandDefinition = new CommandDefinition(query.RawSql, transaction: _transaction, parameters: query.Parameters, cancellationToken: cancellationToken);

            var result = await _dbConnection.QueryFirstOrDefaultAsync(commandDefinition).ConfigureAwait(false);

            return result == null ? null : MediaInfo.FromDynamic(result);
        }

        public async Task<MediaType?> GetMediaTypeAsync(Guid mediaId, CancellationToken cancellationToken)
        {
            var builder = new SqlBuilder();

            var query = builder.AddTemplate(GetScript("MediaTypeId_Select"), new { Id = mediaId });

            var commandDefinition = new CommandDefinition(query.RawSql, transaction: _transaction, parameters: query.Parameters, cancellationToken: cancellationToken);

            var result = await _dbConnection.ExecuteScalarAsync<int?>(commandDefinition).ConfigureAwait(false);

            var mediaType = (MediaType?)result;

            return mediaType;
        }

        public async Task<IEnumerable<MediaBundle<Album>>> GetMostPlayedAlbumsAsync(Guid userId, int size, int offset, string genre, int? fromYear, int? toYear, Guid? collectionId, bool populate, CancellationToken cancellationToken)
        {
            var builder = new SqlBuilder();

            var query = builder.AddTemplate(GetScript("Album_Select"), new { UserId = userId });

            var yearProvided = fromYear.HasValue || toYear.HasValue;

            builder.AddClause("firstjoin", "LEFT JOIN [TrackToAlbum] tta ON tta.AlbumId = a.Id");
            builder.Where("tta.AlbumId IS NOT NULL");
            builder.Join(@"
                         (
                            SELECT tta.AlbumId, COUNT(*) AS [PlaybackCount]
                            FROM [TrackToAlbum] tta
                            JOIN [Playback] p ON (p.TrackId = tta.TrackId)
                            GROUP BY tta.AlbumId
                            ORDER BY [PlaybackCount] DESC
                        ) AS c ON c.AlbumId = a.Id");

            if (yearProvided)
            {
                builder.Join("[Track] t ON t.Id = tta.TrackId");
            }

            if (collectionId.HasValue)
            {
                builder.Where("a.CollectionId = @CollectionId", new { CollectionId = collectionId });
            }

            if (!string.IsNullOrWhiteSpace(genre))
            {
                builder.Join("[GenreToTrack] gtt ON gtt.TrackId = tta.TrackId");
                builder.Join("[Genre] g ON g.Id = gtt.GenreId");
                builder.Where("g.Name IN(@Genre)", new { Genre = genre });
            }

            var reverseYearSort = fromYear.GetValueOrDefault() > toYear.GetValueOrDefault();

            if (fromYear.HasValue)
            {
                builder.Where($"t.ReleaseDate {(reverseYearSort ? "<=" : ">=")} @FromYear", new { FromYear = fromYear });
            }

            if (toYear.HasValue)
            {
                builder.Where($"t.ReleaseDate {(reverseYearSort ? ">=" : "<=")} @ToYear", new { ToYear = toYear });
            }

            builder.AddClause("limit", "LIMIT @Size OFFSET @Offset", new { Size = size, Offset = offset });

            builder.OrderBy("c.[PlaybackCount] DESC, a.Name ASC");

            var commandDefinition = new CommandDefinition(query.RawSql, transaction: _transaction, parameters: query.Parameters, cancellationToken: cancellationToken);

            var results = await _dbConnection.QueryAsync(commandDefinition).ConfigureAwait(false);

            return await GetMediaBundleAsync<Album>(results, userId, populate, cancellationToken).ConfigureAwait(false);
        }

        public async Task<IEnumerable<MediaBundle<Album>>> GetMostRecentlyPlayedAlbumsAsync(Guid userId, int size, int offset, string genre, int? fromYear, int? toYear, Guid? collectionId, bool populate, CancellationToken cancellationToken)
        {
            var builder = new SqlBuilder();

            var query = builder.AddTemplate(GetScript("Album_Select"), new { UserId = userId });

            var yearProvided = fromYear.HasValue || toYear.HasValue;

            builder.AddClause("firstjoin", "LEFT JOIN [TrackToAlbum] tta ON tta.AlbumId = a.Id");
            builder.Where("tta.AlbumId IS NOT NULL");
            builder.Join(@"
                         (
                            SELECT DISTINCT tta.AlbumId, p.Timestamp
                            FROM [TrackToAlbum] tta
                            JOIN [Playback] p ON (p.TrackId = tta.TrackId)
                            GROUP BY tta.AlbumId
                        ) AS c ON c.AlbumId = a.Id");

            if (yearProvided)
            {
                builder.Join("[Track] t ON t.Id = tta.TrackId");
            }

            if (collectionId.HasValue)
            {
                builder.Where("a.CollectionId = @CollectionId", new { CollectionId = collectionId });
            }

            if (!string.IsNullOrWhiteSpace(genre))
            {
                builder.Join("[GenreToTrack] gtt ON gtt.TrackId = tta.TrackId");
                builder.Join("[Genre] g ON g.Id = gtt.GenreId");
                builder.Where("g.Name IN(@Genre)", new { Genre = genre });
            }

            var reverseYearSort = fromYear.GetValueOrDefault() > toYear.GetValueOrDefault();

            if (fromYear.HasValue)
            {
                builder.Where($"t.ReleaseDate {(reverseYearSort ? "<=" : ">=")} @FromYear", new { FromYear = fromYear });
            }

            if (toYear.HasValue)
            {
                builder.Where($"t.ReleaseDate {(reverseYearSort ? ">=" : "<=")} @ToYear", new { ToYear = toYear });
            }

            builder.AddClause("limit", "LIMIT @Size OFFSET @Offset", new { Size = size, Offset = offset });

            builder.OrderBy("c.Timestamp DESC, a.Name ASC");

            var commandDefinition = new CommandDefinition(query.RawSql, transaction: _transaction, parameters: query.Parameters, cancellationToken: cancellationToken);

            var results = await _dbConnection.QueryAsync(commandDefinition).ConfigureAwait(false);

            return await GetMediaBundleAsync<Album>(results, userId, populate, cancellationToken).ConfigureAwait(false);
        }

        public async Task<IEnumerable<MediaBundle<Album>>> GetNewestAlbumsAsync(Guid userId, int size, int offset, string genre, int? fromYear, int? toYear, Guid? collectionId, bool populate, CancellationToken cancellationToken)
        {
            var builder = new SqlBuilder();

            var query = builder.AddTemplate(GetScript("Album_Select"), new { UserId = userId });

            builder.AddClause("firstjoin", "LEFT JOIN [TrackToAlbum] tta ON tta.AlbumId = a.Id");
            builder.Where("tta.AlbumId IS NOT NULL");

            var yearProvided = fromYear.HasValue || toYear.HasValue;

            if (!string.IsNullOrWhiteSpace(genre) || yearProvided)
            {
                if (yearProvided)
                {
                    builder.Join("[Track] t ON t.Id = tta.TrackId");
                }
            }

            if (collectionId.HasValue)
            {
                builder.Where("a.CollectionId = @CollectionId", new { CollectionId = collectionId });
            }

            if (!string.IsNullOrWhiteSpace(genre))
            {
                builder.Join("[GenreToTrack] gtt ON gtt.TrackId = tta.TrackId");
                builder.Join("[Genre] g ON g.Id = gtt.GenreId");
                builder.Where("g.Name IN(@Genre)", new { Genre = genre });
            }

            var reverseYearSort = fromYear.GetValueOrDefault() > toYear.GetValueOrDefault();

            if (fromYear.HasValue)
            {
                builder.Where($"t.ReleaseDate {(reverseYearSort ? "<=" : ">=")} @FromYear", new { FromYear = fromYear });
            }

            if (toYear.HasValue)
            {
                builder.Where($"t.ReleaseDate {(reverseYearSort ? ">=" : "<=")} @ToYear", new { ToYear = toYear });
            }

            builder.AddClause("limit", "LIMIT @Size OFFSET @Offset", new { Size = size, Offset = offset });

            builder.OrderBy("ch.[Timestamp] DESC, a.Name ASC");

            var commandDefinition = new CommandDefinition(query.RawSql, transaction: _transaction, parameters: query.Parameters, cancellationToken: cancellationToken);

            var results = await _dbConnection.QueryAsync(commandDefinition).ConfigureAwait(false);

            return await GetMediaBundleAsync<Album>(results, userId, populate, cancellationToken).ConfigureAwait(false);
        }

        public async Task<Playlist> GetPlaylistAsync(Guid userId, Guid id, bool getTracks, CancellationToken cancellationToken)
        {
            var builder = new SqlBuilder();

            var query = builder.AddTemplate(GetScript("Playlist_Select"));
            builder.Where("p.Id = @Id", new { Id = id });

            var commandDefinition = new CommandDefinition(query.RawSql, transaction: _transaction, parameters: query.Parameters, cancellationToken: cancellationToken);

            var result = await _dbConnection.QueryFirstOrDefaultAsync(commandDefinition).ConfigureAwait(false);

            if (result == null)
            {
                return null;
            }

            Playlist playlist = Playlist.FromDynamic(result);
            playlist.User = await GetUserAsync(userId, cancellationToken).ConfigureAwait(false);

            if (getTracks)
            {
                var trackToPlaylistBuilder = new SqlBuilder();

                var trackToPlaylistQuery = trackToPlaylistBuilder.AddTemplate(GetScript("Playlist_Track_Select"));
                trackToPlaylistBuilder.Where("ttp.PlaylistId = @PlaylistId", new { PlaylistId = id });
                trackToPlaylistBuilder.OrderBy("ttp.Position");

                var trackToPlaylistCommandDefinition = new CommandDefinition(trackToPlaylistQuery.RawSql, transaction: _transaction, parameters: trackToPlaylistQuery.Parameters, cancellationToken: cancellationToken);

                var trackToPlaylistResults = await _dbConnection.QueryAsync(trackToPlaylistCommandDefinition).ConfigureAwait(false);

                var trackMediaBundles = new ConcurrentDictionary<int, MediaBundle<Track>>();

                Parallel.ForEach(trackToPlaylistResults, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, trackToPlaylistResult =>
                {
                    Guid trackId = DynamicExtensions.GetGuidFromDynamic(trackToPlaylistResult.TrackId);

                    var track = GetTrackAsync(userId, trackId, true, cancellationToken).ConfigureAwait(false).GetAwaiter().GetResult();

                    if (track != null)
                    {
                        trackMediaBundles[DynamicExtensions.GetIntFromDynamic(trackToPlaylistResult.Position)] = track;
                    }
                });

                playlist.Tracks = trackMediaBundles.OrderBy(t => t.Key).Select(t => t.Value).ToList();
            }

            return playlist;
        }

        public async Task<List<Playlist>> GetPlaylistsAsync(Guid userId, string username, bool getTracks, CancellationToken cancellationToken)
        {
            var builder = new SqlBuilder();

            var query = builder.AddTemplate(GetScript("Playlist_Select"));

            if (!string.IsNullOrWhiteSpace(username))
            {
                var user = await GetUserAsync(username, cancellationToken).ConfigureAwait(false);

                if (user != null)
                {
                    builder.Where("p.UserId = @AlternateUserId", new { AlternateUserId = user.Id });
                }
            }
            else
            {
                builder.Where("p.UserId = @UserId OR p.Accessibility = @Accessibility", new { UserId = userId, Accessibility = (int)Accessibility.Public });
            }

            var commandDefinition = new CommandDefinition(query.RawSql, transaction: _transaction, parameters: query.Parameters, cancellationToken: cancellationToken);

            var results = await _dbConnection.QueryAsync(commandDefinition).ConfigureAwait(false);

            var playlists = new List<Playlist>();

            foreach (var result in results)
            {
                var playlistId = DynamicExtensions.GetGuidFromDynamic(result.Id);

                var playlist = await GetPlaylistAsync(userId, playlistId, getTracks, cancellationToken).ConfigureAwait(false);

                if (playlist != null)
                {
                    playlists.Add(playlist);
                }
            }

            return playlists;
        }

        public async Task<PlayQueue> GetPlayQueueAsync(Guid userId, CancellationToken cancellationToken)
        {
            var builder = new SqlBuilder();

            var query = builder.AddTemplate(GetScript("PlayQueue_Select"));
            builder.Where("p.UserId = @UserId", new { UserId = userId });

            var commandDefinition = new CommandDefinition(query.RawSql, transaction: _transaction, parameters: query.Parameters, cancellationToken: cancellationToken);

            var result = await _dbConnection.QueryFirstOrDefaultAsync(commandDefinition).ConfigureAwait(false);

            if (result == null)
            {
                return null;
            }

            var playQueue = PlayQueue.FromDynamic(result);
            playQueue.User = await GetUserAsync(userId, cancellationToken).ConfigureAwait(false);

            var trackToPlayQueueBuilder = new SqlBuilder();

            var trackToPlayQueueQuery = trackToPlayQueueBuilder.AddTemplate(GetScript("PlayQueue_Track_Select"));
            trackToPlayQueueBuilder.Where("ttp.PlayQueueId = @PlayQueueId", new { PlayQueueId = playQueue.Id });
            trackToPlayQueueBuilder.OrderBy("ttp.Position");

            var trackToPlayQueueCommandDefinition = new CommandDefinition(trackToPlayQueueQuery.RawSql, transaction: _transaction, parameters: trackToPlayQueueQuery.Parameters, cancellationToken: cancellationToken);

            var trackToPlayQueueResults = await _dbConnection.QueryAsync(trackToPlayQueueCommandDefinition).ConfigureAwait(false);

            var trackMediaBundles = new List<MediaBundle<Track>>();

            foreach (var trackToPlayQueueResult in trackToPlayQueueResults)
            {
                var trackId = DynamicExtensions.GetGuidFromDynamic(trackToPlayQueueResult.TrackId);

                var track = await GetTrackAsync(userId, trackId, true, cancellationToken).ConfigureAwait(false);

                if (track != null)
                {
                    trackMediaBundles.Add(track);
                }
            }

            playQueue.Tracks = trackMediaBundles;

            return playQueue;
        }

        public async Task<IEnumerable<MediaBundle<Album>>> GetRandomAlbumsAsync(Guid userId, int size, int offset, string genre, int? fromYear, int? toYear, Guid? collectionId, bool populate, CancellationToken cancellationToken)
        {
            var builder = new SqlBuilder();

            var query = builder.AddTemplate(GetScript("Album_Select"), new { UserId = userId });

            builder.AddClause("firstjoin", "LEFT JOIN [TrackToAlbum] tta ON tta.AlbumId = a.Id");
            builder.Where("tta.AlbumId IS NOT NULL");

            var yearProvided = fromYear.HasValue || toYear.HasValue;

            if (!string.IsNullOrWhiteSpace(genre) || yearProvided)
            {
                if (yearProvided)
                {
                    builder.Join("[Track] t ON t.Id = tta.TrackId");
                }
            }

            if (collectionId.HasValue)
            {
                builder.Where("a.CollectionId = @CollectionId", new { CollectionId = collectionId });
            }

            if (!string.IsNullOrWhiteSpace(genre))
            {
                builder.Join("[GenreToTrack] gtt ON gtt.TrackId = tta.TrackId");
                builder.Join("[Genre] g ON g.Id = gtt.GenreId");
                builder.Where("g.Name IN(@Genre)", new { Genre = genre });
            }

            var reverseYearSort = fromYear.GetValueOrDefault() > toYear.GetValueOrDefault();

            if (fromYear.HasValue)
            {
                builder.Where($"t.ReleaseDate {(reverseYearSort ? "<=" : ">=")} @FromYear", new { FromYear = fromYear });
            }

            if (toYear.HasValue)
            {
                builder.Where($"t.ReleaseDate {(reverseYearSort ? ">=" : "<=")} @ToYear", new { ToYear = toYear });
            }

            builder.Where("a.Id IN (SELECT Id FROM Album ORDER BY RANDOM() LIMIT @Size + @Offset)", new { Size = size });

            builder.AddClause("limit", "LIMIT @Size OFFSET @Offset", new { Size = size, Offset = offset });

            if (yearProvided)
            {
                builder.OrderBy($"t.ReleaseDate {(reverseYearSort ? "DESC" : "ASC")}");
            }

            var commandDefinition = new CommandDefinition(query.RawSql, transaction: _transaction, parameters: query.Parameters, cancellationToken: cancellationToken);

            var results = await _dbConnection.QueryAsync(commandDefinition).ConfigureAwait(false);

            return await GetMediaBundleAsync<Album>(results, userId, populate, cancellationToken).ConfigureAwait(false);
        }

        public async Task<IEnumerable<MediaBundle<Track>>> GetRecentPlaybackAsync(Guid userId, bool populate, CancellationToken cancellationToken)
        {
            var builder = new SqlBuilder();

            var query = builder.AddTemplate(GetScript("Track_Select"), new { UserId = userId });
            builder.Join("[Playback] p ON p.TrackId = t.Id");
            builder.OrderBy("p.Timestamp DESC");
            builder.AddClause("addselect", ", p.Address, p.ClientId, p.Timestamp, p.UserId");
            builder.AddClause("limit", "LIMIT @Size", new { Size = 25 });

            var commandDefinition = new CommandDefinition(query.RawSql, transaction: _transaction, parameters: query.Parameters, cancellationToken: cancellationToken);

            var results = await _dbConnection.QueryAsync(commandDefinition).ConfigureAwait(false);

            return await GetMediaBundleAsync<Track>(results, userId, populate, cancellationToken).ConfigureAwait(false);
        }

        public async Task<IEnumerable<Role>> GetRolesForUserAsync(Guid userId, CancellationToken cancellationToken)
        {
            var builder = new SqlBuilder();

            var query = builder.AddTemplate(GetScript("Role_Select"));
            builder.Join("[UserToRole] utr ON utr.RoleId = r.Id");
            builder.Where("utr.UserId = @UserId", new { UserId = userId });

            var commandDefinition = new CommandDefinition(query.RawSql, transaction: _transaction, parameters: query.Parameters, cancellationToken: cancellationToken);

            var results = await _dbConnection.QueryAsync(commandDefinition).ConfigureAwait(false);

            return results.Select(result => (Role)result.Id).ToList();
        }

        public async Task<MediaBundle<Track>> GetTrackAsync(Guid userId, Guid id, bool populate, CancellationToken cancellationToken)
        {
            var builder = new SqlBuilder();

            var query = builder.AddTemplate(GetScript("Track_Select"), new { UserId = userId });
            builder.Where("t.Id = @TrackId", new { TrackId = id });

            var commandDefinition = new CommandDefinition(query.RawSql, transaction: _transaction, parameters: query.Parameters, cancellationToken: cancellationToken);

            var result = await _dbConnection.QueryFirstOrDefaultAsync(commandDefinition).ConfigureAwait(false);

            if (result == null)
            {
                return null;
            }

            MediaBundle<Track> mediaBundle = MediaBundle<Track>.FromDynamic(result, userId);

            if (populate)
            {
                await PopulateTrackAsync(userId, mediaBundle, cancellationToken).ConfigureAwait(false);
            }

            return mediaBundle;
        }

        public async Task<MediaBundle<Track>> GetTrackAsync(Guid userId, string artist, string track, Guid? collectionId, bool populate, CancellationToken cancellationToken)
        {
            var builder = new SqlBuilder();

            var query = builder.AddTemplate(GetScript("Track_Select"), new { UserId = userId });
            builder.Join("[ArtistToTrack] att ON att.TrackId = t.Id");
            builder.Join("[Artist] a ON a.Id = att.ArtistId");
            builder.Where("a.Name = @Artist", new { Artist = artist });
            builder.Where("t.Name = @Track", new { Track = track });
            builder.AddClause("limit", "LIMIT @Size", new { Size = 1 });

            if (collectionId.HasValue)
            {
                builder.Where("t.CollectionId = @CollectionId", new { CollectionId = collectionId });
            }

            var commandDefinition = new CommandDefinition(query.RawSql, transaction: _transaction, parameters: query.Parameters, cancellationToken: cancellationToken);

            var result = await _dbConnection.QueryFirstOrDefaultAsync(commandDefinition).ConfigureAwait(false);

            if (result == null)
            {
                return null;
            }

            MediaBundle<Track> mediaBundle = MediaBundle<Track>.FromDynamic(result, userId);

            if (populate)
            {
                await PopulateTrackAsync(userId, mediaBundle, cancellationToken).ConfigureAwait(false);
            }

            return mediaBundle;
        }

        public async Task<MediaBundle<Track>> GetTrackAsync(Guid userId, string path, Guid? collectionId, bool populate, CancellationToken cancellationToken)
        {
            var builder = new SqlBuilder();

            var query = builder.AddTemplate(GetScript("Track_Select"), new { UserId = userId });
            builder.Where("t.Path = @Path", new { Path = path });

            if (collectionId.HasValue)
            {
                builder.Where("t.CollectionId = @CollectionId", new { CollectionId = collectionId });
            }

            var commandDefinition = new CommandDefinition(query.RawSql, transaction: _transaction, parameters: query.Parameters, cancellationToken: cancellationToken);

            var result = await _dbConnection.QueryFirstOrDefaultAsync(commandDefinition).ConfigureAwait(false);

            if (result == null)
            {
                return null;
            }

            MediaBundle<Track> mediaBundle = MediaBundle<Track>.FromDynamic(result, userId);

            if (populate)
            {
                await PopulateTrackAsync(userId, mediaBundle, cancellationToken).ConfigureAwait(false);
            }

            return mediaBundle;
        }

        public async Task<IEnumerable<MediaBundle<Track>>> GetTracksAsync(Guid userId, int size, int offset, string genre, int? fromYear, int? toYear, Guid? collectionId, bool populate, bool randomize, CancellationToken cancellationToken)
        {
            var builder = new SqlBuilder();

            var query = builder.AddTemplate(GetScript("Track_Select"), new { UserId = userId });

            if (collectionId.HasValue)
            {
                builder.Where("t.CollectionId = @CollectionId", new { CollectionId = collectionId });
            }

            if (!string.IsNullOrWhiteSpace(genre))
            {
                builder.Where("g.Name IN(@Genre)", new { Genre = genre });
            }

            if (fromYear.HasValue)
            {
                builder.Where("t.ReleaseDate >= @FromYear", new { FromYear = fromYear });
            }

            if (toYear.HasValue)
            {
                builder.Where("t.ReleaseDate <= @ToYear", new { ToYear = toYear });
            }

            if (randomize)
            {
                builder.Where("t.Id IN (SELECT Id FROM Track ORDER BY RANDOM() LIMIT @Size + @Offset)", new { Size = size });
            }

            builder.AddClause("limit", "LIMIT @Size OFFSET @Offset", new { Size = size, Offset = offset });

            var commandDefinition = new CommandDefinition(query.RawSql, transaction: _transaction, parameters: query.Parameters, cancellationToken: cancellationToken);

            var results = await _dbConnection.QueryAsync(commandDefinition).ConfigureAwait(false);

            return await GetMediaBundleAsync<Track>(results, userId, populate, cancellationToken).ConfigureAwait(false);
        }

        public async Task<IEnumerable<MediaBundle<Track>>> GetTracksAsync(Guid userId, Guid? collectionId, CancellationToken cancellationToken)
        {
            var builder = new SqlBuilder();

            var query = builder.AddTemplate(GetScript("Track_Select"), new { UserId = userId });

            if (collectionId.HasValue)
            {
                builder.Where("t.CollectionId = @CollectionId", new { CollectionId = collectionId });
            }

            var commandDefinition = new CommandDefinition(query.RawSql, transaction: _transaction, parameters: query.Parameters, cancellationToken: cancellationToken);

            var results = await _dbConnection.QueryAsync(commandDefinition).ConfigureAwait(false);

            return await GetMediaBundleAsync<Track>(results, userId, false, cancellationToken).ConfigureAwait(false);
        }

        public async Task<IEnumerable<MediaBundle<Track>>> GetTracksByAlbumAsync(Guid userId, Guid albumId, bool populate, CancellationToken cancellationToken)
        {
            var builder = new SqlBuilder();

            var query = builder.AddTemplate(GetScript("Track_Select"), new { UserId = userId });
            builder.Where("tta.AlbumId = @AlbumId", new { AlbumId = albumId });

            var commandDefinition = new CommandDefinition(query.RawSql, transaction: _transaction, parameters: query.Parameters, cancellationToken: cancellationToken);

            var results = await _dbConnection.QueryAsync(commandDefinition).ConfigureAwait(false);

            return await GetMediaBundleAsync<Track>(results, userId, populate, cancellationToken).ConfigureAwait(false);
        }

        public async Task<IEnumerable<MediaBundle<Track>>> GetTracksByGenreAsync(Guid userId, Guid genreId, bool populate, CancellationToken cancellationToken)
        {
            var builder = new SqlBuilder();

            var query = builder.AddTemplate(GetScript("Track_Select"), new { UserId = userId });
            builder.Where("g.Id = @GenreId", new { GenreId = genreId });

            var commandDefinition = new CommandDefinition(query.RawSql, transaction: _transaction, parameters: query.Parameters, cancellationToken: cancellationToken);

            var results = await _dbConnection.QueryAsync(commandDefinition).ConfigureAwait(false);

            return await GetMediaBundleAsync<Track>(results, userId, populate, cancellationToken).ConfigureAwait(false);
        }

        public async Task<User> GetUserAsync(Guid userId, CancellationToken cancellationToken)
        {
            var builder = new SqlBuilder();

            var query = builder.AddTemplate(GetScript("User_Select"));
            builder.Where("u.Id = @UserId", new { UserId = userId });

            var commandDefinition = new CommandDefinition(query.RawSql, transaction: _transaction, parameters: query.Parameters, cancellationToken: cancellationToken);

            var result = await _dbConnection.QueryFirstOrDefaultAsync(commandDefinition).ConfigureAwait(false);

            return result == null ? null : User.FromDynamic(result);
        }

        public async Task<User> GetUserAsync(string username, CancellationToken cancellationToken)
        {
            var builder = new SqlBuilder();

            var query = builder.AddTemplate(GetScript("User_Select"));
            builder.Where("u.Name = @Username", new { Username = username });

            var commandDefinition = new CommandDefinition(query.RawSql, transaction: _transaction, parameters: query.Parameters, cancellationToken: cancellationToken);

            var result = await _dbConnection.QueryFirstOrDefaultAsync(commandDefinition).ConfigureAwait(false);

            return result == null ? null : User.FromDynamic(result);
        }

        public async Task<IEnumerable<User>> GetUsersAsync(CancellationToken cancellationToken)
        {
            var builder = new SqlBuilder();

            var query = builder.AddTemplate(GetScript("User_Select"));

            var commandDefinition = new CommandDefinition(query.RawSql, transaction: _transaction, cancellationToken: cancellationToken);

            var results = await _dbConnection.QueryAsync(commandDefinition).ConfigureAwait(false);

            return results.Select(result => (User)User.FromDynamic(result)).ToList();
        }

        public async Task InsertChatAsync(Chat chat, CancellationToken cancellationToken)
        {
            if (chat == null)
            {
                return;
            }

            var transaction = _transaction ?? _dbConnection.BeginTransaction();

            try
            {
                var commandDefinition = new CommandDefinition(GetScript("Chat_Insert"), new { UserId = chat.User.Id, chat.Timestamp, chat.Message }, transaction, cancellationToken: cancellationToken);

                await _dbConnection.ExecuteAsync(commandDefinition).ConfigureAwait(false);

                if (_transaction == null)
                {
                    transaction.Commit();
                }
            }
            catch
            {
                transaction.Rollback();
                throw;
            }

            if (_transaction == null)
            {
                transaction.Dispose();
            }
        }

        public async Task InsertOrUpdateAlbumAsync(Album album, CancellationToken cancellationToken)
        {
            if (album == null)
            {
                return;
            }

            var transaction = _transaction ?? _dbConnection.BeginTransaction();

            try
            {
                var commandDefinition = new CommandDefinition(GetScript("Album_Insert"), new { album.Id, album.Name, album.CollectionId, ArtistIds = GetIds(album.Artists.Select(a => a.Media)) }, transaction, cancellationToken: cancellationToken);

                await _dbConnection.ExecuteAsync(commandDefinition).ConfigureAwait(false);

                foreach (var artist in album.Artists)
                {
                    var artistToAlbumCommand = new CommandDefinition(GetScript("ArtistToAlbum_Insert"), new { ArtistId = artist.Media.Id, AlbumId = album.Id }, transaction, cancellationToken: cancellationToken);

                    await _dbConnection.ExecuteAsync(artistToAlbumCommand).ConfigureAwait(false);
                }

                if (_transaction == null)
                {
                    transaction.Commit();
                }
            }
            catch
            {
                transaction.Rollback();
                throw;
            }

            if (_transaction == null)
            {
                transaction.Dispose();
            }
        }

        public async Task InsertOrUpdateArtistAsync(Artist artist, CancellationToken cancellationToken)
        {
            if (artist == null)
            {
                return;
            }

            var transaction = _transaction ?? _dbConnection.BeginTransaction();

            try
            {
                var commandDefinition = new CommandDefinition(GetScript("Artist_Insert"), new { artist.Id, artist.Name, artist.CollectionId }, transaction, cancellationToken: cancellationToken);

                await _dbConnection.ExecuteAsync(commandDefinition).ConfigureAwait(false);

                if (_transaction == null)
                {
                    transaction.Commit();
                }
            }
            catch
            {
                transaction.Rollback();
                throw;
            }

            if (_transaction == null)
            {
                transaction.Dispose();
            }
        }

        public async Task InsertOrUpdateCollectionAsync(Collection collection, CancellationToken cancellationToken)
        {
            if (collection == null)
            {
                return;
            }

            var transaction = _transaction ?? _dbConnection.BeginTransaction();

            try
            {
                var collectionCommand = new CommandDefinition(GetScript("Collection_Upsert"), new { collection.Id, collection.Name, collection.Filter, collection.Path }, transaction, cancellationToken: cancellationToken);

                await _dbConnection.ExecuteAsync(collectionCommand).ConfigureAwait(false);

                var statusCommand = new CommandDefinition(GetScript("Status_Upsert"), new { collection.Id, Enabled = true }, transaction, cancellationToken: cancellationToken);

                await _dbConnection.ExecuteAsync(statusCommand).ConfigureAwait(false);

                if (_transaction == null)
                {
                    transaction.Commit();
                }
            }
            catch
            {
                transaction.Rollback();
                throw;
            }

            if (_transaction == null)
            {
                transaction.Dispose();
            }
        }

        public async Task InsertOrUpdateDispositionAsync(Disposition disposition, CancellationToken cancellationToken)
        {
            if (disposition == null)
            {
                return;
            }

            var transaction = _transaction ?? _dbConnection.BeginTransaction();

            try
            {
                if (disposition.MediaType != null)
                {
                    var collectionCommand = new CommandDefinition(GetScript("Disposition_Upsert"), new { disposition.Id, disposition.CollectionId, MediaTypeId = (int)disposition.MediaType, disposition.Favorited, disposition.MediaId, disposition.UserId, Rating = disposition.UserRating == 0 ? null : disposition.UserRating }, transaction, cancellationToken: cancellationToken);

                    await _dbConnection.ExecuteAsync(collectionCommand).ConfigureAwait(false);
                }

                if (_transaction == null)
                {
                    transaction.Commit();
                }
            }
            catch
            {
                transaction.Rollback();
                throw;
            }

            if (_transaction == null)
            {
                transaction.Dispose();
            }
        }

        public async Task InsertOrUpdateFileInfoAsync(Track track, CancellationToken cancellationToken)
        {
            if (track == null)
            {
                return;
            }

            var transaction = _transaction ?? _dbConnection.BeginTransaction();

            try
            {
                var fileInfoCommand = new CommandDefinition(GetScript("FileInfo_Upsert"), new
                {
                    track.Id,
                    DateCreated = track.DateFileCreated,
                    DateModified = track.DateFileModified,
                    track.Size,
                    track.Visible
                }, transaction, cancellationToken: cancellationToken);

                await _dbConnection.ExecuteAsync(fileInfoCommand).ConfigureAwait(false);

                if (_transaction == null)
                {
                    transaction.Commit();
                }
            }
            catch
            {
                transaction.Rollback();
                throw;
            }

            if (_transaction == null)
            {
                transaction.Dispose();
            }
        }

        public async Task InsertOrUpdateGenreAsync(Genre genre, CancellationToken cancellationToken)
        {
            if (genre == null)
            {
                return;
            }

            var transaction = _transaction ?? _dbConnection.BeginTransaction();

            try
            {
                var commandDefinition = new CommandDefinition(GetScript("Genre_Insert"), new { genre.Id, genre.Name, genre.CollectionId }, transaction, cancellationToken: cancellationToken);

                await _dbConnection.ExecuteAsync(commandDefinition).ConfigureAwait(false);

                if (_transaction == null)
                {
                    transaction.Commit();
                }
            }
            catch
            {
                transaction.Rollback();
                throw;
            }

            if (_transaction == null)
            {
                transaction.Dispose();
            }
        }

        public async Task InsertOrUpdateMarkerAsync(Marker marker, CancellationToken cancellationToken)
        {
            if (marker == null)
            {
                return;
            }

            var transaction = _transaction ?? _dbConnection.BeginTransaction();

            try
            {
                var fileInfoCommand = new CommandDefinition(GetScript("Marker_Upsert"), new
                {
                    TrackId = marker.TrackId,
                    UserId = marker.User.Id,
                    Position = marker.Position,
                    Comment = marker.Comment
                }, transaction, cancellationToken: cancellationToken);

                await _dbConnection.ExecuteAsync(fileInfoCommand).ConfigureAwait(false);

                if (_transaction == null)
                {
                    transaction.Commit();
                }
            }
            catch
            {
                transaction.Rollback();
                throw;
            }

            if (_transaction == null)
            {
                transaction.Dispose();
            }
        }

        public async Task InsertOrUpdateMediaInfoAsync(MediaInfo mediaInfo, CancellationToken cancellationToken)
        {
            if (mediaInfo == null)
            {
                return;
            }

            var transaction = _transaction ?? _dbConnection.BeginTransaction();

            try
            {
                if (mediaInfo.MediaId == Guid.Empty)
                {
                    return;
                }

                var lastFm = mediaInfo.LastFm;

                string lastFmId = null;

                if (lastFm != null)
                {
                    lastFmId = lastFm.LastFmId;
                }

                var mediaInfoCommandDefinition = new CommandDefinition(GetScript("MediaInfo_Upsert"), new { mediaInfo.Id, mediaInfo.MediaId, LastFmId = lastFmId, mediaInfo.MusicBrainzId }, transaction, cancellationToken: cancellationToken);

                await _dbConnection.ExecuteAsync(mediaInfoCommandDefinition).ConfigureAwait(false);

                if (lastFm != null)
                {
                    var lastFmCommandDefinition = new CommandDefinition(GetScript("LastFm_Upsert"), new { lastFm.Id, lastFm.LastFmId, mediaInfo.MusicBrainzId, Url = lastFm.Url.ToString(), lastFm.Details, SmallImageUrl = lastFm.SmallImageUrl?.ToString(), MediumImageUrl = lastFm.MediumImageUrl?.ToString(), LargeImageUrl = lastFm.LargeImageUrl?.ToString(), LargestImageUrl = lastFm.LargestImageUrl?.ToString() }, transaction, cancellationToken: cancellationToken);

                    await _dbConnection.ExecuteAsync(lastFmCommandDefinition).ConfigureAwait(false);
                }

                if (_transaction == null)
                {
                    transaction.Commit();
                }
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }

            if (_transaction == null)
            {
                transaction.Dispose();
            }
        }

        public async Task InsertOrUpdatePlaylistAsync(Playlist playlist, CancellationToken cancellationToken)
        {
            if (playlist == null)
            {
                return;
            }

            var transaction = _transaction ?? _dbConnection.BeginTransaction();

            try
            {
                var commandDefinition = new CommandDefinition(GetScript("Playlist_Upsert"), new { playlist.Id, UserId = playlist.User.Id, playlist.Name, playlist.Comment, Accessibility = (int)playlist.Accessibility }, transaction, cancellationToken: cancellationToken);

                await _dbConnection.ExecuteAsync(commandDefinition).ConfigureAwait(false);

                if (_transaction == null)
                {
                    transaction.Commit();
                }
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }

            if (_transaction == null)
            {
                transaction.Dispose();
            }
        }

        public async Task InsertOrUpdatePlaylistTrackAsync(Guid playlistId, Guid trackId, int position, CancellationToken cancellationToken)
        {
            var transaction = _transaction ?? _dbConnection.BeginTransaction();

            try
            {
                var commandDefinition = new CommandDefinition(GetScript("Playlist_Track_Insert"), new { PlaylistId = playlistId, TrackId = trackId, Position = position }, transaction, cancellationToken: cancellationToken);

                await _dbConnection.ExecuteAsync(commandDefinition).ConfigureAwait(false);

                if (_transaction == null)
                {
                    transaction.Commit();
                }
            }
            catch
            {
                transaction.Rollback();
                throw;
            }

            if (_transaction == null)
            {
                transaction.Dispose();
            }
        }

        public async Task InsertOrUpdatePlayQueueAsync(PlayQueue playQueue, CancellationToken cancellationToken)
        {
            if (playQueue == null)
            {
                return;
            }

            var transaction = _transaction ?? _dbConnection.BeginTransaction();

            try
            {
                var commandDefinition = new CommandDefinition(GetScript("PlayQueue_Upsert"), new { playQueue.Id, UserId = playQueue.User.Id, playQueue.ClientName, playQueue.CurrentTrackId, playQueue.Position }, transaction, cancellationToken: cancellationToken);

                await _dbConnection.ExecuteAsync(commandDefinition).ConfigureAwait(false);

                if (_transaction == null)
                {
                    transaction.Commit();
                }
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }

            if (_transaction == null)
            {
                transaction.Dispose();
            }
        }

        public async Task InsertOrUpdatePlayQueueTrackAsync(Guid playQueueId, Guid trackId, int position, CancellationToken cancellationToken)
        {
            var transaction = _transaction ?? _dbConnection.BeginTransaction();

            try
            {
                var commandDefinition = new CommandDefinition(GetScript("PlayQueue_Track_Insert"), new { PlayQueueId = playQueueId, TrackId = trackId, Position = position }, transaction, cancellationToken: cancellationToken);

                await _dbConnection.ExecuteAsync(commandDefinition).ConfigureAwait(false);

                if (_transaction == null)
                {
                    transaction.Commit();
                }
            }
            catch
            {
                transaction.Rollback();
                throw;
            }

            if (_transaction == null)
            {
                transaction.Dispose();
            }
        }

        public async Task InsertOrUpdateTrackAsync(Track track, CancellationToken cancellationToken)
        {
            if (track == null)
            {
                return;
            }

            var transaction = _transaction ?? _dbConnection.BeginTransaction();

            try
            {
                var trackCommand = new CommandDefinition(GetScript("Track_Upsert"), new
                {
                    track.Id,
                    track.CollectionId,
                    track.Path,
                    track.Name,
                    track.Bitrate,
                    track.Channels,
                    track.Comment,
                    track.ContentType,
                    Disc = track.DiscNumber,
                    Duration = track.Duration.TotalMilliseconds,
                    track.Number,
                    track.ReleaseDate,
                    track.SampleRate,
                }, transaction, cancellationToken: cancellationToken);

                await _dbConnection.ExecuteAsync(trackCommand).ConfigureAwait(false);

                var fileInfoCommand = new CommandDefinition(GetScript("FileInfo_Upsert"), new
                {
                    track.Id,
                    DateCreated = track.DateFileCreated,
                    DateModified = track.DateFileModified,
                    track.Size,
                    track.Visible
                }, transaction, cancellationToken: cancellationToken);

                await _dbConnection.ExecuteAsync(fileInfoCommand).ConfigureAwait(false);

                if (track.AlbumGain.HasValue || track.AlbumPeak.HasValue || track.TrackGain.HasValue || track.TrackPeak.HasValue)
                {
                    var replayGainCommand = new CommandDefinition(GetScript("ReplayGain_Upsert"), new
                    {
                        track.Id,
                        AlbumGain = track.AlbumGain.GetValueOrDefault(),
                        AlbumPeak = track.AlbumPeak.GetValueOrDefault(),
                        TrackGain = track.TrackGain.GetValueOrDefault(),
                        TrackPeak = track.TrackPeak.GetValueOrDefault()
                    }, transaction, cancellationToken: cancellationToken);

                    await _dbConnection.ExecuteAsync(replayGainCommand).ConfigureAwait(false);
                }
                else
                {
                    var replayGainDelete = new CommandDefinition(GetScript("ReplayGain_Delete"), new { TrackId = track.Id }, transaction, cancellationToken: cancellationToken);

                    await _dbConnection.ExecuteAsync(replayGainDelete).ConfigureAwait(false);
                }

                if (track.CoverArt != null)
                {
                    var coverArtDeleteCommmand = new CommandDefinition(GetScript("CoverArt_Delete"), new { TrackId = track.Id }, transaction, cancellationToken: cancellationToken);

                    await _dbConnection.ExecuteAsync(coverArtDeleteCommmand).ConfigureAwait(false);

                    foreach (var coverArt in track.CoverArt)
                    {
                        var coverArtCommand = new CommandDefinition(GetScript("CoverArt_Insert"), new { TrackId = track.Id, CoverArtTypeId = (int)CoverArtType.Front, coverArt.MimeType, coverArt.Size }, transaction, cancellationToken: cancellationToken);

                        await _dbConnection.ExecuteAsync(coverArtCommand).ConfigureAwait(false);
                    }
                }
                else
                {
                    var coverArtDeleteCommmand = new CommandDefinition(GetScript("CoverArt_Delete"), new { TrackId = track.Id }, transaction, cancellationToken: cancellationToken);

                    await _dbConnection.ExecuteAsync(coverArtDeleteCommmand).ConfigureAwait(false);
                }

                var artistToTrackDeleteCommand = new CommandDefinition(GetScript("ArtistToTrack_Delete"), new { TrackId = track.Id }, transaction, cancellationToken: cancellationToken);

                await _dbConnection.ExecuteAsync(artistToTrackDeleteCommand).ConfigureAwait(false);

                foreach (var artist in track.Artists)
                {
                    var artistToTrackCommand = new CommandDefinition(GetScript("ArtistToTrack_Insert"), new { ArtistId = artist.Media.Id, TrackId = track.Id }, transaction, cancellationToken: cancellationToken);

                    await _dbConnection.ExecuteAsync(artistToTrackCommand).ConfigureAwait(false);
                }

                var genreToTrackDeleteCommand = new CommandDefinition(GetScript("GenreToTrack_Delete"), new { TrackId = track.Id }, transaction, cancellationToken: cancellationToken);

                await _dbConnection.ExecuteAsync(genreToTrackDeleteCommand).ConfigureAwait(false);

                foreach (var genre in track.Genres)
                {
                    var genreToTrackCommand = new CommandDefinition(GetScript("GenreToTrack_Insert"), new { GenreId = genre.Id, TrackId = track.Id }, transaction, cancellationToken: cancellationToken);

                    await _dbConnection.ExecuteAsync(genreToTrackCommand).ConfigureAwait(false);
                }

                var trackToAlbumDeleteCommand = new CommandDefinition(GetScript("TrackToAlbum_Delete"), new { TrackId = track.Id }, transaction, cancellationToken: cancellationToken);

                await _dbConnection.ExecuteAsync(trackToAlbumDeleteCommand).ConfigureAwait(false);

                var trackToAlbumCommand = new CommandDefinition(GetScript("TrackToAlbum_Insert"), new { TrackId = track.Id, track.AlbumId }, transaction, cancellationToken: cancellationToken);

                await _dbConnection.ExecuteAsync(trackToAlbumCommand).ConfigureAwait(false);

                if (_transaction == null)
                {
                    transaction.Commit();
                }
            }
            catch
            {
                transaction.Rollback();
                throw;
            }

            if (_transaction == null)
            {
                transaction.Dispose();
            }
        }

        public async Task InsertOrUpdateUserAsync(User user, CancellationToken cancellationToken)
        {
            if (user == null)
            {
                return;
            }

            var transaction = _transaction ?? _dbConnection.BeginTransaction();

            try
            {
                var userCommand = new CommandDefinition(GetScript("User_Upsert"), new { user.Id, user.Name, user.Password, user.EmailAddress }, transaction, cancellationToken: cancellationToken);

                await _dbConnection.ExecuteAsync(userCommand).ConfigureAwait(false);

                var statusCommand = new CommandDefinition(GetScript("Status_Upsert"), new { user.Id, user.Enabled }, transaction, cancellationToken: cancellationToken);

                await _dbConnection.ExecuteAsync(statusCommand).ConfigureAwait(false);

                var roleDeleteCommand = new CommandDefinition(GetScript("UserToRole_Delete"), new { UserId = user.Id }, transaction, cancellationToken: cancellationToken);

                await _dbConnection.ExecuteAsync(roleDeleteCommand).ConfigureAwait(false);

                if (user.Roles?.Any() == true)
                {
                    foreach (var role in user.Roles)
                    {
                        var roleCommand = new CommandDefinition(GetScript("UserToRole_Insert"), new { UserId = user.Id, RoleId = (int)role }, transaction, cancellationToken: cancellationToken);
                        await _dbConnection.ExecuteAsync(roleCommand).ConfigureAwait(false);
                    }
                }

                if (_transaction == null)
                {
                    transaction.Commit();
                }
            }
            catch
            {
                transaction.Rollback();
                throw;
            }

            if (_transaction == null)
            {
                transaction.Dispose();
            }
        }

        public async Task InsertPlaybackAsync(Playback playback, CancellationToken cancellationToken)
        {
            if (playback == null)
            {
                return;
            }

            var transaction = _transaction ?? _dbConnection.BeginTransaction();

            try
            {
                var commandDefinition = new CommandDefinition(GetScript("Playback_Insert"), new { playback.Address, playback.ClientId, Timestamp = playback.PlaybackDateTime, playback.TrackId, playback.UserId }, transaction, cancellationToken: cancellationToken);

                await _dbConnection.ExecuteAsync(commandDefinition).ConfigureAwait(false);

                if (_transaction == null)
                {
                    transaction.Commit();
                }
            }
            catch
            {
                transaction.Rollback();
                throw;
            }

            if (_transaction == null)
            {
                transaction.Dispose();
            }
        }

        public Task RemoveCollectionAsync(Collection collection, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task RemoveUserAsync(User user, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<MediaBundle<T>>> SearchAsync<T>(Guid userId, string queryString, int size, int offset, Guid? collectionId, bool populate, CancellationToken cancellationToken) where T : MediaBase, ISearchable, ICollectionIdentifier
        {
            var genericType = typeof(T);
            var builder = new SqlBuilder();
            Dapper.SqlBuilder.Template query;

            queryString = queryString.Replace('*', '%');
            queryString = $"%{queryString}%";

            if (genericType == typeof(Artist))
            {
                query = builder.AddTemplate(GetScript("Artist_Select"), new { UserId = userId });
                builder.Where(@"a.Id NOT IN (
                  SELECT ar.Id FROM [Artist] ar
                  WHERE
                  NOT EXISTS (SELECT NULL FROM [ArtistToTrack] att WHERE att.ArtistId = ar.Id)
                  AND
                  NOT EXISTS (SELECT NULL FROM [ArtistToAlbum] ata WHERE ata.ArtistId = ar.Id)

                  UNION

                  SELECT ar.Id FROM [Artist] ar
                  JOIN [ArtistToAlbum] ata ON ata.ArtistId = ar.Id
                  WHERE
                  NOT EXISTS (SELECT NULL FROM [TrackToAlbum] tta WHERE tta.AlbumId = ata.AlbumId)
                ) ");

                if (collectionId.HasValue)
                {
                    builder.Where("a.CollectionId = @CollectionId", new { CollectionId = collectionId });
                }

                if (!string.IsNullOrWhiteSpace(queryString))
                {
                    builder.Where("a.Name LIKE @Query", new { Query = queryString });
                }

                builder.OrderBy("a.Name");
            }
            else if (genericType == typeof(Album))
            {
                query = builder.AddTemplate(GetScript("Album_Select"), new { UserId = userId });

                builder.AddClause("firstjoin", "LEFT JOIN [TrackToAlbum] tta ON tta.AlbumId = a.Id");
                builder.Where("tta.AlbumId IS NOT NULL");

                if (collectionId.HasValue)
                {
                    builder.Where("a.CollectionId = @CollectionId", new { CollectionId = collectionId });
                }

                if (!string.IsNullOrWhiteSpace(queryString))
                {
                    builder.Where("a.Name LIKE @Query", new { Query = queryString });
                }

                builder.OrderBy("a.Name");
            }
            else if (genericType == typeof(Track))
            {
                query = builder.AddTemplate(GetScript("Track_Select"), new { UserId = userId });

                if (collectionId.HasValue)
                {
                    builder.Where("t.CollectionId = @CollectionId", new { CollectionId = collectionId });
                }

                if (!string.IsNullOrWhiteSpace(queryString))
                {
                    builder.Where("t.Name LIKE @Query", new { Query = queryString });
                }

                builder.OrderBy("t.Name");
            }
            else
            {
                return null;
            }

            builder.AddClause("limit", "LIMIT @Size OFFSET @Offset", new { Size = size, Offset = offset });

            var commandDefinition = new CommandDefinition(query.RawSql, transaction: _transaction, parameters: query.Parameters, cancellationToken: cancellationToken);

            var result = await _dbConnection.QueryAsync(commandDefinition).ConfigureAwait(false);

            if (result == null)
            {
                return null;
            }

            return await GetMediaBundleAsync<T>(result, userId, populate, cancellationToken).ConfigureAwait(false);
        }

        public async Task SetDispositionAsync(Disposition disposition, CancellationToken cancellationToken)
        {
            await InsertOrUpdateDispositionAsync(disposition, cancellationToken).ConfigureAwait(false);
        }

        public async Task UpdatePlaylistAsync(Playlist playlist, CancellationToken cancellationToken)
        {
            if (_transaction == null)
            {
                BeginTransaction(cancellationToken);
            }

            try
            {
                await InsertOrUpdatePlaylistAsync(playlist, cancellationToken).ConfigureAwait(false);
                await DeletePlaylistTracksAsync(playlist.Id, cancellationToken).ConfigureAwait(false);

                var position = 0;

                foreach (var track in playlist.Tracks)
                {
                    await InsertOrUpdatePlaylistTrackAsync(playlist.Id, track.Media.Id, position, cancellationToken).ConfigureAwait(false);
                    position++;
                }

                EndTransaction(true, cancellationToken);
            }
            catch
            {
                EndTransaction(false, cancellationToken);
            }
        }

        public async Task UpdatePlayQueueAsync(PlayQueue playQueue, CancellationToken cancellationToken)
        {
            if (_transaction == null)
            {
                BeginTransaction(cancellationToken);
            }

            try
            {
                await InsertOrUpdatePlayQueueAsync(playQueue, cancellationToken).ConfigureAwait(false);
                await DeletePlayQueueTracksAsync(playQueue.Id, cancellationToken).ConfigureAwait(false);

                var position = 0;

                foreach (var track in playQueue.Tracks)
                {
                    await InsertOrUpdatePlayQueueTrackAsync(playQueue.Id, track.Media.Id, position, cancellationToken).ConfigureAwait(false);
                    position++;
                }

                EndTransaction(true, cancellationToken);
            }
            catch
            {
                EndTransaction(false, cancellationToken);
            }
        }

        private static string GetIds<T>(IEnumerable<T> list) where T : ModelBase
        {
            return string.Join(":", list.Select(l => l.Id).OrderBy(l => l));
        }

        private async Task CreateSchemaAsync()
        {
            var transaction = _dbConnection.BeginTransaction();

            try
            {
                var commandDefinition = new CommandDefinition("SELECT name FROM sqlite_master WHERE type='table' AND name='Schema';", null, transaction);

                var result = await _dbConnection.ExecuteScalarAsync<string>(commandDefinition).ConfigureAwait(false);

                if (result != "Schema")
                {
                    var schemaCommandDefinition = new CommandDefinition(GetScript("Schema"), null, transaction);

                    await _dbConnection.ExecuteAsync(schemaCommandDefinition).ConfigureAwait(false);

                    transaction.Commit();
                }
                else
                {
                    transaction.Rollback();
                }
            }
            catch
            {
                transaction.Rollback();
            }

            transaction = _dbConnection.BeginTransaction();

            try
            {
                var commandDefinition = new CommandDefinition(GetScript("SchemaPopulate"), new { Password = "Admin".EncryptString(Constants.ResonanceKey) }, transaction);

                await _dbConnection.ExecuteAsync(commandDefinition).ConfigureAwait(false);

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
            }
        }

        private IDbConnection GetConnection()
        {
            var builder = new SqliteConnectionStringBuilder
            {
                DataSource = Path.Combine(_path, _database)
            };

            return new SqliteConnection(builder.ConnectionString);
        }

        private async Task<List<MediaBundle<T>>> GetMediaBundleAsync<T>(IEnumerable<dynamic> results, Guid userId, bool populate, CancellationToken cancellationToken) where T : MediaBase, ISearchable, ICollectionIdentifier
        {
            var genericType = typeof(T);

            var mediaBundles = new List<MediaBundle<T>>();

            if (genericType == typeof(Album))
            {
                foreach (var album in results)
                {
                    var albumMediaBundle = MediaBundle<Album>.FromDynamic(album, userId);

                    if (albumMediaBundle == null)
                    {
                        continue;
                    }

                    if (populate)
                    {
                        await PopulateAlbumAsync(userId, albumMediaBundle, cancellationToken).ConfigureAwait(false);
                    }

                    mediaBundles.Add(albumMediaBundle);
                }
            }
            else if (genericType == typeof(Artist))
            {
                foreach (var artist in results)
                {
                    var artistMediaBundle = MediaBundle<Artist>.FromDynamic(artist, userId);

                    if (artistMediaBundle != null)
                    {
                        mediaBundles.Add(artistMediaBundle);
                    }
                }
            }
            else if (genericType == typeof(Track))
            {
                foreach (var track in results)
                {
                    var trackMediaBundle = MediaBundle<Track>.FromDynamic(track, userId);

                    if (trackMediaBundle == null)
                    {
                        continue;
                    }

                    if (populate)
                    {
                        await PopulateTrackAsync(userId, trackMediaBundle, cancellationToken).ConfigureAwait(false);
                    }

                    mediaBundles.Add(trackMediaBundle);
                }
            }

            return mediaBundles;
        }

        private string GetScript(string scriptName)
        {
            return _scriptCache.GetOrAdd(scriptName, name =>
            {
                var resourceStream = _assembly.GetManifestResourceStream($"{_assemblyName}.Scripts.{name}.sql");

                if (resourceStream == null)
                {
                    return null;
                }

                using (var reader = new StreamReader(resourceStream, Encoding.UTF8))
                {
                    return reader.ReadToEnd();
                }
            });
        }

        private async Task PopulateAlbumAsync(Guid userId, MediaBundle<Album> album, CancellationToken cancellationToken)
        {
            if (album.Media.Artists == null)
            {
                var albumArtists = await GetArtistsByAlbumAsync(userId, album.Media.Id, cancellationToken).ConfigureAwait(false);
                album.Media.Artists = new HashSet<MediaBundle<Artist>>(albumArtists);
            }

            if (album.Media.Tracks == null)
            {
                var albumTracks = await GetTracksByAlbumAsync(userId, album.Media.Id, true, cancellationToken).ConfigureAwait(false);

                foreach (var track in albumTracks.OrderBy(t => t.Media.DiscNumber).ThenBy(t => t.Media.Number))
                {
                    await PopulateTrackAsync(userId, track, cancellationToken).ConfigureAwait(false);
                    album.Media.AddTrack(track);
                }
            }
        }

        private async Task PopulateTrackAsync(Guid userId, MediaBundle<Track> track, CancellationToken cancellationToken)
        {
            if (track.Media.Artists == null)
            {
                var trackArtists = await GetArtistsByTrackAsync(userId, track.Media.Id, cancellationToken).ConfigureAwait(false);
                track.Media.Artists = new HashSet<MediaBundle<Artist>>(trackArtists);
            }

            if (track.Media.Genres == null)
            {
                var trackGenres = await GetGenresByTrackAsync(track.Media.Id, cancellationToken).ConfigureAwait(false);
                track.Media.Genres = new HashSet<Genre>(trackGenres);
            }
        }
    }
}