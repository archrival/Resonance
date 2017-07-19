//using MongoDB.Bson;
//using MongoDB.Driver;
//using MongoDB.Driver.Linq;
//using Resonance.Common;
//using Resonance.Data.Models;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Linq.Expressions;
//using System.Text.RegularExpressions;
//using System.Threading;
//using System.Threading.Tasks;

//namespace Resonance.Data.Storage.MongoDB
//{
//    public class MetadataRepository : IMetadataRepository
//    {
//        public static readonly Dictionary<Type, string> CollectionLookup = new Dictionary<Type, string>
//        {
//            {typeof(Album), DatabaseConstants.Albums },
//            {typeof(Artist), DatabaseConstants.Artists },
//            {typeof(Genre), DatabaseConstants.Genres },
//            {typeof(Track), DatabaseConstants.Tracks },
//            {typeof(User), DatabaseConstants.Configuration },
//            {typeof(Disposition), DatabaseConstants.Disposition },
//            {typeof(Playback), DatabaseConstants.Playback },
//            {typeof(Chat), DatabaseConstants.Chat },
//            {typeof(MediaInfo), DatabaseConstants.MediaInfo }
//        };

//        private readonly MongoClient _client;
//        private readonly string _port;
//        private readonly string _server;

//        public MetadataRepository(string server, string port)
//        {
//            _server = server;
//            _port = port;

//            _client = new MongoClient(Uri);
//        }

//        private string Uri => string.Format("mongodb://{0}:{1}", _server, _port);

//        public Task AddChatAsync(Chat chat, CancellationToken cancellationToken)
//        {
//            throw new NotImplementedException();
//        }

//        public Task AddCollectionAsync(Collection collection, CancellationToken cancellationToken)
//        {
//            throw new NotImplementedException();
//        }

//        public async Task AddPlaybackAsync(Playback playback, CancellationToken cancellationToken)
//        {
//            await InsertPlaybackAsync(playback, cancellationToken);
//        }

//        public Task AddPlaylistAsync(Playlist playlist, CancellationToken cancellationToken)
//        {
//            throw new NotImplementedException();
//        }

//        public async Task AddSettingAsync<T>(T type, CancellationToken cancellationToken) where T : SettingBase
//        {
//            var resultList = await GetSettingsAsync<T>(cancellationToken);

//            if (resultList.Contains(type))
//            {
//                return;
//            }

//            await InsertOrUpdateSettingAsync(type, cancellationToken);
//        }

//        public Task AddUserAsync(User user, CancellationToken cancellationToken)
//        {
//            throw new NotImplementedException();
//        }

//        public void BeginTransactionAsync(CancellationToken cancellationToken)
//        {
//            throw new NotImplementedException();
//        }

//        public async Task ClearCollectionAsync<T>(Guid? collectionId, CancellationToken cancellationToken) where T : ModelBase, ICollectionIdentifier
//        {
//            var db = _client.GetDatabase(DatabaseConstants.Database);

//            var objectType = typeof(T);

//            if (CollectionLookup.ContainsKey(objectType))
//            {
//                var collectionName = CollectionLookup[objectType];

//                await db.DropCollectionAsync(collectionName, cancellationToken);

//                await db.CreateCollectionAsync(collectionName, null, cancellationToken);
//                var collection = db.GetCollection<T>(collectionName);

//                await CreateIndexesAsync(collection, cancellationToken);
//            }
//        }

//        public Task DeletePlaylistAsync(Guid id, CancellationToken cancellationToken)
//        {
//            throw new NotImplementedException();
//        }

//        public Task DeletePlaylistTracksAsync(Guid id, CancellationToken cancellationToken)
//        {
//            throw new NotImplementedException();
//        }

//        public Task DeletePlayQueueAsync(Guid userId, CancellationToken cancellationToken)
//        {
//            throw new NotImplementedException();
//        }

//        public Task DeletePlayQueueTracksAsync(Guid playQueueId, CancellationToken cancellationToken)
//        {
//            throw new NotImplementedException();
//        }

//        public Task DeleteTrackReferencesAsync(Track track, CancellationToken cancellationToken)
//        {
//            throw new NotImplementedException();
//        }

//        public Task DeleteUserAsync(Guid userId, CancellationToken cancellationToken)
//        {
//            throw new NotImplementedException();
//        }

//        public void EndTransactionAsync(bool commit, CancellationToken cancellationToken)
//        {
//            throw new NotImplementedException();
//        }

//        public async Task<MediaBundle<Album>> GetAlbumAsync(Guid userId, Guid id, bool populate, CancellationToken cancellationToken)
//        {
//            var collection = await GetCollectionAsync<Album>(DatabaseConstants.Albums, cancellationToken);

//            var filter = Builders<Album>.Filter.Eq(t => t.Id, id);

//            var result = await collection.FindAsync(filter, null, cancellationToken);

//            var albumTracks = await GetTracksAsync(id, cancellationToken);

//            var album = await result.FirstOrDefaultAsync(cancellationToken);

//            foreach (var track in albumTracks)
//            {
//                album.AddTrack(track);
//            }

//            return album;
//        }

//        public async Task<MediaBundle<Album>> GetAlbumAsync(Guid userId, HashSet<Artist> artists, string name, Guid? collectionId, bool populate, CancellationToken cancellationToken)
//        {
//            var collection = await GetCollectionAsync<Album>(DatabaseConstants.Albums, cancellationToken);

//            var filter = Builders<Album>.Filter.Eq(a => a.Name, name);

//            filter = UpdateCollectionFilter(filter, collectionId);

//            var result = await collection.FindAsync(filter, null, cancellationToken);

//            foreach (var album in await result.ToListAsync(cancellationToken))
//            {
//                if (album.Artists != null)
//                {
//                    if (album.Artists.SequenceEqual(artists))
//                    {
//                        var albumTracks = await GetTracksAsync(album.Id, cancellationToken);

//                        foreach (var track in albumTracks)
//                        {
//                            album.AddTrack(track);
//                        }

//                        return album;
//                    }
//                }
//            }

//            return null;
//        }

//        public async Task<IEnumerable<MediaBundle<Album>>> GetAlbumsAsync(Guid userId, Guid? collectionId, bool populate, CancellationToken cancellationToken)
//        {
//            var collection = await GetCollectionAsync<Album>(DatabaseConstants.Albums, cancellationToken);

//            var filter = Builders<Album>.Filter.Empty;

//            filter = UpdateCollectionFilter(filter, collectionId);

//            var result = await collection.FindAsync(filter, null, cancellationToken);

