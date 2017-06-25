using Microsoft.Extensions.Caching.Memory;
using Resonance.Data.Media.Common;
using Resonance.Data.Models;
using Resonance.Data.Storage.Common;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Resonance.Data.Storage
{
    public class MetadataRepositoryCache<TTagReader> where TTagReader : ITagReader, new()
    {
        private readonly IMediaLibrary _mediaLibrary;
        private readonly IMemoryCache _memoryCache;
        private readonly IMetadataRepository _metadataRepository;
        private readonly ITagReaderFactory _tagReaderFactory;

        public MetadataRepositoryCache(IMetadataRepository metadataRepository, ITagReaderFactory tagReaderFactory, IMediaLibrary mediaLibrary)
        {
            _metadataRepository = metadataRepository;
            _tagReaderFactory = tagReaderFactory;
            _mediaLibrary = mediaLibrary;

            var memoryCacheOptions = new MemoryCacheOptions
            {
                CompactOnMemoryPressure = true,
                ExpirationScanFrequency = TimeSpan.FromMinutes(30)
            };

            _memoryCache = new MemoryCache(memoryCacheOptions);
        }

        private enum CacheTypes
        {
            Artists,
            Genres,
            GenreCounts
        }

        public async Task<MediaBundle<Album>> GetAlbumAsync(Guid userId, HashSet<Artist> artists, string name, Guid collectionId, bool populate, CancellationToken cancellationToken)
        {
            var repositoryCache = new AlbumRepositoryCache(_metadataRepository, userId, artists, name, collectionId, populate);
            return await repositoryCache.GetResultAsync(cancellationToken, _mediaLibrary.UseCache);
        }

        public async Task<MediaBundle<Album>> GetAlbumAsync(Guid userId, Guid id, bool populate, CancellationToken cancellationToken)
        {
            var repositoryCache = new AlbumRepositoryIdCache<TTagReader>(_metadataRepository, _tagReaderFactory, _mediaLibrary, userId, id, populate);
            repositoryCache.SetAddNullToCache(false);
            return await repositoryCache.GetResultAsync(cancellationToken, _mediaLibrary.UseCache);
        }

        public async Task<IEnumerable<MediaBundle<Album>>> GetAlbumsByArtistAsync(Guid userId, Guid artistId, bool populate, CancellationToken cancellationToken)
        {
            var repositoryCache = new AlbumRepositoryByArtistIdCache<TTagReader>(_metadataRepository, _tagReaderFactory, _mediaLibrary, userId, artistId, populate);
            return await repositoryCache.GetResultAsync(cancellationToken, _mediaLibrary.UseCache);
        }

        public async Task<MediaBundle<Artist>> GetArtistAsync(Guid userId, Guid id, CancellationToken cancellationToken)
        {
            var repositoryCache = new ArtistRepositoryIdCache<TTagReader>(_metadataRepository, _tagReaderFactory, _mediaLibrary, userId, id);
            repositoryCache.SetAddNullToCache(false);
            return await repositoryCache.GetResultAsync(cancellationToken, _mediaLibrary.UseCache);
        }

        public async Task<MediaBundle<Artist>> GetArtistAsync(Guid userId, string artist, Guid collectionId, CancellationToken cancellationToken)
        {
            var repositoryCache = new ArtistRepositoryCache(_metadataRepository, userId, artist, collectionId);
            return await repositoryCache.GetResultAsync(cancellationToken, _mediaLibrary.UseCache);
        }

        public async Task<IEnumerable<MediaBundle<Artist>>> GetArtistsAsync(Guid userId, Guid? collectionId, CancellationToken cancellationToken)
        {
            IEnumerable<MediaBundle<Artist>> artists;

            if (_memoryCache.TryGetValue(CacheTypes.Artists, out artists))
                return artists;

            artists = await _metadataRepository.GetArtistsAsync(userId, collectionId, cancellationToken);

            _memoryCache.Set(CacheTypes.Artists, artists, new MemoryCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromMinutes(5),
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
            });

            return artists;
        }

        public async Task<Genre> GetGenreAsync(string genre, Guid collectionId, CancellationToken cancellationToken)
        {
            var repositoryCache = new GenreRepositoryCache(_metadataRepository, genre, collectionId);
            return await repositoryCache.GetResultAsync(cancellationToken, _mediaLibrary.UseCache);
        }

        public async Task<Dictionary<string, Tuple<int, int>>> GetGenreCountsAsync(Guid? collectionId, CancellationToken cancellationToken)
        {
            Dictionary<string, Tuple<int, int>> genreCounts;

            if (_memoryCache.TryGetValue(CacheTypes.GenreCounts, out genreCounts))
                return genreCounts;

            genreCounts = await _metadataRepository.GetGenreCountsAsync(collectionId, cancellationToken);

            _memoryCache.Set(CacheTypes.GenreCounts, genreCounts, new MemoryCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromMinutes(5),
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
            });

            return genreCounts;
        }

        public async Task<IEnumerable<Genre>> GetGenresAsync(Guid? collectionId, CancellationToken cancellationToken)
        {
            IEnumerable<Genre> genres;

            if (_memoryCache.TryGetValue(CacheTypes.Genres, out genres))
                return genres;

            genres = await _metadataRepository.GetGenresAsync(collectionId, cancellationToken);

            _memoryCache.Set(CacheTypes.Genres, genres, new MemoryCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromMinutes(5),
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
            });

            return genres;
        }

        public async Task<Playlist> GetPlaylistAsync(Guid userId, Guid id, bool getTracks, CancellationToken cancellationToken)
        {
            var repositoryCache = new PlaylistRepositoryIdCache<TTagReader>(_metadataRepository, _tagReaderFactory, _mediaLibrary, userId, id, getTracks);
            repositoryCache.SetAddNullToCache(false);
            return await repositoryCache.GetResultAsync(cancellationToken, _mediaLibrary.UseCache);
        }

        public async Task<IEnumerable<Playlist>> GetPlaylistsAsync(Guid userId, string username, bool getTracks, CancellationToken cancellationToken)
        {
            var repositoryCache = new PlaylistsRepositoryIdCache<TTagReader>(_metadataRepository, _tagReaderFactory, _mediaLibrary, userId, username, getTracks);
            repositoryCache.SetAddNullToCache(false);
            return await repositoryCache.GetResultAsync(cancellationToken, _mediaLibrary.UseCache);
        }

        public async Task<MediaBundle<Track>> GetTrackAsync(Guid userId, string artist, string track, Guid? collectionId, bool populate, CancellationToken cancellationToken)
        {
            var repositoryCache = new TrackRepositoryArtistAndTrackCache<TTagReader>(_metadataRepository, _tagReaderFactory, _mediaLibrary, userId, artist, track, collectionId, populate);
            return await repositoryCache.GetResultAsync(cancellationToken, _mediaLibrary.UseCache);
        }

        public async Task<MediaBundle<Track>> GetTrackAsync(Guid userId, string path, Guid collectionId, bool populate, bool updateCollection, CancellationToken cancellationToken)
        {
            var repositoryCache = new TrackRepositoryPathCache<TTagReader>(_metadataRepository, _tagReaderFactory, _mediaLibrary, userId, path, collectionId, populate, updateCollection);
            return await repositoryCache.GetResultAsync(cancellationToken, _mediaLibrary.UseCache);
        }

        public async Task<MediaBundle<Track>> GetTrackAsync(Guid userId, Guid id, bool populate, CancellationToken cancellationToken)
        {
            var repositoryCache = new TrackRepositoryIdCache<TTagReader>(_metadataRepository, _tagReaderFactory, _mediaLibrary, userId, id, populate);
            repositoryCache.SetAddNullToCache(false);
            return await repositoryCache.GetResultAsync(cancellationToken, _mediaLibrary.UseCache);
        }

        public void RemovePlaylistFromCache(Guid userId, Guid id, bool getTracks)
        {
            var repositoryCache = new PlaylistRepositoryIdCache<TTagReader>(_metadataRepository, _tagReaderFactory, _mediaLibrary, userId, id, getTracks);
            repositoryCache.SetAddNullToCache(false);
            repositoryCache.Remove();
        }
    }
}