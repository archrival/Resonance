using Resonance.Data.Media.Common;
using Resonance.Data.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Resonance.Data.Storage
{
    public interface IMetadataRepositoryCache
    {
        bool UseCache { get; set; }

        Task<MediaBundle<Album>> GetAlbumAsync(Guid userId, Guid id, bool populate, CancellationToken cancellationToken);

        Task<MediaBundle<Album>> GetAlbumAsync(Guid userId, HashSet<Artist> artists, string name, Guid collectionId, bool populate, CancellationToken cancellationToken);

        Task<IEnumerable<MediaBundle<Album>>> GetAlbumsByArtistAsync(Guid userId, Guid artistId, bool populate, CancellationToken cancellationToken);

        Task<MediaBundle<Artist>> GetArtistAsync(Guid userId, Guid id, CancellationToken cancellationToken);

        Task<MediaBundle<Artist>> GetArtistAsync(Guid userId, string artist, Guid collectionId, CancellationToken cancellationToken);

        Task<IEnumerable<MediaBundle<Artist>>> GetArtistsAsync(Guid userId, Guid? collectionId, CancellationToken cancellationToken);

        Task<Genre> GetGenreAsync(string genre, Guid collectionId, CancellationToken cancellationToken);

        Task<Dictionary<string, Tuple<int, int>>> GetGenreCountsAsync(Guid? collectionId, CancellationToken cancellationToken);

        Task<IEnumerable<Genre>> GetGenresAsync(Guid? collectionId, CancellationToken cancellationToken);

        Task<Playlist> GetPlaylistAsync(Guid userId, Guid id, bool getTracks, CancellationToken cancellationToken);

        Task<IEnumerable<Playlist>> GetPlaylistsAsync(Guid userId, string username, bool getTracks, CancellationToken cancellationToken);

        Task<MediaBundle<Track>> GetTrackAsync(Guid userId, Guid id, bool populate, CancellationToken cancellationToken);

        Task<MediaBundle<Track>> GetTrackAsync(Guid userId, string path, Guid collectionId, bool populate, bool updateCollection, CancellationToken cancellationToken);

        Task<MediaBundle<Track>> GetTrackAsync(Guid userId, string artist, string track, Guid? collectionId, bool populate, CancellationToken cancellationToken);

        void RemovePlaylistFromCache(Guid userId, Guid id, bool getTracks);

        Task<Track> TagReaderToTrackModelAsync(Guid userId, ITagReader tagReader, Guid collectionId, CancellationToken cancellationToken);
    }
}