//            var albums = await result.ToListAsync(cancellationToken);

//            var albumResult = new List<Album>(albums.Count);

//            foreach (var album in albums)
//            {
//                var albumTracks = await GetTracksAsync(album.Id, cancellationToken);

//                foreach (var track in albumTracks)
//                {
//                    album.AddTrack(track);
//                }

//                albumResult.Add(album);
//            }

//            return albumResult;
//        }

//        public async Task<IEnumerable<MediaBundle<Album>>> GetAlbumsByArtistAsync(Guid userId, Guid artistId, bool populate, CancellationToken cancellationToken)
//        {
//            var collection = await GetCollectionAsync<Album>(DatabaseConstants.Albums, cancellationToken);
//            var artist = await GetArtistAsync(artistId, cancellationToken);
//            var tracks = await GetTracksByArtistAsync(artistId, cancellationToken);

//            var artistTrackAlbums = tracks.Select(a => a.Album.Id);

//            var filter = Builders<Album>.Filter.Where(a => a.Artists.Contains(artist) || artistTrackAlbums.Contains(a.Id));

//            filter = UpdateCollectionFilter(filter, collectionId);

//            var result = await collection.FindAsync(filter, null, cancellationToken);

//            var albums = await result.ToListAsync(cancellationToken);

//            var albumResult = new List<Album>(albums.Count);

//            foreach (var album in albums)
//            {
//                var albumTracks = await GetTracksAsync(album.Id, cancellationToken);

//                foreach (var track in albumTracks)
//                {
//                    album.AddTrack(track);
//                }

//                albumResult.Add(album);
//            }

//            return albumResult;
//        }

//        public Task<IEnumerable<MediaBundle<Album>>> GetAlbumsByGenreAsync(Guid userId, Guid genreId, bool populate, CancellationToken cancellationToken)
//        {
//            throw new NotImplementedException();
//        }

//        public async Task<IEnumerable<Album>> GetAlbumsByGenreAsync(Guid userId, int size, int offset, string genre, int? fromYear, int? toYear, Guid? collectionId, CancellationToken cancellationToken)
//        {
//            var collection = await GetCollectionAsync<Album>(DatabaseConstants.Albums, cancellationToken);
//            var predicate = await GetAlbumPredicateAsync(genre, fromYear, toYear, collectionId, cancellationToken);

//            var results = await collection.AsQueryable().Where(predicate).OrderBy(a => a.ReleaseDate).OrderBy(a => a.Name).Skip(offset).Take(size).ToListAsync(cancellationToken);

//            return await PopulateAlbumTracks(results, cancellationToken);
//        }

//        public async Task<IEnumerable<Album>> GetAlbumsByYearAsync(Guid userId, int size, int offset, string genre, int? fromYear, int? toYear, Guid? collectionId, CancellationToken cancellationToken)
//        {
//            var collection = await GetCollectionAsync<Album>(DatabaseConstants.Albums, cancellationToken);
//            var predicate = await GetAlbumPredicateAsync(genre, fromYear, toYear, collectionId, cancellationToken);

//            var results = await collection.AsQueryable().Where(predicate).OrderBy(a => a.ReleaseDate).OrderBy(a => a.Name).Skip(offset).Take(size).ToListAsync(cancellationToken);

//            return await PopulateAlbumTracks(results, cancellationToken);
//        }

//        public Task<IEnumerable<MediaBundle<Album>>> GetAlphabeticalAlbumsAsync(Guid userId, int size, int offset, string genre, int? fromYear, int? toYear,
//                            Guid? collectionId, bool populate, CancellationToken cancellationToken)
//        {
//            throw new NotImplementedException();
//        }

//        public async Task<IEnumerable<Album>> GetAlphabeticalAlbumsAsync(Guid userId, int size, int offset, string genre, int? fromYear, int? toYear, Guid? collectionId, CancellationToken cancellationToken)
//        {
//            var collection = await GetCollectionAsync<Album>(DatabaseConstants.Albums, cancellationToken);
//            var predicate = await GetAlbumPredicateAsync(genre, fromYear, toYear, collectionId, cancellationToken);
//            var results = await collection.AsQueryable().Where(predicate).OrderBy(a => a.ReleaseDate).OrderByDescending(a => a.Name).Skip(offset).Take(size).ToListAsync(cancellationToken);

//            return await PopulateAlbumTracks(results, cancellationToken);
//        }

//        public Task<IEnumerable<MediaBundle<Album>>> GetAlphabeticalByArtistAlbumsAsync(Guid userId, int size, int offset, string genre, int? fromYear, int? toYear,
//                    Guid? collectionId, bool populate, CancellationToken cancellationToken)
//        {
//            throw new NotImplementedException();
//        }

//        public async Task<IEnumerable<Album>> GetAlphabeticalByArtistAlbumsAsync(Guid userId, int size, int offset, string genre, int? fromYear, int? toYear, Guid? collectionId, CancellationToken cancellationToken)
//        {
//            var collection = await GetCollectionAsync<Album>(DatabaseConstants.Albums, cancellationToken);
//            var predicate = await GetAlbumPredicateAsync(genre, fromYear, toYear, collectionId, cancellationToken);
//            var results = await collection.AsQueryable().Where(predicate).OrderBy(a => a.ReleaseDate).OrderByDescending(a => a.Name).Skip(offset).Take(size).ToListAsync(cancellationToken);

//            return await PopulateAlbumTracks(results, cancellationToken);
//        }

//        public async Task<MediaBundle<Artist>> GetArtistAsync(Guid userId, Guid id, CancellationToken cancellationToken)
//        {
//            var collection = await GetCollectionAsync<Artist>(DatabaseConstants.Artists, cancellationToken);

//            var filter = Builders<Artist>.Filter.Eq(a => a.Id, id);

//            var result = await collection.FindAsync(filter, null, cancellationToken);

//            return await result.FirstOrDefaultAsync(cancellationToken);
//        }

//        public async Task<MediaBundle<Artist>> GetArtistAsync(Guid userId, string artist, Guid? collectionId, CancellationToken cancellationToken)
//        {
//            var collection = await GetCollectionAsync<Artist>(DatabaseConstants.Artists, cancellationToken);

//            var filter = Builders<Artist>.Filter.Eq(a => a.Name, artist);

//            filter = UpdateCollectionFilter(filter, collectionId);

//            var result = await collection.FindAsync(filter, null, cancellationToken);

//            return await result.FirstOrDefaultAsync(cancellationToken);
//        }

