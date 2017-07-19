using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Resonance.Data.Models;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Linq;
using System.Linq;
using Resonance.Common;

namespace Resonance.Data.Storage.DocumentDB
{
    public class MetadataRepository : IMetadataRepository
    {
        private readonly DocumentClient _client;
        private readonly string _port;
        private readonly string _server;
        private readonly string _path;

        private readonly FeedOptions _defaultFeedOptions = new FeedOptions { MaxItemCount = -1, EnableCrossPartitionQuery = true };

        public MetadataRepository(string path, string server, string port, string key)
        {
            _path = path;
            _server = server;
            _port = port;

            _client = new DocumentClient(new Uri(Uri), key, new ConnectionPolicy { EnableEndpointDiscovery = false });

            CreateSchemaAsync().Wait();
        }

        private string Uri => string.Format("https://{0}:{1}", _server, _port);

        public async Task AddChatAsync(Chat chat, CancellationToken cancellationToken)
        {
            await _client.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri(DatabaseConstants.DatabaseId, DatabaseConstants.Chat), chat);
        }

        public async Task AddCollectionAsync(Collection collection, CancellationToken cancellationToken)
        {
            await _client.UpsertDocumentAsync(UriFactory.CreateDocumentCollectionUri(DatabaseConstants.DatabaseId, DatabaseConstants.Collection), collection);
        }

