using ImageSharp;
using Resonance.Common;
using Resonance.Data.Media.Common;
using Resonance.Data.Media.Tag;
using Resonance.Data.Models;
using Resonance.Data.Storage.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Resonance.Data.Storage
{
    public class MediaLibrary : IMediaLibrary
    {
        private static readonly object _mutex = new object();
        private static volatile bool _scanningLibrary = false;
        private static volatile ScanProgress _scanProgress = null;
        private readonly ILastFmClient _lastFmClient;
        private readonly IMetadataRepository _metadataRepository;
        private readonly MetadataRepositoryCache<TagLibTagReader> _metadataRepositoryCache;
        private readonly MetadataRepositorySettings _metadataRepositorySettings;
        private readonly ISettingsRepository _settingsRepository;
        private readonly ITagReaderFactory _tagReaderFactory;

        public MediaLibrary(IMetadataRepository metadataRepository, ILastFmClient lastFmClient, MetadataRepositorySettings metadataRepositorySettings) : this(metadataRepository, lastFmClient)
        {
            _metadataRepositorySettings = metadataRepositorySettings;
        }

        public MediaLibrary(IMetadataRepository metadataRepository, ILastFmClient lastFmClient)
        {
            _metadataRepository = metadataRepository;
            _settingsRepository = new SettingsRepository(_metadataRepository);
            _tagReaderFactory = new TagReaderFactory();
            _metadataRepositoryCache = new MetadataRepositoryCache<TagLibTagReader>(_metadataRepository, _tagReaderFactory, this);
            _lastFmClient = lastFmClient;
        }

        public bool ScanInProgress
        {
            get
            {
                lock (_mutex)
                {
                    return _scanningLibrary;
                }
            }
            set
            {
                lock (_mutex)
                {
                    _scanningLibrary = value;
                }
            }
        }

        public ScanProgress ScanProgress
        {
            get
            {
                lock (_mutex)
                {
                    return _scanProgress;
                }
            }

            set
            {
                lock (_mutex)
                {
                    _scanProgress = value;
                }
            }
        }

        public bool UseCache
        {
            get
            {
                lock (_mutex)
                {
                    return !_scanningLibrary;
                }
            }

            set
            {
                lock (_mutex)
                {
                    _scanningLibrary = !value;
                }
            }
        }

        public async Task AddUserAsync(string username, string password, CancellationToken cancellationToken)
        {
            await _settingsRepository.AddUserAsync(username, password, cancellationToken);
        }

        public async Task ClearLibraryAsync(Guid userId, Guid? collectionId, CancellationToken cancellationToken)
        {
            await _metadataRepository.ClearCollectionAsync<Album>(collectionId, cancellationToken);
            await _metadataRepository.ClearCollectionAsync<Artist>(collectionId, cancellationToken);
            await _metadataRepository.ClearCollectionAsync<Genre>(collectionId, cancellationToken);
            await _metadataRepository.ClearCollectionAsync<Track>(collectionId, cancellationToken);
            await _metadataRepository.ClearCollectionAsync<Disposition>(collectionId, cancellationToken);
            await _metadataRepository.ClearCollectionAsync<Playback>(collectionId, cancellationToken);
        }

        public async Task<MediaBundle<Album>> GetAlbumAsync(Guid userId, Guid id, bool populate, CancellationToken cancellationToken)
        {
            return await _metadataRepositoryCache.GetAlbumAsync(userId, id, populate, cancellationToken);
        }

        public async Task<MediaBundle<Album>> GetAlbumAsync(Guid userId, string[] albumArtists, string name, Guid collectionId, bool populate, CancellationToken cancellationToken)
        {
            var artists = await GetArtistsFromListAsync(userId, albumArtists, collectionId, cancellationToken);

            return await _metadataRepositoryCache.GetAlbumAsync(userId, artists, name, collectionId, populate, cancellationToken);
        }

        public async Task<MediaInfo> GetAlbumInfoAsync(Album album, CancellationToken cancellationToken)
        {
            //var mediaInfo = await _metadataRepository.GetMediaInfoAsync(album.Id, cancellationToken);

            //if (mediaInfo == null)
            //{
            var mediaInfo = await _lastFmClient.GetAlbumInfoAsync(album, cancellationToken);

            //	if (mediaInfo != null)
            //	{
            //		await _metadataRepository.InsertOrUpdateMediaInfoAsync(mediaInfo, cancellationToken);
            //	}
            //}

            return mediaInfo;
        }

        public async Task<IEnumerable<MediaBundle<Album>>> GetAlbumsAsync(Guid userId, Guid? collectionId, bool populate, CancellationToken cancellationToken)
        {
            return await _metadataRepository.GetAlbumsAsync(userId, collectionId, populate, cancellationToken);
        }

        public async Task<IEnumerable<MediaBundle<Album>>> GetAlbumsByArtistAsync(Guid userId, Guid artistId, bool populate, CancellationToken cancellationToken)
        {
            return await _metadataRepositoryCache.GetAlbumsByArtistAsync(userId, artistId, populate, cancellationToken);
        }

        public async Task<MediaBundle<Artist>> GetArtistAsync(Guid userId, Guid id, CancellationToken cancellationToken)
        {
            return await _metadataRepositoryCache.GetArtistAsync(userId, id, cancellationToken);
        }

        public async Task<MediaBundle<Artist>> GetArtistAsync(Guid userId, string name, Guid collectionId, bool create, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            if (create)
            {
                return await _metadataRepositoryCache.GetArtistAsync(userId, name, collectionId, cancellationToken);
            }
            else
            {
                return await _metadataRepository.GetArtistAsync(userId, name, collectionId, cancellationToken);
            }
        }

        public async Task<MediaInfo> GetArtistInfoAsync(Artist artist, CancellationToken cancellationToken)
        {
            //var mediaInfo = await _metadataRepository.GetMediaInfoAsync(artist.Id, cancellationToken);

            //if (mediaInfo == null)
            //{
            var mediaInfo = await _lastFmClient.GetArtistInfoAsync(artist, cancellationToken);

            //	if (mediaInfo != null)
            //	{
            //		await _metadataRepository.InsertOrUpdateMediaInfoAsync(mediaInfo, cancellationToken);
            //	}
            //}

            return mediaInfo;
        }

        public async Task<IEnumerable<MediaBundle<Artist>>> GetArtistsAsync(Guid userId, Guid? collectionId, CancellationToken cancellationToken)
        {
            return await _metadataRepositoryCache.GetArtistsAsync(userId, collectionId, cancellationToken);
        }

        public async Task<HashSet<Artist>> GetArtistsFromListAsync(Guid userId, IEnumerable<string> artistNames, Guid collectionId, CancellationToken cancellationToken)
        {
            HashSet<Artist> artists = new HashSet<Artist>();

            foreach (var artistName in artistNames)
            {
                var artist = await GetArtistAsync(userId, artistName, collectionId, true, cancellationToken);
                artists.Add(artist.Media);
            }

            return artists.Any() ? artists : null;
        }

        public async Task<CoverArt> GetCoverArtAsync(Guid userId, Guid id, int? size, CancellationToken cancellationToken)
        {
            var mediaBundle = await _metadataRepositoryCache.GetTrackAsync(userId, id, false, cancellationToken);

            Track track = null;

            if (mediaBundle == null)
            {
                return null;
            }
            else
            {
                track = mediaBundle.Media as Track;
            }

            var coverArtDirectory = Path.Combine(Path.Combine(_metadataRepositorySettings.ResonancePath, "CoverArt"), size.HasValue ? size.Value.ToString() : "full");

            var coverArtPath = Path.Combine(coverArtDirectory, id.ToString("n"));

            if (File.Exists(coverArtPath) && track.DateFileModified < File.GetLastWriteTimeUtc(coverArtPath))
            {
                var coverArtReturn = new CoverArt()
                {
                    CoverArtData = File.ReadAllBytes(coverArtPath),
                    CoverArtType = CoverArtType.Front,
                    MediaId = id
                };

                coverArtReturn.Size = coverArtReturn.CoverArtData.Length;
                coverArtReturn.MimeType = MimeType.GetMimeType(coverArtReturn.CoverArtData, coverArtPath);

                return coverArtReturn;
            }

            var tagReader = _tagReaderFactory.Create<TagLibTagReader>(track.Path);

            var coverArt = tagReader.CoverArt.FirstOrDefault(ca => ca.CoverArtType == CoverArtType.Front || ca.CoverArtType == CoverArtType.Other);

            if (coverArt == null)
            {
                return null;
            }

            if (size.HasValue)
            {
                var bytes = coverArt.CoverArtData;

                using (var memoryStream = new MemoryStream(bytes))
                using (var imageMemoryStream = new MemoryStream())
                using (var image = Image.Load(memoryStream))
                {
                    var height = (size.Value / image.Width) * image.Height;

                    image.Resize(size.Value, height).SaveAsPng(imageMemoryStream);

                    coverArt.CoverArtData = imageMemoryStream.ToArray();
                }
            }

            if (!Directory.Exists(coverArtDirectory))
            {
                Directory.CreateDirectory(coverArtDirectory);
            }

            File.WriteAllBytes(coverArtPath, coverArt.CoverArtData);

            coverArt.MimeType = MimeType.GetMimeType(coverArt.CoverArtData, coverArtPath);

            return coverArt;
        }

        public async Task<IEnumerable<MediaBundle<Album>>> GetFavoritedAlbumsAsync(Guid userId, int size, int offset, string genre, int? fromYear, int? toYear, Guid? collectionId, bool populate, CancellationToken cancellationToken)
        {
            return await _metadataRepository.GetFavoritedAlbumsAsync(userId, size, offset, genre, fromYear, toYear, collectionId, populate, cancellationToken);
        }

        public async Task<Genre> GetGenreAsync(string name, Guid collectionId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            return await _metadataRepositoryCache.GetGenreAsync(name, collectionId, cancellationToken);
        }

        public async Task<Dictionary<string, Tuple<int, int>>> GetGenreCountsAsync(Guid? collectionId, CancellationToken cancellationToken)
        {
            return await _metadataRepositoryCache.GetGenreCountsAsync(collectionId, cancellationToken);
        }

        public async Task<IEnumerable<Genre>> GetGenresAsync(Guid? collectionId, CancellationToken cancellationToken)
        {
            return await _metadataRepositoryCache.GetGenresAsync(collectionId, cancellationToken);
        }

        public async Task<HashSet<Genre>> GetGenresFromListAsync(IEnumerable<string> genreNames, Guid collectionId, CancellationToken cancellationToken)
        {
            HashSet<Genre> genres = new HashSet<Genre>();

            foreach (var genreName in genreNames)
            {
                var genre = await GetGenreAsync(genreName, collectionId, cancellationToken);
                genres.Add(genre);
            }

            return genres.Any() ? genres : null;
        }

        public async Task<IEnumerable<MediaBundle<Album>>> GetHighestRatedAlbumsAsync(Guid userId, int size, int offset, string genre, int? fromYear, int? toYear, Guid? collectionId, bool populate, CancellationToken cancellationToken)
        {
            return await _metadataRepository.GetHighestRatedAlbumsAsync(userId, size, offset, genre, fromYear, toYear, collectionId, populate, cancellationToken);
        }


        public async Task<IEnumerable<MediaBundle<Album>>> GetNewestAlbumsAsync(Guid userId, int size, int offset, string genre, int? fromYear, int? toYear, Guid? collectionId, bool populate, CancellationToken cancellationToken)
        {
            return await _metadataRepository.GetNewestAlbumsAsync(userId, size, offset, genre, fromYear, toYear, collectionId, populate, cancellationToken);
        }

        public async Task<Playlist> GetPlaylistAsync(Guid userId, Guid id, bool getTracks, CancellationToken cancellationToken)
        {
            return await _metadataRepositoryCache.GetPlaylistAsync(userId, id, getTracks, cancellationToken);
        }

        public async Task<IEnumerable<Playlist>> GetPlaylistsAsync(Guid userId, string username, bool getTracks, CancellationToken cancellationToken)
        {
            return await _metadataRepositoryCache.GetPlaylistsAsync(userId, username, getTracks, cancellationToken);
        }

        public async Task<IEnumerable<MediaBundle<Album>>> GetRandomAlbumsAsync(Guid userId, int size, int offset, string genre, int? fromYear, int? toYear, Guid? collectionId, bool populate, CancellationToken cancellationToken)
        {
            return await _metadataRepository.GetRandomAlbumsAsync(userId, size, offset, genre, fromYear, toYear, collectionId, populate, cancellationToken);
        }

        public async Task<IEnumerable<MediaInfo>> GetSimilarArtistsAsync(Guid userId, Artist artist, bool autocorrect, int limit, Guid collectionId, CancellationToken cancellationToken)
        {
            List<MediaInfo> mediaInfo = new List<MediaInfo>();

            var similarArtists = await _lastFmClient.GetSimilarArtistsAsync(artist, autocorrect, limit, cancellationToken);

            foreach (var similarArtist in similarArtists)
            {
                var artistModel = await GetArtistAsync(userId, similarArtist.LastFm.Name, collectionId, false, cancellationToken);

                if (artistModel != null)
                {
                    similarArtist.MediaId = artistModel.Media.Id;
                }

                mediaInfo.Add(similarArtist);
            }

            return mediaInfo;
        }

        public async Task<IEnumerable<MediaInfo>> GetTopTracksAsync(string artist, int count, CancellationToken cancellationToken)
        {
            return await _lastFmClient.GetTopTracksAsync(artist, count, cancellationToken);
        }

        public async Task<MediaBundle<Track>> GetTrackAsync(Guid userId, Guid id, bool populate, CancellationToken cancellationToken)
        {
            return await _metadataRepositoryCache.GetTrackAsync(userId, id, populate, cancellationToken);
        }

        public async Task<MediaBundle<Track>> GetTrackAsync(Guid userId, string artist, string track, Guid? collectionId, bool populate, CancellationToken cancellationToken)
        {
            return await _metadataRepositoryCache.GetTrackAsync(userId, artist, track, collectionId, populate, cancellationToken);
        }

        public async Task<IEnumerable<MediaBundle<Track>>> GetTracksAsync(Guid userId, int size, int offset, string genre, int? fromYear, int? toYear, Guid? collectionId, bool populate, bool randomize, CancellationToken cancellationToken)
        {
            return await _metadataRepository.GetTracksAsync(userId, size, offset, genre, fromYear, toYear, collectionId, populate, randomize, cancellationToken);
        }

        public Task<IEnumerable<MediaBundle<Track>>> GetTracksAsync(Guid userId, Guid? collectionId, bool populate, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public void RemovePlaylistFromCache(Guid userId, Guid id, bool getTracks)
        {
            _metadataRepositoryCache.RemovePlaylistFromCache(userId, id, getTracks);
        }

        public void ScanLibrary(Guid userId, Guid? collectionId, bool clear, CancellationToken cancellationToken)
        {
            if (ScanInProgress)
            {
                return;
            }

            Task.Run(async () =>
             {
                 ScanProgress = new ScanProgress();
                 ScanInProgress = true;

                 try
                 {
                     var collections = await _settingsRepository.GetCollectionsAsync(cancellationToken);

                     int collectionCount = 0;

                     var collectionsToScan = collections.Where(c => c.Enabled);

                     if (collectionId.HasValue)
                     {
                         collectionsToScan = collectionsToScan.Where(c => c.Id == collectionId);
                     }

                     int totalCollectionCount = collectionsToScan.Count();

                     ScanProgress.TotalCollectionCount = totalCollectionCount;

                     foreach (var collection in collectionsToScan)
                     {
                         collectionCount++;

                         ScanProgress.CurrentCollection = collectionCount;
                         ScanProgress.CurrentCollectionId = collection.Id;

                         if (!Directory.Exists(collection.Path))
                         {
                             throw new Exception();
                         }

                         if (clear)
                         {
                             collection.DateModified = DateTime.MinValue;
                             await _metadataRepository.InsertOrUpdateCollectionAsync(collection, cancellationToken);
                         }

                         var files = FileUtilities.FindFiles(collection.Path, collection.Filter, true);

                         int fileCount = 0;
                         int totalFileCount = files.Count;

                         ScanProgress.TotalFileCount = totalFileCount;

                         foreach (var file in files)
                         {
                             fileCount++;

                             ScanProgress.CurrentFile = fileCount;
                             ScanProgress.CurrentFilename = file.FullName;

                             if (fileCount == 1 || fileCount % 250 == 0)
                             {
                                 _metadataRepository.EndTransaction(true, cancellationToken);
                                 _metadataRepository.BeginTransaction(cancellationToken);
                             }

                             var track = await _metadataRepositoryCache.GetTrackAsync(userId, file.FullName, collection.Id, false, true, cancellationToken);

                             if (!ScanInProgress)
                             {
                                 break;
                             }
                         }

                         if (!ScanInProgress)
                         {
                             break;
                         }

                         _metadataRepository.EndTransaction(true, cancellationToken);

                         foreach (var track in await _metadataRepository.GetTracksAsync(userId, collection.Id, cancellationToken))
                         {
                             var fileExists = File.Exists(track.Media.Path);

                             if (track.Media.Visible != fileExists)
                             {
                                 track.Media.Visible = fileExists;

                                 await _metadataRepository.InsertOrUpdateFileInfoAsync(track.Media, cancellationToken);

                                 if (!fileExists)
                                 {
                                     await _metadataRepository.DeleteTrackReferencesAsync(track.Media, cancellationToken);
                                 }
                             }
                         }

                         await _metadataRepository.DeleteAlbumReferencesAsync(cancellationToken);
                     }
                 }
                 catch (Exception ex)
                 {
                     _metadataRepository.EndTransaction(false, cancellationToken);

                     File.WriteAllText(string.Format("{0}.txt", Guid.NewGuid().ToString("n")), ex.ToString());
                 }
                 finally
                 {
                     ScanProgress = null;
                     UseCache = true;
                 }
             });
        }

        public async Task<IEnumerable<MediaBundle<Album>>> SearchAlbumsAsync(Guid userId, string query, int size, int offset, Guid? collectionId, bool populate, CancellationToken cancellationToken)
        {
            return await _metadataRepository.SearchAsync<Album>(userId, query, size, offset, collectionId, populate, cancellationToken);
        }

        public async Task<IEnumerable<MediaBundle<Artist>>> SearchArtistsAsync(Guid userId, string query, int size, int offset, Guid? collectionId, bool populate, CancellationToken cancellationToken)
        {
            return await _metadataRepository.SearchAsync<Artist>(userId, query, size, offset, collectionId, populate, cancellationToken);
        }

        public async Task<IEnumerable<MediaBundle<Track>>> SearchTracksAsync(Guid userId, string query, int size, int offset, Guid? collectionId, bool populate, CancellationToken cancellationToken)
        {
            return await _metadataRepository.SearchAsync<Track>(userId, query, size, offset, collectionId, populate, cancellationToken);
        }

        public Task SetPasswordAsync(string username, string password)
        {
            throw new NotImplementedException();
        }

        public void StopScanningLibrary(Guid userId, CancellationToken cancellationToken)
        {
            ScanInProgress = false;
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

            var albumMediaBundle = await GetAlbumAsync(userId, albumArtists, tagReader.AlbumName, collectionId, false, cancellationToken);

            Track track = null;

            if (albumMediaBundle == null)
            {
                track = new Track();
            }
            else
            {
                track = new Track(albumMediaBundle.Media as Album);
            }

            var artists = await GetArtistsFromListAsync(userId, tagReader.Artists, collectionId, cancellationToken);

            if (artists != null && artists.Any())
            {
                var artistMediaBundles = new List<MediaBundle<Artist>>();

                foreach (var artist in artists)
                {
                    artistMediaBundles.Add(new MediaBundle<Artist>
                    {
                        Media = artist
                    });
                }

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

            var genres = await GetGenresFromListAsync(tagReader.Genres, collectionId, cancellationToken);

            if (genres != null && genres.Any())
            {
                track.Genres = new HashSet<Genre>(genres);
            }
            else
            {
                track.Genres = new HashSet<Genre>();
            }

            track.Number = (int)tagReader.TrackNumber;

            var composers = await GetArtistsFromListAsync(userId, tagReader.Composers, collectionId, cancellationToken);

            if (composers != null && composers.Any())
            {
                var composerMediaBundles = new List<MediaBundle<Artist>>();

                foreach (var composer in composers)
                {
                    composerMediaBundles.Add(new MediaBundle<Artist>
                    {
                        Media = composer
                    });
                }

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