//        public async Task<IEnumerable<KeyValuePair<Artist, Disposition>>> GetArtistsAsync(Guid userId, Guid? collectionId, CancellationToken cancellationToken)
//        {
//            var collection = await GetCollectionAsync<Artist>(DatabaseConstants.Artists, cancellationToken);
//            var dispositionCollection = await GetCollectionAsync<Disposition>(DatabaseConstants.Disposition, cancellationToken);

//            var query = from a in collection.AsQueryable() select a;

//            if (collectionId.HasValue)
//            {
//                query = query.Where(a => a.CollectionId == collectionId);
//            }

//            var query2 = from a in query
//                         join d in dispositionCollection on
//                         a.Id equals d.MediaId
//                         into joined
//                         select new { Key = a, Value = joined };

//            var test = query2.ToEnumerable(cancellationToken);

//            var results = new List<KeyValuePair<Artist, Disposition>>(test.Count());

//            foreach (var t in test)
//            {
//                var artist = t.Key;
//                var disposition = t.Value.FirstOrDefault(d => d.UserId == userId);

//                results.Add(new KeyValuePair<Artist, Disposition>(artist, disposition));
//            }

//            return results;

//            //var filter = Builders<Artist>.Filter.Empty;

//            //filter = UpdateCollectionFilter(filter, collectionId);

//            //var result = await collection.FindAsync(filter, null, cancellationToken);

//            //return result.ToEnumerable();
//        }

//        public Task<IEnumerable<MediaBundle<Artist>>> GetArtistsByAlbumAsync(Guid userId, Guid albumId, CancellationToken cancellationToken)
//        {
//            throw new NotImplementedException();
//        }

//        public Task<IEnumerable<MediaBundle<Artist>>> GetArtistsByTrackAsync(Guid userId, Guid trackId, CancellationToken cancellationToken)
//        {
//            throw new NotImplementedException();
//        }

//        public async Task<double?> GetAverageRatingAsync(Guid mediaId, CancellationToken cancellationToken)
//        {
//            var collection = await GetCollectionAsync<Disposition>(DatabaseConstants.Disposition, cancellationToken);
//            return await collection.AsQueryable().Where(d => d.MediaId == mediaId).AverageAsync(d => d.UserRating);
//        }

//        public Task<IEnumerable<Chat>> GetChatAsync(DateTime? since, CancellationToken cancellationToken)
//        {
//            throw new NotImplementedException();
//        }

//        public Task<IEnumerable<Collection>> GetCollectionsAsync(CancellationToken cancellationToken)
//        {
//            throw new NotImplementedException();
//        }

//        public async Task<Disposition> GetDispositionAsync(Guid userId, Guid mediaId, CancellationToken cancellationToken)
//        {
//            var collection = await GetCollectionAsync<Disposition>(DatabaseConstants.Disposition, cancellationToken);
//            return await collection.AsQueryable().Where(d => d.UserId == userId && d.MediaId == mediaId).FirstOrDefaultAsync(cancellationToken);
//        }

//        public async Task<IEnumerable<Disposition>> GetDispositionsAsync(Guid userId, DispositionType dispositionType, CancellationToken cancellationToken)
//        {
//            var collection = await GetCollectionAsync<Disposition>(DatabaseConstants.Disposition, cancellationToken);
//            return await collection.AsQueryable().Where(d => d.UserId == userId && d.DispositionType == dispositionType).ToListAsync(cancellationToken);
//        }

//        public Task<DispositionType?> GetDispositionTypeAsync(Guid mediaId, CancellationToken cancellationToken)
//        {
//            throw new NotImplementedException();
//        }

//        public Task<IEnumerable<MediaBundle<T>>> GetFavorited<T>(Guid userId, Guid? collectionId, bool populate, CancellationToken cancellationToken) where T : MediaBase, ISearchable, ICollectionIdentifier
//        {
//            throw new NotImplementedException();
//        }

//        public Task<IEnumerable<MediaBundle<Album>>> GetFavoritedAlbumsAsync(Guid userId, int size, int offset, string genre, int? fromYear, int? toYear,
//            Guid? collectionId, bool populate, CancellationToken cancellationToken)
//        {
//            throw new NotImplementedException();
//        }

//        public async Task<IEnumerable<Album>> GetFavoritedAlbumsAsync(Guid userId, int size, int offset, string genre, int? fromYear, int? toYear, Guid? collectionId, CancellationToken cancellationToken)
//        {
//            var collection = await GetCollectionAsync<Album>(DatabaseConstants.Albums, cancellationToken);
//            var predicate = await GetAlbumPredicateAsync(genre, fromYear, toYear, collectionId, cancellationToken);

//            var dispositionCollection = await GetCollectionAsync<Disposition>(DatabaseConstants.Disposition, cancellationToken);
//            var dispositions = dispositionCollection.AsQueryable().Where(d => d.UserId == userId && d.DispositionType == DispositionType.Album && d.Favorited.HasValue);

//            predicate.And(a => dispositions.Select(d => d.MediaId).Contains(a.Id));

//            var results = await collection.AsQueryable().Where(predicate).OrderByDescending(a => a.Name).Skip(offset).Take(size).ToListAsync(cancellationToken);

//            return await PopulateAlbumTracks(results, cancellationToken);
//        }

//        public async Task<Genre> GetGenreAsync(string genre, Guid? collectionId, CancellationToken cancellationToken)
//        {
//            var collection = await GetCollectionAsync<Genre>(DatabaseConstants.Genres, cancellationToken);

//            var filter = Builders<Genre>.Filter.Eq(g => g.Name, genre);

//            filter = UpdateCollectionFilter(filter, collectionId);

//            var result = await collection.FindAsync(filter, null, cancellationToken);

//            return await result.FirstOrDefaultAsync(cancellationToken);
//        }

//        public Task<Dictionary<string, Tuple<int, int>>> GetGenreCountsAsync(Guid? collectionId, CancellationToken cancellationToken)
//        {
//            throw new NotImplementedException();
//        }

//        public async Task<IEnumerable<Genre>> GetGenresAsync(Guid? collectionId, CancellationToken cancellationToken)
//        {
//            var collection = await GetCollectionAsync<Genre>(DatabaseConstants.Genres, cancellationToken);

//            var filter = Builders<Genre>.Filter.Empty;

//            filter = UpdateCollectionFilter(filter, collectionId);

//            var result = await collection.FindAsync(filter, null, cancellationToken);

//            return result.ToEnumerable();
//        }

//        public Task<IEnumerable<Genre>> GetGenresByTrackAsync(Guid trackId, CancellationToken cancellationToken)
//        {
//            throw new NotImplementedException();
//        }

