using Resonance.Data.Media.Common;
using Resonance.Data.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Resonance.Data.Storage.Common
{
	public interface IMediaLibrary
	{
		ScanProgress ScanProgress { get; set; }
		bool UseCache { get; set; }

		Task ClearLibraryAsync(Guid userId, Guid? collectionId, CancellationToken cancellationToken);

		Task<MediaBundle<Album>> GetAlbumAsync(Guid userId, Guid id, bool populate, CancellationToken cancellationToken);

		Task<MediaBundle<Album>> GetAlbumAsync(Guid userId, string[] albumArtists, string name, Guid collectionId, bool populate, CancellationToken cancellationToken);

		Task<MediaInfo> GetAlbumInfoAsync(Album album, CancellationToken cancellationToken);

		Task<IEnumerable<MediaBundle<Album>>> GetAlbumsAsync(Guid userId, Guid? collectionId, bool populate, CancellationToken cancellationToken);

		Task<IEnumerable<MediaBundle<Album>>> GetAlbumsByArtistAsync(Guid userId, Guid artistId, bool populate, CancellationToken cancellationToken);

		Task<MediaBundle<Artist>> GetArtistAsync(Guid userId, string name, Guid collectionId, bool create, CancellationToken cancellationToken);

		Task<MediaBundle<Artist>> GetArtistAsync(Guid userId, Guid id, CancellationToken cancellationToken);

		Task<MediaInfo> GetArtistInfoAsync(Artist artist, CancellationToken cancellationToken);

		Task<IEnumerable<MediaBundle<Artist>>> GetArtistsAsync(Guid userId, Guid? collectionId, CancellationToken cancellationToken);

		Task<HashSet<Artist>> GetArtistsFromListAsync(Guid userId, IEnumerable<string> artistNames, Guid collectionId, CancellationToken cancellationToken);

		Task<CoverArt> GetCoverArtAsync(Guid userId, Guid id, int? size, CancellationToken cancellationToken);

		Task<IEnumerable<MediaBundle<Album>>> GetFavoritedAlbumsAsync(Guid userId, int size, int offset, string genre, int? fromYear, int? toYear, Guid? collectionId, bool populate, CancellationToken cancellationToken);

		Task<Genre> GetGenreAsync(string name, Guid collectionId, CancellationToken cancellationToken);

		Task<Dictionary<string, Tuple<int, int>>> GetGenreCountsAsync(Guid? collectionId, CancellationToken cancellationToken);

		Task<IEnumerable<Genre>> GetGenresAsync(Guid? collectionId, CancellationToken cancellationToken);

		Task<HashSet<Genre>> GetGenresFromListAsync(IEnumerable<string> genreNames, Guid collectionId, CancellationToken cancellationToken);

        Task<IEnumerable<MediaBundle<Album>>> GetHighestRatedAlbumsAsync(Guid userId, int size, int offset, string genre, int? fromYear, int? toYear, Guid? collectionId, bool populate, CancellationToken cancellationToken);

        Task<IEnumerable<MediaBundle<Album>>> GetNewestAlbumsAsync(Guid userId, int size, int offset, string genre, int? fromYear, int? toYear, Guid? collectionId, bool populate, CancellationToken cancellationToken);

		Task<Playlist> GetPlaylistAsync(Guid userId, Guid id, bool getTracks, CancellationToken cancellationToken);

		Task<IEnumerable<Playlist>> GetPlaylistsAsync(Guid userId, string username, bool getTracks, CancellationToken cancellationToken);

		Task<IEnumerable<MediaBundle<Album>>> GetRandomAlbumsAsync(Guid userId, int size, int offset, string genre, int? fromYear, int? toYear, Guid? collectionId, bool populate, CancellationToken cancellationToken);

		Task<IEnumerable<MediaInfo>> GetSimilarArtistsAsync(Guid userId, Artist artist, bool autocorrect, int limit, Guid collectionId, CancellationToken cancellationToken);

		Task<IEnumerable<MediaInfo>> GetTopTracksAsync(string artist, int count, CancellationToken cancellationToken);

		Task<MediaBundle<Track>> GetTrackAsync(Guid userId, string artist, string track, Guid? collectionId, bool populate, CancellationToken cancellationToken);

		Task<MediaBundle<Track>> GetTrackAsync(Guid userId, Guid id, bool populate, CancellationToken cancellationToken);

		Task<IEnumerable<MediaBundle<Track>>> GetTracksAsync(Guid userId, int size, int offset, string genre, int? fromYear, int? toYear, Guid? collectionId, bool populate, bool randomize, CancellationToken cancellationToken);

		Task<IEnumerable<MediaBundle<Track>>> GetTracksAsync(Guid userId, Guid? collectionId, bool populate, CancellationToken cancellationToken);

		void RemovePlaylistFromCache(Guid userId, Guid id, bool getTracks);

		void ScanLibrary(Guid userId, Guid? collectionId, bool clear, CancellationToken cancellationToken);

		Task<IEnumerable<MediaBundle<Album>>> SearchAlbumsAsync(Guid userId, string query, int size, int offset, Guid? collectionId, bool populate, CancellationToken cancellationToken);

		Task<IEnumerable<MediaBundle<Artist>>> SearchArtistsAsync(Guid userId, string query, int size, int offset, Guid? collectionId, bool populate, CancellationToken cancellationToken);

		Task<IEnumerable<MediaBundle<Track>>> SearchTracksAsync(Guid userId, string query, int size, int offset, Guid? collectionId, bool populate, CancellationToken cancellationToken);

		void StopScanningLibrary(Guid userId, CancellationToken cancellationToken);

		Task<Track> TagReaderToTrackModelAsync(Guid userId, ITagReader tagReader, Guid collectionId, CancellationToken cancellationToken);
	}
}