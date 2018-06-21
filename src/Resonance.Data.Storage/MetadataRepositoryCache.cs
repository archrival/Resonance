using Microsoft.Extensions.Caching.Memory;
using Resonance.Data.Media.Common;
using Resonance.Data.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Resonance.Data.Storage
{
    public class MetadataRepositoryCache : IMetadataRepositoryCache
    {
        private static readonly MemoryCacheEntryOptions DefaultMemoryCacheEntryOptions = new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromMinutes(5),
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
        };

        private readonly IMemoryCache _memoryCache;
        private readonly IMetadataRepository _metadataRepository;
        private readonly ITagReaderFactory _tagReaderFactory;

        public MetadataRepositoryCache(IMetadataRepository metadataRepository, ITagReaderFactory tagReaderFactory, IMemoryCache memoryCache)
        {
            _metadataRepository = metadataRepository;
            _tagReaderFactory = tagReaderFactory;
            _memoryCache = memoryCache;
        }

        private enum CacheTypes
        {
            Artists,
            Genres,
            GenreCounts
        }

        public bool UseCache { get; set; }

        public async Task<MediaBundle<Album>> GetAlbumAsync(Guid userId, string[] albumArtists, string name, Guid collectionId, bool populate, CancellationToken cancellationToken)
        {
            var artists = await GetArtistsFromListAsync(userId, albumArtists, collectionId, cancellationToken).ConfigureAwait(false);

            return await GetAlbumAsync(userId, artists, name, collectionId, populate, cancellationToken);
        }

        public Task<MediaBundle<Album>> GetAlbumAsync(Guid userId, HashSet<Artist> artists, string name, Guid collectionId, bool populate, CancellationToken cancellationToken)
        {
            var repositoryCache = new AlbumRepositoryCache(_metadataRepository, userId, artists, name, collectionId, populate);
            return repositoryCache.GetResultAsync(cancellationToken, UseCache);
        }

        public Task<MediaBundle<Album>> GetAlbumAsync(Guid userId, Guid id, bool populate, CancellationToken cancellationToken)
        {
            var repositoryCache = new AlbumRepositoryIdCache(_metadataRepository, userId, id, populate);
            repositoryCache.SetAddNullToCache(false);
            return repositoryCache.GetResultAsync(cancellationToken, UseCache);
        }

        public Task<IEnumerable<MediaBundle<Album>>> GetAlbumsByArtistAsync(Guid userId, Guid artistId, bool populate, CancellationToken cancellationToken)
        {
            var repositoryCache = new AlbumRepositoryByArtistIdCache(_metadataRepository, userId, artistId, populate);
            return repositoryCache.GetResultAsync(cancellationToken, UseCache);
        }

        public Task<MediaBundle<Artist>> GetArtistAsync(Guid userId, Guid id, CancellationToken cancellationToken)
        {
            var repositoryCache = new ArtistRepositoryIdCache(_metadataRepository, userId, id);
            repositoryCache.SetAddNullToCache(false);
            return repositoryCache.GetResultAsync(cancellationToken, UseCache);
        }

        public Task<MediaBundle<Artist>> GetArtistAsync(Guid userId, string artist, Guid collectionId, CancellationToken cancellationToken)
        {
            var repositoryCache = new ArtistRepositoryCache(_metadataRepository, userId, artist, collectionId);
            return repositoryCache.GetResultAsync(cancellationToken, UseCache);
        }

        public async Task<IEnumerable<MediaBundle<Artist>>> GetArtistsAsync(Guid userId, Guid? collectionId, CancellationToken cancellationToken)
        {
            if (_memoryCache.TryGetValue(CacheTypes.Artists, out IEnumerable<MediaBundle<Artist>> artists))
            {
                return artists;
            }

            artists = (await _metadataRepository.GetArtistsAsync(userId, collectionId, cancellationToken).ConfigureAwait(false)).ToList();

            _memoryCache.Set(CacheTypes.Artists, artists, DefaultMemoryCacheEntryOptions);

            return artists;
        }

        public async Task<HashSet<Artist>> GetArtistsFromListAsync(Guid userId, IEnumerable<string> artistNames, Guid collectionId, CancellationToken cancellationToken)
        {
            var artists = new HashSet<Artist>();

            foreach (var artistName in artistNames)
            {
                var artist = await GetArtistAsync(userId, artistName, collectionId, cancellationToken).ConfigureAwait(false);
                artists.Add(artist.Media);
            }

            return artists.Any() ? artists : null;
        }

        public Task<Genre> GetGenreAsync(string genre, Guid collectionId, CancellationToken cancellationToken)
        {
            var repositoryCache = new GenreRepositoryCache(_metadataRepository, genre, collectionId);
            return repositoryCache.GetResultAsync(cancellationToken, UseCache);
        }

        public async Task<Dictionary<string, Tuple<int, int>>> GetGenreCountsAsync(Guid? collectionId, CancellationToken cancellationToken)
        {
            if (_memoryCache.TryGetValue(CacheTypes.GenreCounts, out Dictionary<string, Tuple<int, int>> genreCounts))
            {
                return genreCounts;
            }

            genreCounts = await _metadataRepository.GetGenreCountsAsync(collectionId, cancellationToken).ConfigureAwait(false);

            _memoryCache.Set(CacheTypes.GenreCounts, genreCounts, DefaultMemoryCacheEntryOptions);

            return genreCounts;
        }

        public async Task<IEnumerable<Genre>> GetGenresAsync(Guid? collectionId, CancellationToken cancellationToken)
        {
            if (_memoryCache.TryGetValue(CacheTypes.Genres, out IEnumerable<Genre> genres))
            {
                return genres;
            }

            genres = (await _metadataRepository.GetGenresAsync(collectionId, cancellationToken).ConfigureAwait(false)).ToList();

            _memoryCache.Set(CacheTypes.Genres, genres, DefaultMemoryCacheEntryOptions);

            return genres;
        }

        public async Task<HashSet<Genre>> GetGenresFromListAsync(IEnumerable<string> genreNames, Guid collectionId, CancellationToken cancellationToken)
        {
            var genres = new HashSet<Genre>();

            foreach (var genreName in genreNames)
            {
                var genre = await GetGenreAsync(genreName, collectionId, cancellationToken);
                genres.Add(genre);
            }

            return genres.Any() ? genres : null;
        }

        public Task<Playlist> GetPlaylistAsync(Guid userId, Guid id, bool getTracks, CancellationToken cancellationToken)
        {
            var repositoryCache = new PlaylistRepositoryIdCache(_metadataRepository, userId, id, getTracks);
            repositoryCache.SetAddNullToCache(false);
            return repositoryCache.GetResultAsync(cancellationToken, UseCache);
        }

        public Task<IEnumerable<Playlist>> GetPlaylistsAsync(Guid userId, string username, bool getTracks, CancellationToken cancellationToken)
        {
            var repositoryCache = new PlaylistsRepositoryIdCache(_metadataRepository, userId, username, getTracks);
            repositoryCache.SetAddNullToCache(false);
            return repositoryCache.GetResultAsync(cancellationToken, UseCache);
        }

        public Task<MediaBundle<Track>> GetTrackAsync(Guid userId, string artist, string track, Guid? collectionId, bool populate, CancellationToken cancellationToken)
        {
            var repositoryCache = new TrackRepositoryArtistAndTrackCache(_metadataRepository, userId, artist, track, collectionId, populate);
            return repositoryCache.GetResultAsync(cancellationToken, UseCache);
        }

        public Task<MediaBundle<Track>> GetTrackAsync(Guid userId, string path, Guid collectionId, bool populate, bool updateCollection, CancellationToken cancellationToken)
        {
            var repositoryCache = new TrackRepositoryPathCache(_metadataRepository, this, _tagReaderFactory, userId, path, collectionId, populate, updateCollection);
            return repositoryCache.GetResultAsync(cancellationToken, UseCache);
        }

        public Task<MediaBundle<Track>> GetTrackAsync(Guid userId, Guid id, bool populate, CancellationToken cancellationToken)
        {
            var repositoryCache = new TrackRepositoryIdCache(_metadataRepository, userId, id, populate);
            repositoryCache.SetAddNullToCache(false);
            return repositoryCache.GetResultAsync(cancellationToken, UseCache);
        }

        public void RemovePlaylistFromCache(Guid userId, Guid id, bool getTracks)
        {
            var repositoryCache = new PlaylistRepositoryIdCache(_metadataRepository, userId, id, getTracks);
            repositoryCache.SetAddNullToCache(false);
            repositoryCache.Remove();
        }

        public async Task<Track> TagReaderToTrackModelAsync(Guid userId, ITagReader tagReader, Guid collectionId, CancellationToken cancellationToken)
        {
            var albumArtists = tagReader.AlbumArtists;
            var trackArtists = tagReader.Artists;

            if (albumArtists == null || !albumArtists.Any())
            {
                if (trackArtists != null && trackArtists.Any())
                {
                    albumArtists = trackArtists;
                }
                else
                {
                    albumArtists = new[] { Path.GetDirectoryName(Path.GetDirectoryName(tagReader.Path)) };
                }
            }

            var albumMediaBundle = await GetAlbumAsync(userId, albumArtists, tagReader.AlbumName, collectionId, false, cancellationToken).ConfigureAwait(false);

            var track = albumMediaBundle == null ? new Track() : new Track(albumMediaBundle.Media);

            var artists = await GetArtistsFromListAsync(userId, tagReader.Artists, collectionId, cancellationToken);

            if (artists != null && artists.Any())
            {
                var artistMediaBundles = artists.Select(artist => new MediaBundle<Artist>
                {
                    Media = artist
                }).ToList();

                track.Artists = new HashSet<MediaBundle<Artist>>(artistMediaBundles);
            }
            else
            {
                track.Artists = new HashSet<MediaBundle<Artist>>();
            }

            track.Comment = tagReader.Comment;
            track.DateAdded = DateTime.UtcNow;
            track.DiscNumber = (int)tagReader.DiscNumber;

            track.DateFileCreated = tagReader.DateCreated;
            track.DateFileModified = tagReader.DateModified;
            track.FileHash = tagReader.Hash;
            track.HashType = tagReader.HashType;
            track.Path = tagReader.Path;
            track.Size = tagReader.Size;

            if (tagReader.CoverArt.Any())
            {
                track.CoverArt = new HashSet<CoverArt>();
            }

            foreach (var coverArt in tagReader.CoverArt)
            {
                coverArt.MediaId = track.Id;
                track.CoverArt.Add(coverArt);
            }

            var genres = await GetGenresFromListAsync(tagReader.Genres, collectionId, cancellationToken).ConfigureAwait(false);

            if (genres != null && genres.Any())
            {
                track.Genres = new HashSet<Genre>(genres);
            }
            else
            {
                track.Genres = new HashSet<Genre>();
            }

            track.Number = (int)tagReader.TrackNumber;

            var composers = await GetArtistsFromListAsync(userId, tagReader.Composers, collectionId, cancellationToken).ConfigureAwait(false);

            if (composers != null && composers.Any())
            {
                var composerMediaBundles = composers.Select(composer => new MediaBundle<Artist>
                {
                    Media = composer
                }).ToList();

                track.WrittenBy = new HashSet<MediaBundle<Artist>>(composerMediaBundles);
            }
            else
            {
                track.WrittenBy = new HashSet<MediaBundle<Artist>>();
            }

            track.Duration = tagReader.Duration;

            track.Bitrate = tagReader.Bitrate;
            track.Channels = tagReader.Channels;
            track.ContentType = tagReader.ContentType;
            track.SampleRate = tagReader.SampleRate;

            track.Name = tagReader.TrackName;
            track.ReleaseDate = (int)tagReader.ReleaseDate;

            track.AlbumGain = double.IsNaN(tagReader.ReplayGainAlbumGain) ? (double?)null : tagReader.ReplayGainAlbumGain;
            track.AlbumPeak = double.IsNaN(tagReader.ReplayGainAlbumPeak) ? (double?)null : tagReader.ReplayGainAlbumPeak;
            track.TrackGain = double.IsNaN(tagReader.ReplayGainTrackGain) ? (double?)null : tagReader.ReplayGainTrackGain;
            track.TrackPeak = double.IsNaN(tagReader.ReplayGainTrackPeak) ? (double?)null : tagReader.ReplayGainTrackPeak;

            return track;
        }
    }
}