//        public Task<MediaInfo> GetMediaInfoAsync(Guid mediaId, CancellationToken cancellationToken)
//        {
//            throw new NotImplementedException();
//        }

//        public Task<IEnumerable<MediaBundle<Album>>> GetMostPlayedAlbumsAsync(Guid userId, int size, int offset, string genre, int? fromYear, int? toYear,
//            Guid? collectionId, bool populate, CancellationToken cancellationToken)
//        {
//            throw new NotImplementedException();
//        }

//        public Task<IEnumerable<MediaBundle<Album>>> GetMostRecentlyPlayedAlbumsAsync(Guid userId, int size, int offset, string genre, int? fromYear, int? toYear,
//            Guid? collectionId, bool populate, CancellationToken cancellationToken)
//        {
//            throw new NotImplementedException();
//        }

//        public Task<IEnumerable<MediaBundle<Album>>> GetNewestAlbumsAsync(Guid userId, int size, int offset, string genre, int? fromYear, int? toYear,
//            Guid? collectionId, bool populate, CancellationToken cancellationToken)
//        {
//            throw new NotImplementedException();
//        }

//        public async Task<IEnumerable<Album>> GetNewestAlbumsAsync(Guid userId, int size, int offset, string genre, int? fromYear, int? toYear, Guid? collectionId, CancellationToken cancellationToken)
//        {
//            var collection = await GetCollectionAsync<Album>(DatabaseConstants.Albums, cancellationToken);
//            var predicate = await GetAlbumPredicateAsync(genre, fromYear, toYear, collectionId, cancellationToken);
//            var results = await collection.AsQueryable().Where(predicate).OrderByDescending(a => a.DateAdded).Skip(offset).Take(size).ToListAsync(cancellationToken);

//            return await PopulateAlbumTracks(results, cancellationToken);
//        }

//        public Task<Playlist> GetPlaylistAsync(Guid userId, Guid id, bool getTracks, CancellationToken cancellationToken)
//        {
//            throw new NotImplementedException();
//        }

//        public Task<List<Playlist>> GetPlaylistsAsync(Guid userId, string username, bool getTracks, CancellationToken cancellationToken)
//        {
//            throw new NotImplementedException();
//        }

//        public Task<PlayQueue> GetPlayQueueAsync(Guid userId, CancellationToken cancellationToken)
//        {
//            throw new NotImplementedException();
//        }

//        public Task<IEnumerable<MediaBundle<Album>>> GetRandomAlbumsAsync(Guid userId, int size, int offset, string genre, int? fromYear, int? toYear,
//            Guid? collectionId, bool populate, CancellationToken cancellationToken)
//        {
//            throw new NotImplementedException();
//        }

//        public async Task<IEnumerable<Album>> GetRandomAlbumsAsync(Guid userId, int size, int offset, string genre, int? fromYear, int? toYear, Guid? collectionId, CancellationToken cancellationToken)
//        {
//            var collection = await GetCollectionAsync<Album>(DatabaseConstants.Albums, cancellationToken);

//            var predicate = PredicateBuilder.True<Album>();

//            var filter = Builders<Album>.Filter.Empty;

//            if (!string.IsNullOrWhiteSpace(genre))
//            {
//                var filterGenre = await GetGenreAsync(genre, collectionId, cancellationToken);

//                if (filterGenre != null)
//                {
//                    filter = filter & Builders<Album>.Filter.Where(a => a.Genres.Contains(filterGenre));
//                    predicate = predicate.And(a => a.Genres.Contains(filterGenre));
//                }
//            }

//            if (fromYear.HasValue)
//            {
//                filter = filter & Builders<Album>.Filter.Where(a => a.ReleaseDate >= fromYear.Value);
//                predicate = predicate.And(a => a.ReleaseDate >= fromYear.Value);
//            }

//            if (toYear.HasValue)
//            {
//                filter = filter & Builders<Album>.Filter.Where(a => a.ReleaseDate <= toYear.Value);
//                predicate = predicate.And(a => a.ReleaseDate <= toYear.Value);
//            }

//            predicate = UpdatePredicate(predicate, collectionId);

//            var filteredResults = collection.AsQueryable().Where(predicate).Select(a => new { id = Guid.NewGuid(), a }).Select(a => (Album)a.a);
//            var results = await filteredResults.ToListAsync(cancellationToken);

//            var albums = new List<Album>();

//            foreach (var r in results)
//            {
//                var albumTracks = await GetTracksAsync(r.Id, cancellationToken);

//                foreach (var track in albumTracks)
//                {
//                    r.AddTrack(track);
//                }

//                albums.Add(r);
//            }

//            return albums;
//        }

//        public async Task<IEnumerable<Track>> GetRandomTracksAsync(int size, int offset, string genre, int? fromYear, int? toYear, Guid? collectionId, CancellationToken cancellationToken)
//        {
//            var collection = await GetCollectionAsync<Track>(DatabaseConstants.Tracks, cancellationToken);

//            var filter = Builders<Track>.Filter.Empty;

//            if (!string.IsNullOrWhiteSpace(genre))
//            {
//                var filterGenre = await GetGenreAsync(genre, collectionId, cancellationToken);

//                if (filterGenre != null)
//                {
//                    filter = filter & Builders<Track>.Filter.Where(t => t.Genres.Contains(filterGenre));
//                }
//            }

//            if (fromYear > 0)
//            {
//                filter = filter & Builders<Track>.Filter.Where(t => t.ReleaseDate >= fromYear);
//            }

//            if (toYear > 0)
//            {
//                filter = filter & Builders<Track>.Filter.Where(t => t.ReleaseDate <= toYear);
//            }

//            filter = UpdateCollectionFilter(filter, collectionId);

//            var findOptions = new FindOptions<Track, Track>
//            {
//                Limit = size,
//                Skip = offset
//            };

//            var result = await collection.FindAsync(filter, findOptions, cancellationToken);

//            var list = await result.ToListAsync(cancellationToken);

//            var tracks = list.OrderBy(t => Guid.NewGuid());

//            return tracks;
//        }

//        public Task<IEnumerable<MediaBundle<Track>>> GetRecentPlaybackAsync(Guid userId, bool populate, CancellationToken cancellationToken)
//        {
//            throw new NotImplementedException();
//        }

//        public async Task<IEnumerable<MediaBundle<Track>>> GetRecentPlaybackAsync(CancellationToken cancellationToken)
//        {
//            var playbackCollection = await GetCollectionAsync<Playback>(DatabaseConstants.Playback, cancellationToken);
//            var dispositionCollection = await GetCollectionAsync<Disposition>(DatabaseConstants.Disposition, cancellationToken);
//            var mediaCollection = await GetCollectionAsync<Track>(DatabaseConstants.Tracks, cancellationToken);