        public async Task AddPlaybackAsync(Playback playback, CancellationToken cancellationToken)
        {
            await _client.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri(DatabaseConstants.DatabaseId, DatabaseConstants.Playback), playback);
        }

        public async Task AddPlaylistAsync(Playlist playlist, CancellationToken cancellationToken)
        {
            await _client.UpsertDocumentAsync(UriFactory.CreateDocumentCollectionUri(DatabaseConstants.DatabaseId, DatabaseConstants.Playlist), playlist);
        }

        public async Task AddUserAsync(Models.User user, CancellationToken cancellationToken)
        {
            await _client.UpsertDocumentAsync(UriFactory.CreateDocumentCollectionUri(DatabaseConstants.DatabaseId, DatabaseConstants.User), user);
        }

        public void BeginTransaction(CancellationToken cancellationToken)
        {
            
        }

        public Task ClearCollectionAsync<T>(Guid? collectionId, CancellationToken cancellationToken) where T : ModelBase, ICollectionIdentifier
        {
            throw new NotImplementedException();
        }

        public async Task DeleteMarkerAsync(Guid userId, Guid trackId, CancellationToken cancellationToken)
        {
            var query = _client.CreateDocumentQuery<Marker>(UriFactory.CreateDocumentCollectionUri(DatabaseConstants.DatabaseId, DatabaseConstants.Marker), _defaultFeedOptions)
            .Where(s => s.User.Id == userId && s.TrackId == trackId)
            .AsDocumentQuery();
            
            while (query.HasMoreResults)
            {
                foreach (var marker in await query.ExecuteNextAsync<Marker>(cancellationToken))
                {
                    await _client.DeleteDocumentAsync("");
                }
            }
        }

        public Task DeletePlaylistAsync(Guid userId, Guid id, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task DeletePlaylistTracksAsync(Guid id, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task DeletePlayQueueAsync(Guid userId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task DeletePlayQueueTracksAsync(Guid playQueueId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task DeleteTrackReferencesAsync(Track track, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task DeleteUserAsync(Guid userId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public void EndTransaction(bool commit, CancellationToken cancellationToken)
        {
            
        }

        public async Task<MediaBundle<Album>> GetAlbumAsync(Guid userId, Guid id, bool populate, CancellationToken cancellationToken)
        {
            var album = _client.CreateDocumentQuery<Album>(UriFactory.CreateDocumentCollectionUri(DatabaseConstants.DatabaseId, DatabaseConstants.Album), _defaultFeedOptions)
            .FirstOrDefault(s => s.Id == id);

            var mediaBundle = new MediaBundle<Album>()
            {
                Media = album,
                Dispositions = new List<Disposition> { await GetDispositionAsync(userId, id, cancellationToken) }
            };

            if (populate)
            {
                await PopulateAlbumAsync(userId, mediaBundle, cancellationToken).ConfigureAwait(false);
            }

            return mediaBundle;
        }

        public Task<MediaBundle<Album>> GetAlbumAsync(Guid userId, HashSet<Artist> artists, string name, Guid? collectionId, bool populate, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<MediaBundle<Album>>> GetAlbumsAsync(Guid userId, Guid? collectionId, bool populate, CancellationToken cancellationToken)
        {
            var query = _client.CreateDocumentQuery<Album>(UriFactory.CreateDocumentCollectionUri(DatabaseConstants.DatabaseId, DatabaseConstants.Album), _defaultFeedOptions).AsQueryable();

            if (collectionId.HasValue)
            {
                query = query.Where(s => s.CollectionId == collectionId.Value);
            }

            var documentQuery = query.AsDocumentQuery();

            var mediaBundles = new List<MediaBundle<Album>>();

            while (documentQuery.HasMoreResults)
            {

                foreach (var album in await documentQuery.ExecuteNextAsync<Album>(cancellationToken).ConfigureAwait(false))
                {
                    var mediaBundle = new MediaBundle<Album>()
                    {
                        Media = album,
                        Dispositions = new List<Disposition> { await GetDispositionAsync(userId, album.Id, cancellationToken) }
                    };

                    if (populate)
                    {
                        await PopulateAlbumAsync(userId, mediaBundle, cancellationToken).ConfigureAwait(false);
                    }

                    mediaBundles.Add(mediaBundle);
                }
            }

            return mediaBundles;
        }

        public async Task<IEnumerable<MediaBundle<Album>>> GetAlbumsByArtistAsync(Guid userId, Guid artistId, bool populate, CancellationToken cancellationToken)
        {
            var query = _client.CreateDocumentQuery<Album>(UriFactory.CreateDocumentCollectionUri(DatabaseConstants.DatabaseId, DatabaseConstants.Album), _defaultFeedOptions)
                .Where(al => al.Artists.Select(ar => ar.Media.Id).Contains(artistId) || al.Tracks.Select(t => t.Media).SelectMany(tr => tr.Artists).Select(tar => tar.Media.Id).Contains(artistId));
            
            var documentQuery = query.AsDocumentQuery();

            var mediaBundles = new List<MediaBundle<Album>>();

            while (documentQuery.HasMoreResults)
            {
                foreach (var album in await documentQuery.ExecuteNextAsync<Album>(cancellationToken).ConfigureAwait(false))
                {
                    var mediaBundle = new MediaBundle<Album>()
                    {
                        Media = album,
                        Dispositions = new List<Disposition> { await GetDispositionAsync(userId, album.Id, cancellationToken) }
                    };

                    if (populate)
                    {
                        await PopulateAlbumAsync(userId, mediaBundle, cancellationToken).ConfigureAwait(false);
                    }

                    mediaBundles.Add(mediaBundle);
                }
            }

            return mediaBundles;
        }

        public Task<IEnumerable<MediaBundle<Album>>> GetAlbumsByGenreAsync(Guid userId, Guid genreId, bool populate, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<MediaBundle<Album>>> GetAlphabeticalAlbumsAsync(Guid userId, int size, int offset, string genre, int? fromYear, int? toYear, Guid? collectionId, bool populate, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<MediaBundle<Album>>> GetAlphabeticalByArtistAlbumsAsync(Guid userId, int size, int offset, string genre, int? fromYear, int? toYear, Guid? collectionId, bool populate, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task<MediaBundle<Artist>> GetArtistAsync(Guid userId, Guid id, CancellationToken cancellationToken)
        {
            var artist = _client.CreateDocumentQuery<Artist>(UriFactory.CreateDocumentCollectionUri(DatabaseConstants.DatabaseId, DatabaseConstants.Artist), _defaultFeedOptions)
            .FirstOrDefault(s => s.Id == id);

            var mediaBundle = new MediaBundle<Artist>()
            {
                Media = artist,
                Dispositions = new List<Disposition> { await GetDispositionAsync(userId, id, cancellationToken) }
            };

            return mediaBundle;
        }

        public async Task<MediaBundle<Artist>> GetArtistAsync(Guid userId, string artist, Guid? collectionId, CancellationToken cancellationToken)
        {
            var query = _client.CreateDocumentQuery<Artist>(UriFactory.CreateDocumentCollectionUri(DatabaseConstants.DatabaseId, DatabaseConstants.Artist), _defaultFeedOptions)
                .AsQueryable()
                .Where(ar => ar.Name == artist);
                            
            if (collectionId.HasValue)
            {
                query = query.Where(s => s.CollectionId == collectionId.Value);
            }

            var artistModel = query.FirstOrDefault();

            if (artistModel == null)
            {
                return null;
            }
            
            var mediaBundle = new MediaBundle<Artist>()
            {
                Media = artistModel,
                Dispositions = new List<Disposition> { await GetDispositionAsync(userId, artistModel.Id, cancellationToken) }
            };

            return mediaBundle;
        }

        public Task<IEnumerable<MediaBundle<Artist>>> GetArtistsAsync(Guid userId, Guid? collectionId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<MediaBundle<Artist>>> GetArtistsByAlbumAsync(Guid userId, Guid albumId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<MediaBundle<Artist>>> GetArtistsByTrackAsync(Guid userId, Guid trackId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<double?> GetAverageRatingAsync(Guid mediaId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Chat>> GetChatAsync(DateTime? since, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<Collection>> GetCollectionsAsync(CancellationToken cancellationToken)
        {
            var query = _client.CreateDocumentQuery<Collection>(UriFactory.CreateDocumentCollectionUri(DatabaseConstants.DatabaseId, DatabaseConstants.Collection), _defaultFeedOptions).AsDocumentQuery();

            var results = new List<Collection>();

            while (query.HasMoreResults)
            {
                results.AddRange(await query.ExecuteNextAsync<Collection>(cancellationToken));
            }

            return results;
        }

        public Task<Disposition> GetDispositionAsync(Guid userId, Guid mediaId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Disposition>> GetDispositionsAsync(Guid userId, MediaType mediaType, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<MediaBundle<Album>>> GetFavoritedAlbumsAsync(Guid userId, int size, int offset, string genre, int? fromYear, int? toYear, Guid? collectionId, bool populate, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<MediaBundle<T>>> GetFavoritedAsync<T>(Guid userId, Guid? collectionId, bool populate, CancellationToken cancellationToken) where T : MediaBase, ISearchable, ICollectionIdentifier
        {
            throw new NotImplementedException();
        }

        public Task<Genre> GetGenreAsync(string genre, Guid? collectionId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<Dictionary<string, Tuple<int, int>>> GetGenreCountsAsync(Guid? collectionId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Genre>> GetGenresAsync(Guid? collectionId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Genre>> GetGenresByTrackAsync(Guid trackId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Marker>> GetMarkersAsync(Guid userId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<MediaInfo> GetMediaInfoAsync(Guid mediaId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<MediaType?> GetMediaTypeAsync(Guid mediaId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<MediaBundle<Album>>> GetMostPlayedAlbumsAsync(Guid userId, int size, int offset, string genre, int? fromYear, int? toYear, Guid? collectionId, bool populate, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<MediaBundle<Album>>> GetMostRecentlyPlayedAlbumsAsync(Guid userId, int size, int offset, string genre, int? fromYear, int? toYear, Guid? collectionId, bool populate, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<MediaBundle<Album>>> GetNewestAlbumsAsync(Guid userId, int size, int offset, string genre, int? fromYear, int? toYear, Guid? collectionId, bool populate, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<Playlist> GetPlaylistAsync(Guid userId, Guid id, bool getTracks, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<List<Playlist>> GetPlaylistsAsync(Guid userId, string username, bool getTracks, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<PlayQueue> GetPlayQueueAsync(Guid userId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<MediaBundle<Album>>> GetRandomAlbumsAsync(Guid userId, int size, int offset, string genre, int? fromYear, int? toYear, Guid? collectionId, bool populate, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<MediaBundle<Track>>> GetRecentPlaybackAsync(Guid userId, bool populate, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<Role>> GetRolesForUserAsync(Guid userId, CancellationToken cancellationToken)
        {
            var user = await GetUserAsync(userId, cancellationToken);

            return user.Roles;
        }

        public Task<MediaBundle<Track>> GetTrackAsync(Guid userId, string artist, string track, Guid? collectionId, bool populate, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task<MediaBundle<Track>> GetTrackAsync(Guid userId, string path, Guid? collectionId, bool populate, CancellationToken cancellationToken)
        {
            var query = _client.CreateDocumentQuery<Track>(UriFactory.CreateDocumentCollectionUri(DatabaseConstants.DatabaseId, DatabaseConstants.User), _defaultFeedOptions)
            .Where(s => s.Path == path);

            var results = await query.AsDocumentQuery().ExecuteNextAsync<Track>(cancellationToken);

            var track = results.FirstOrDefault();

            if (track == null)
            {
                return null;
            }

            var mediaBundle = new MediaBundle<Track>()
            {
                Media = track,
                Dispositions = new List<Disposition> { await GetDispositionAsync(userId, track.Id, cancellationToken) }
            };

            if (populate)
            {
                await PopulateTrackAsync(userId, mediaBundle, cancellationToken).ConfigureAwait(false);
            }

            return mediaBundle;
        }

        public Task<MediaBundle<Track>> GetTrackAsync(Guid userId, Guid id, bool populate, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<MediaBundle<Track>>> GetTracksAsync(Guid userId, int size, int offset, string genre, int? fromYear, int? toYear, Guid? collectionId, bool populate, bool randomize, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<MediaBundle<Track>>> GetTracksAsync(Guid userId, Guid? collectionId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<MediaBundle<Track>>> GetTracksByAlbumAsync(Guid userId, Guid albumId, bool populate, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<MediaBundle<Track>>> GetTracksByGenreAsync(Guid userId, Guid genreId, bool populate, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task<Models.User> GetUserAsync(string username, CancellationToken cancellationToken)
        {
            var query = _client.CreateDocumentQuery<Models.User>(UriFactory.CreateDocumentCollectionUri(DatabaseConstants.DatabaseId, DatabaseConstants.User), _defaultFeedOptions)
            .Where(s => s.Name == username);

            var results = await query.AsDocumentQuery().ExecuteNextAsync<Models.User>(cancellationToken);

            return results.FirstOrDefault();
        }

        public async Task<Models.User> GetUserAsync(Guid userId, CancellationToken cancellationToken)
        {
            var query = _client.CreateDocumentQuery<Models.User>(UriFactory.CreateDocumentCollectionUri(DatabaseConstants.DatabaseId, DatabaseConstants.User), _defaultFeedOptions)
            .Where(s => s.Id == userId);

            var results = await query.AsDocumentQuery().ExecuteNextAsync<Models.User>(cancellationToken);

            return results.FirstOrDefault();
        }

        public async Task<IEnumerable<Models.User>> GetUsersAsync(CancellationToken cancellationToken)
        {
            var query = _client.CreateDocumentQuery<Models.User>(UriFactory.CreateDocumentCollectionUri(DatabaseConstants.DatabaseId, DatabaseConstants.User), _defaultFeedOptions).AsDocumentQuery();

            var results = new List<Models.User>();

            while (query.HasMoreResults)
            {
                results.AddRange(await query.ExecuteNextAsync<Models.User>(cancellationToken));
            }

            return results;
        }

        public Task InsertOrUpdateAlbumAsync(Album album, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task InsertOrUpdateArtistAsync(Artist artist, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task InsertOrUpdateCollectionAsync(Collection collection, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task InsertOrUpdateDispositionAsync(Disposition disposition, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task InsertOrUpdateFileInfoAsync(Track track, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task InsertOrUpdateGenreAsync(Genre genre, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task InsertOrUpdateMarkerAsync(Marker marker, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task InsertOrUpdateMediaInfoAsync(MediaInfo mediaInfo, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task InsertOrUpdatePlaylistAsync(Playlist playlist, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task InsertOrUpdatePlaylistTrackAsync(Guid playlistId, Guid trackId, int position, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task InsertOrUpdatePlayQueueAsync(PlayQueue playQueue, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task InsertOrUpdatePlayQueueTrackAsync(Guid playQueueId, Guid trackId, int position, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task InsertOrUpdateTrackAsync(Track track, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task InsertOrUpdateUserAsync(Models.User type, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task InsertPlaybackAsync(Playback playback, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task RemoveCollectionAsync(Collection collection, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task RemoveUserAsync(Models.User user, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<MediaBundle<T>>> SearchAsync<T>(Guid userId, string query, int size, int offset, Guid? collectionId, bool populate, CancellationToken cancellationToken) where T : MediaBase, ISearchable, ICollectionIdentifier
        {
            throw new NotImplementedException();
        }

        public Task SetDispositionAsync(Disposition disposition, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task UpdatePlaylistAsync(Playlist playlist, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task UpdatePlayQueueAsync(PlayQueue playQueue, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        private async Task CreateSchemaAsync()
        {
            var response = await _client.CreateDatabaseIfNotExistsAsync(new Database { Id = DatabaseConstants.DatabaseId });

            foreach (var database in DatabaseConstants.AllDatabases)
            {
                await _client.CreateDocumentCollectionIfNotExistsAsync(response.Resource.SelfLink, new DocumentCollection { Id = database });
            }

            var adminUser = await GetUserAsync("Admin", CancellationToken.None);

            if (adminUser == null)
            {
                await _client.UpsertDocumentAsync(UriFactory.CreateDocumentCollectionUri(DatabaseConstants.DatabaseId, DatabaseConstants.User), new Models.User { Name = "Admin", Password = "Admin".EncryptString(Constants.ResonanceKey), Enabled = true, Roles = new List<Role> { Role.Administrator } });
            }
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