//            var lookup = DateTime.UtcNow.AddHours(-4);

//            var playbackFilter = Builders<Playback>.Filter.Gte(new StringFieldDefinition<Playback, BsonDateTime>("PlaybackDateTime"), new BsonDateTime(lookup));
//            var playbackResults = await playbackCollection.FindAsync<Playback>(playbackFilter, null, cancellationToken);

//            var playbackEnumerable = await playbackResults.ToListAsync(cancellationToken);

//            var trackFilter = Builders<Track>.Filter.In(new StringFieldDefinition<Track, Guid>("Id"), playbackEnumerable.Select(t => t.TrackId));
//            var trackResults = await mediaCollection.FindAsync<Track>(trackFilter, null, cancellationToken);

//            var trackEnumerable = await trackResults.ToListAsync(cancellationToken);

//            var dispositionFilter = Builders<Disposition>.Filter.In(new StringFieldDefinition<Disposition, Guid>("MediaId"), playbackEnumerable.Select(t => t.TrackId));
//            var dispositionResults = await dispositionCollection.FindAsync<Disposition>(dispositionFilter, null, cancellationToken);

//            var dispositionEnumerable = await dispositionResults.ToListAsync(cancellationToken);

//            var mediaBundles = new List<MediaBundle<Track>>();

//            foreach (var playback in playbackEnumerable)
//            {
//                var mediaBundle = new MediaBundle<Track>();
//                mediaBundle.Playback = playback;
//                mediaBundle.Dispositions = dispositionEnumerable.Where(d => d.MediaId == playback.TrackId);
//                mediaBundle.Media = trackEnumerable.First(t => t.Id == playback.TrackId);

//                mediaBundles.Add(mediaBundle);
//            }

//            return mediaBundles;
//        }

//        public Task<IEnumerable<Role>> GetRolesForUserAsync(Guid userId, CancellationToken cancellationToken)
//        {
//            throw new NotImplementedException();
//        }

//        public async Task<IEnumerable<T>> GetSettingsAsync<T>(CancellationToken cancellationToken) where T : SettingBase
//        {
//            var collection = await GetCollectionAsync<SettingBase>(DatabaseConstants.Configuration, cancellationToken);

//            var filter = Builders<SettingBase>.Filter.Where(s => true);

//            var result = await collection.FindAsync<SettingBase>(filter, null, cancellationToken);

//            var settings = new List<T>();

//            foreach (var r in await result.ToListAsync(cancellationToken))
//            {
//                if (r.GetType() == typeof(T))
//                {
//                    settings.Add((T)r);
//                }
//            }

//            return settings;
//        }

//        public Task<MediaBundle<Track>> GetTrackAsync(Guid userId, string artist, string track, Guid? collectionId, bool populate,
//            CancellationToken cancellationToken)
//        {
//            throw new NotImplementedException();
//        }

//        public Task<MediaBundle<Track>> GetTrackAsync(Guid userId, string path, Guid? collectionId, bool populate, CancellationToken cancellationToken)
//        {
//            throw new NotImplementedException();
//        }

//        public Task<MediaBundle<Track>> GetTrackAsync(Guid userId, Guid id, bool populate, CancellationToken cancellationToken)
//        {
//            throw new NotImplementedException();
//        }

//        public async Task<Track> GetTrackAsync(Guid id, CancellationToken cancellationToken)
//        {
//            var collection = await GetCollectionAsync<Track>(DatabaseConstants.Tracks, cancellationToken);

//            var filter = Builders<Track>.Filter.Eq(t => t.Id, id);

//            var result = await collection.FindAsync(filter, null, cancellationToken);

//            return await result.FirstOrDefaultAsync(cancellationToken);
//        }

//        public async Task<Track> GetTrackAsync(string path, Guid? collectionId, CancellationToken cancellationToken)
//        {
//            var collection = await GetCollectionAsync<Track>(DatabaseConstants.Tracks, cancellationToken);

//            var filter = Builders<Track>.Filter.Eq(t => t.FileMetadata.Path, path);

//            filter = UpdateCollectionFilter(filter, collectionId);

//            var result = await collection.FindAsync(filter, null, cancellationToken);

//            return await result.FirstOrDefaultAsync(cancellationToken);
//        }

//        public Task<IEnumerable<MediaBundle<Track>>> GetTracksAsync(Guid userId, int size, int offset, string genre, int? fromYear, int? toYear, Guid? collectionId,
//            bool populate, bool randomize, CancellationToken cancellationToken)
//        {
//            throw new NotImplementedException();
//        }

//        public Task<IEnumerable<MediaBundle<Track>>> GetTracksAsync(Guid userId, Guid? collectionId, CancellationToken cancellationToken)
//        {
//            throw new NotImplementedException();
//        }

//        public async Task<IEnumerable<Track>> GetTracksAsync(Guid albumId, CancellationToken cancellationToken)
//        {
//            var collection = await GetCollectionAsync<Track>(DatabaseConstants.Tracks, cancellationToken);

//            var filter = Builders<Track>.Filter.Eq(t => t.Album.Id, albumId);

//            var result = await collection.FindAsync(filter, null, cancellationToken);

//            return result.ToEnumerable().OrderBy(t => t.FileMetadata.Path);
//        }

//        public Task<IEnumerable<MediaBundle<Track>>> GetTracksByAlbumAsync(Guid userId, Guid albumId, bool populate, CancellationToken cancellationToken)
//        {
//            throw new NotImplementedException();
//        }

//        public async Task<IEnumerable<Track>> GetTracksByArtistAsync(Guid artistId, CancellationToken cancellationToken)
//        {
//            var collection = await GetCollectionAsync<Track>(DatabaseConstants.Tracks, cancellationToken);

//            var artist = await GetArtistAsync(artistId, cancellationToken);

//            var filter = Builders<Track>.Filter.Where(t => t.Artists.Contains(artist));

//            var result = await collection.FindAsync(filter, null, cancellationToken);

//            return result.ToEnumerable().OrderBy(t => t.FileMetadata.Path);
//        }

//        public Task<IEnumerable<MediaBundle<Track>>> GetTracksByGenreAsync(Guid userId, Guid genreId, bool populate, CancellationToken cancellationToken)
//        {
//            throw new NotImplementedException();
//        }

//        public async Task<User> GetUserAsync(string username, CancellationToken cancellationToken)
//        {
//            var collection = await GetCollectionAsync<SettingBase>(DatabaseConstants.Configuration, cancellationToken);

//            var filter = Builders<SettingBase>.Filter.Where(s => true);

//            var result = await collection.FindAsync<SettingBase>(filter, null, cancellationToken);

//            foreach (var r in await result.ToListAsync(cancellationToken))
//            {
//                if (r.GetType() == typeof(User))
//                {
//                    var user = (User)r;
//                    if (user.Name == username)
//                    {
//                        return user;
//                    }
//                }
//            }

//            return null;
//        }

//        public async Task<User> GetUserAsync(Guid userId, CancellationToken cancellationToken)
//        {
//            var collection = await GetCollectionAsync<SettingBase>(DatabaseConstants.Configuration, cancellationToken);

//            var filter = Builders<SettingBase>.Filter.Empty;

//            var result = await collection.FindAsync<SettingBase>(filter, null, cancellationToken);

//            foreach (var r in await result.ToListAsync(cancellationToken))
//            {
//                if (r.GetType() == typeof(User))
//                {
//                    var user = (User)r;
//                    if (user.Id == userId)
//                    {
//                        return user;
//                    }
//                }
//            }

//            return null;
//        }

//        public Task<IEnumerable<User>> GetUsersAsync(CancellationToken cancellationToken)
//        {
//            throw new NotImplementedException();
//        }

//        Task<IEnumerable<MediaBundle<Artist>>> IMetadataRepository.GetArtistsAsync(Guid userId, Guid? collectionId, CancellationToken cancellationToken)
//        {
//            throw new NotImplementedException();
//        }

//        public async Task InsertOrUpdateAlbumAsync(Album album, CancellationToken cancellationToken)
//        {
//            var collection = await GetCollectionAsync<Album>(DatabaseConstants.Albums, cancellationToken);

//            var filter = Builders<Album>.Filter.Eq(a => a.Id, album.Id);

//            var updateOptions = new UpdateOptions
//            {
//                IsUpsert = true
//            };

//            await collection.ReplaceOneAsync(filter, album, updateOptions, cancellationToken);
//        }

//        public async Task InsertOrUpdateArtistAsync(Artist artist, CancellationToken cancellationToken)
//        {
//            var collection = await GetCollectionAsync<Artist>(DatabaseConstants.Artists, cancellationToken);

//            var filter = Builders<Artist>.Filter.Eq(a => a.Id, artist.Id);

//            var updateOptions = new UpdateOptions
//            {
//                IsUpsert = true
//            };

//            await collection.ReplaceOneAsync(filter, artist, updateOptions, cancellationToken);
//        }

//        public Task InsertOrUpdateCollectionAsync(Collection collection, CancellationToken cancellationToken)
//        {
//            throw new NotImplementedException();
//        }

//        public async Task InsertOrUpdateDispositionAsync(Disposition disposition, CancellationToken cancellationToken)
//        {
//            var collection = await GetCollectionAsync<Disposition>(DatabaseConstants.Disposition, cancellationToken);

//            var filter = Builders<Disposition>.Filter.Eq(d => d.Id, disposition.Id);

//            var updateOptions = new UpdateOptions
//            {
//                IsUpsert = true
//            };

//            await collection.ReplaceOneAsync(filter, disposition, updateOptions, cancellationToken);
//        }

//        public Task InsertOrUpdateFileInfoAsync(Track track, CancellationToken cancellationToken)
//        {
//            throw new NotImplementedException();
//        }

//        public async Task InsertOrUpdateGenreAsync(Genre genre, CancellationToken cancellationToken)
//        {
//            var collection = await GetCollectionAsync<Genre>(DatabaseConstants.Genres, cancellationToken);

//            var filter = Builders<Genre>.Filter.Eq(g => g.Id, genre.Id);

//            var updateOptions = new UpdateOptions
//            {
//                IsUpsert = true
//            };

//            await collection.ReplaceOneAsync(filter, genre, updateOptions, cancellationToken);
//        }

//        public Task InsertOrUpdateMediaInfoAsync(MediaInfo mediaInfo, CancellationToken cancellationToken)
//        {
//            throw new NotImplementedException();
//        }

//        public Task InsertOrUpdatePlaylistAsync(Playlist playlist, CancellationToken cancellationToken)
//        {
//            throw new NotImplementedException();
//        }

//        public Task InsertOrUpdatePlaylistTrackAsync(Guid playlistId, Guid trackId, int position, CancellationToken cancellationToken)
//        {
//            throw new NotImplementedException();
//        }

//        public Task InsertOrUpdatePlayQueueAsync(PlayQueue playQueue, CancellationToken cancellationToken)
//        {
//            throw new NotImplementedException();
//        }

//        public Task InsertOrUpdatePlayQueueTrackAsync(Guid playQueueId, Guid trackId, int position, CancellationToken cancellationToken)
//        {
//            throw new NotImplementedException();
//        }

//        public async Task InsertOrUpdateSettingAsync<T>(T type, CancellationToken cancellationToken) where T : SettingBase
//        {
//            var collection = await GetCollectionAsync<SettingBase>(DatabaseConstants.Configuration, cancellationToken);

//            var filter = Builders<SettingBase>.Filter.Eq(s => s.Id, type.Id);

//            var updateOptions = new UpdateOptions
//            {
//                IsUpsert = true
//            };

//            await collection.ReplaceOneAsync(filter, type, updateOptions, cancellationToken);
//        }

//        public async Task InsertOrUpdateTrackAsync(Track track, CancellationToken cancellationToken)
//        {
//            var collection = await GetCollectionAsync<Track>(DatabaseConstants.Tracks, cancellationToken);

//            var filter = Builders<Track>.Filter.Eq(t => t.Id, track.Id);

//            var updateOptions = new UpdateOptions
//            {
//                IsUpsert = true
//            };

//            await collection.ReplaceOneAsync(filter, track, updateOptions, cancellationToken);
//        }

//        public Task InsertOrUpdateUserAsync(User type, CancellationToken cancellationToken)
//        {
//            throw new NotImplementedException();
//        }

//        public async Task InsertPlaybackAsync(Playback playback, CancellationToken cancellationToken)
//        {
//            var collection = await GetCollectionAsync<Playback>(DatabaseConstants.Playback, cancellationToken);

//            await collection.InsertOneAsync(playback, null, cancellationToken);
//        }

//        public Task RemoveCollectionAsync(Collection collection, CancellationToken cancellationToken)
//        {
//            throw new NotImplementedException();
//        }

//        public async Task RemoveSettingAsync<T>(T type, CancellationToken cancellationToken) where T : SettingBase
//        {
//            var collection = await GetCollectionAsync<SettingBase>(DatabaseConstants.Configuration, cancellationToken);

//            var filter = Builders<SettingBase>.Filter.Eq(s => s.Id, type.Id);

//            await collection.DeleteOneAsync(filter, cancellationToken);
//        }

//        public Task RemoveUserAsync(User user, CancellationToken cancellationToken)
//        {
//            throw new NotImplementedException();
//        }

//        public Task<IEnumerable<MediaBundle<T>>> SearchAsync<T>(Guid userId, string query, int size, int offset, Guid? collectionId, bool populate,
//            CancellationToken cancellationToken) where T : MediaBase, ISearchable, ICollectionIdentifier
//        {
//            throw new NotImplementedException();
//        }

//        public async Task<IEnumerable<T>> SearchAsync<T>(string query, int size, int offset, Guid? collectionId, CancellationToken cancellationToken) where T : ISearchable, ICollectionIdentifier
//        {
//            var genericType = typeof(T);

//            IMongoCollection<T> collection = null;

//            if (genericType == typeof(Artist))
//            {
//                collection = await GetCollectionAsync<T>(DatabaseConstants.Artists, cancellationToken);
//            }
//            else if (genericType == typeof(Album))
//            {
//                collection = await GetCollectionAsync<T>(DatabaseConstants.Albums, cancellationToken);
//            }
//            else if (genericType == typeof(Track))
//            {
//                collection = await GetCollectionAsync<T>(DatabaseConstants.Tracks, cancellationToken);
//            }

//            if (collection == null)
//            {
//                return null;
//            }

//            var filter = Builders<T>.Filter.Empty;

//            if (!string.IsNullOrWhiteSpace(query))
//            {
//                var escapedQuery = Regex.Escape(query).Replace(@"\*", string.Empty).Replace(@"\?", ".");

//                var regex = new Regex(escapedQuery, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

//                filter = filter & Builders<T>.Filter.Where(t => regex.IsMatch(t.Name));
//            }

//            filter = UpdateCollectionFilter(filter, collectionId);

//            var findOptions = new FindOptions<T, T>
//            {
//                Limit = size,
//                Skip = offset
//            };

//            var result = await collection.FindAsync(filter, findOptions, cancellationToken);

//            var list = await result.ToListAsync(cancellationToken);

//            if (genericType == typeof(Album))
//            {
//                foreach (var album in list)
//                {
//                    var a = album as Album;

//                    var albumTracks = await GetTracksAsync(a.Id, cancellationToken);

//                    foreach (var track in albumTracks)
//                    {
//                        a.AddTrack(track);
//                    }
//                }
//            }

//            var filteredResult = list.OrderBy(t => t.Name);

//            return filteredResult;
//        }

//        public async Task SetDispositionAsync(Disposition disposition, CancellationToken cancellationToken)
//        {
//            await InsertOrUpdateDispositionAsync(disposition, cancellationToken);
//        }

//        public Task UpdatePlaylistAsync(Playlist playlist, CancellationToken cancellationToken)
//        {
//            throw new NotImplementedException();
//        }

//        public Task UpdatePlayQueueAsync(PlayQueue playQueue, CancellationToken cancellationToken)
//        {
//            throw new NotImplementedException();
//        }

//        private static FilterDefinition<T> UpdateCollectionFilter<T>(FilterDefinition<T> filter, Guid? collectionId) where T : ICollectionIdentifier
//        {
//            if (collectionId.HasValue)
//            {
//                var id = collectionId.Value;

//                Builders<T>.Filter.Eq(a => a.CollectionId, id);
//            }

//            return filter;
//        }

//        private static Expression<Func<T, bool>> UpdatePredicate<T>(Expression<Func<T, bool>> predicate, Guid? collectionId) where T : ICollectionIdentifier
//        {
//            if (collectionId.HasValue)
//            {
//                var id = collectionId.Value;

//                predicate.And(a => a.CollectionId.Equals(id));
//            }

//            return predicate;
//        }

//        private async Task CreateAlbumIndexesAsync(IMongoCollection<Album> collection, CancellationToken cancellationToken)
//        {
//            var builder = new IndexKeysDefinitionBuilder<Album>();
//            var indexFilter = builder.Ascending(g => g.Name);
//            await collection.Indexes.CreateOneAsync(indexFilter, null, cancellationToken);
//        }

//        private async Task CreateArtistIndexesAsync(IMongoCollection<Artist> collection, CancellationToken cancellationToken)
//        {
//            var options = new CreateIndexOptions() { Unique = true };

//            var builder = new IndexKeysDefinitionBuilder<Artist>();
//            var indexFilter = builder.Ascending(g => g.Name);
//            await collection.Indexes.CreateOneAsync(indexFilter, options, cancellationToken);
//        }

//        private async Task CreateDispositionIndexesAsync(IMongoCollection<Disposition> collection, CancellationToken cancellationToken)
//        {
//            var builder = new IndexKeysDefinitionBuilder<Disposition>();

//            var options = new CreateIndexOptions() { Unique = true };

//            var indexFilter = builder.Ascending(d => d.MediaId).Ascending(d => d.UserId);
//            await collection.Indexes.CreateOneAsync(indexFilter, options, cancellationToken);

//            builder = new IndexKeysDefinitionBuilder<Disposition>();
//            indexFilter = builder.Ascending(p => p.MediaId);
//            await collection.Indexes.CreateOneAsync(indexFilter, null, cancellationToken);
//        }

//        private async Task CreateGenreIndexesAsync(IMongoCollection<Genre> collection, CancellationToken cancellationToken)
//        {
//            var options = new CreateIndexOptions() { Unique = true };

//            var builder = new IndexKeysDefinitionBuilder<Genre>();
//            var indexFilter = builder.Ascending(g => g.Name);
//            await collection.Indexes.CreateOneAsync(indexFilter, options, cancellationToken);
//        }

//        private async Task CreateIndexesAsync<T>(IMongoCollection<T> collection, CancellationToken cancellationToken)
//        {
//            var collectionType = typeof(T);

//            if (collectionType == typeof(Album))
//            {
//                await CreateAlbumIndexesAsync(collection as IMongoCollection<Album>, cancellationToken);
//            }
//            else if (collectionType == typeof(Artist))
//            {
//                await CreateArtistIndexesAsync(collection as IMongoCollection<Artist>, cancellationToken);
//            }
//            else if (collectionType == typeof(Genre))
//            {
//                await CreateGenreIndexesAsync(collection as IMongoCollection<Genre>, cancellationToken);
//            }
//            else if (collectionType == typeof(Track))
//            {
//                await CreateTrackIndexesAsync(collection as IMongoCollection<Track>, cancellationToken);
//            }
//            else if (collectionType == typeof(Disposition))
//            {
//                await CreateDispositionIndexesAsync(collection as IMongoCollection<Disposition>, cancellationToken);
//            }
//            else if (collectionType == typeof(Playback))
//            {
//                await CreatePlaybackIndexesAsync(collection as IMongoCollection<Playback>, cancellationToken);
//            }
//        }

//        private async Task CreatePlaybackIndexesAsync(IMongoCollection<Playback> collection, CancellationToken cancellationToken)
//        {
//            var builder = new IndexKeysDefinitionBuilder<Playback>();
//            var indexFilter = builder.Ascending(p => p.TrackId);
//            await collection.Indexes.CreateOneAsync(indexFilter, null, cancellationToken);

//            builder = new IndexKeysDefinitionBuilder<Playback>();
//            indexFilter = builder.Ascending(p => p.UserId);
//            await collection.Indexes.CreateOneAsync(indexFilter, null, cancellationToken);
//        }

//        private async Task CreateTrackIndexesAsync(IMongoCollection<Track> collection, CancellationToken cancellationToken)
//        {
//            var options = new CreateIndexOptions() { Unique = true };

//            var keys = new IndexKeysDefinitionBuilder<Track>().Ascending(t => t.FileMetadata.Path).Ascending(t => t.CollectionId);
//            await collection.Indexes.CreateOneAsync(keys, options, cancellationToken);

//            var builder = new IndexKeysDefinitionBuilder<Track>();
//            var indexFilter = builder.Ascending(t => t.ReleaseDate);
//            await collection.Indexes.CreateOneAsync(indexFilter, null, cancellationToken);

//            builder = new IndexKeysDefinitionBuilder<Track>();
//            indexFilter = builder.Ascending(t => t.Album.Id);
//            await collection.Indexes.CreateOneAsync(indexFilter, null, cancellationToken);
//        }

//        private async Task<Expression<Func<Album, bool>>> GetAlbumPredicateAsync(string genre, int? fromYear, int? toYear, Guid? collectionId, CancellationToken cancellationToken)
//        {
//            var predicate = PredicateBuilder.True<Album>();

//            if (!string.IsNullOrWhiteSpace(genre))
//            {
//                var filterGenre = await GetGenreAsync(genre, collectionId, cancellationToken);

//                if (filterGenre != null)
//                {
//                    predicate = predicate.And(a => a.Genres.Contains(filterGenre));
//                }
//            }

//            if (fromYear.HasValue)
//            {
//                predicate = predicate.And(a => a.ReleaseDate >= fromYear.Value);
//            }

//            if (toYear.HasValue)
//            {
//                predicate = predicate.And(a => a.ReleaseDate <= toYear.Value);
//            }

//            return UpdatePredicate(predicate, collectionId);
//        }

//        private async Task<IMongoCollection<T>> GetCollectionAsync<T>(string collectionName, CancellationToken cancellationToken)
//        {
//            var db = _client.GetDatabase(DatabaseConstants.Database);
//            var collection = db.GetCollection<T>(collectionName);

//            if (collection == null)
//            {
//                await db.CreateCollectionAsync(collectionName, null, cancellationToken);
//                collection = db.GetCollection<T>(collectionName);

//                await CreateIndexesAsync(collection, cancellationToken);
//            }

//            return collection;
//        }

//        private async Task<IEnumerable<Album>> PopulateAlbumTracks(IEnumerable<Album> results, CancellationToken cancellationToken)
//        {
//            var albums = new List<Album>();

//            foreach (var r in results)
//            {
//                var albumTracks = await GetTracksAsync(r.Id, cancellationToken);

//                foreach (var track in albumTracks)
//                {
//                    r.AddTrack(track);
//                }

//                albums.Add(r);
//            }

//            return albums;
//        }

//        private async Task<bool> ShouldCreateIndex<T>(IMongoCollection<T> collection, string name, string value, CancellationToken cancellationToken)
//        {
//            var createIndex = false;

//            if (collection.Indexes == null)
//            {
//                createIndex = true;
//            }
//            else
//            {
//                var indexes = await collection.Indexes.ListAsync(cancellationToken);

//                var indexList = await indexes.ToListAsync(cancellationToken);

//                createIndex = !indexList.Any();

//                if (!createIndex)
//                {
//                    createIndex = true;

//                    foreach (var index in indexList)
//                    {
//                        if (index.IsBsonDocument)
//                        {
//                            var bsonDocument = index.AsBsonDocument;
//                            var indexExists = bsonDocument.Any(a => a.Name == name && a.Value == value);

//                            if (indexExists)
//                            {
//                                createIndex = false;
//                                break;
//                            }
//                        }
//                    }
//                }
//            }

//            return createIndex;
//        }

//        public void BeginTransaction(CancellationToken cancellationToken)
//        {
//            throw new NotImplementedException();
//        }

//        public Task DeleteMarkerAsync(Guid userId, Guid trackId, CancellationToken cancellationToken)
//        {
//            throw new NotImplementedException();
//        }

//        public Task DeletePlaylistAsync(Guid userId, Guid id, CancellationToken cancellationToken)
//        {
//            throw new NotImplementedException();
//        }

//        public void EndTransaction(bool commit, CancellationToken cancellationToken)
//        {
//            throw new NotImplementedException();
//        }

//        public Task<IEnumerable<Disposition>> GetDispositionsAsync(Guid userId, MediaType mediaType, CancellationToken cancellationToken)
//        {
//            throw new NotImplementedException();
//        }

//        public Task<IEnumerable<MediaBundle<T>>> GetFavoritedAsync<T>(Guid userId, Guid? collectionId, bool populate, CancellationToken cancellationToken) where T : MediaBase, ISearchable, ICollectionIdentifier
//        {
//            throw new NotImplementedException();
//        }

//        public Task<IEnumerable<Marker>> GetMarkersAsync(Guid userId, CancellationToken cancellationToken)
//        {
//            throw new NotImplementedException();
//        }

//        public Task<MediaType?> GetMediaTypeAsync(Guid mediaId, CancellationToken cancellationToken)
//        {
//            throw new NotImplementedException();
//        }

//        public Task InsertOrUpdateMarkerAsync(Marker marker, CancellationToken cancellationToken)
//        {
//            throw new NotImplementedException();
//        }
//    }
//}