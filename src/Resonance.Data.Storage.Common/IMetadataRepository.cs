using Resonance.Data.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Resonance.Data.Storage
{
    public interface IMetadataRepository
    {
        Task AddChatAsync(Chat chat, CancellationToken cancellationToken);

        Task AddCollectionAsync(Collection collection, CancellationToken cancellationToken);

        Task AddPlaybackAsync(Playback playback, CancellationToken cancellationToken);

        Task AddPlaylistAsync(Playlist playlist, CancellationToken cancellationToken);

        Task AddRadioStationAsync(RadioStation radioStation, CancellationToken cancellationToken);

        Task AddUserAsync(User user, CancellationToken cancellationToken);

        void BeginTransaction(CancellationToken cancellationToken);

        Task ClearCollectionAsync<T>(Guid? collectionId, CancellationToken cancellationToken) where T : ModelBase, ICollectionIdentifier;

        Task DeleteAlbumReferencesAsync(CancellationToken cancellationToken);

        Task DeleteMarkerAsync(Guid userId, Guid trackId, CancellationToken cancellationToken);

        Task DeletePlaylistAsync(Guid userId, Guid id, CancellationToken cancellationToken);

        Task DeletePlaylistTracksAsync(Guid id, CancellationToken cancellationToken);

        Task DeletePlayQueueAsync(Guid userId, CancellationToken cancellationToken);

        Task DeletePlayQueueTracksAsync(Guid playQueueId, CancellationToken cancellationToken);

        Task DeleteRadioStationAsync(Guid id, CancellationToken cancellationToken);

        Task DeleteTrackReferencesAsync(Track track, CancellationToken cancellationToken);

        Task DeleteUserAsync(Guid userId, CancellationToken cancellationToken);

        void EndTransaction(bool commit, CancellationToken cancellationToken);

        Task<MediaBundle<Album>> GetAlbumAsync(Guid userId, Guid id, bool populate, CancellationToken cancellationToken);

        Task<MediaBundle<Album>> GetAlbumAsync(Guid userId, HashSet<Artist> artists, string name, Guid? collectionId, bool populate, CancellationToken cancellationToken);

        Task<IEnumerable<MediaBundle<Album>>> GetAlbumsAsync(Guid userId, Guid? collectionId, bool populate, CancellationToken cancellationToken);

        Task<IEnumerable<MediaBundle<Album>>> GetAlbumsByArtistAsync(Guid userId, Guid artistId, bool populate, CancellationToken cancellationToken);

        Task<IEnumerable<MediaBundle<Album>>> GetAlbumsByGenreAsync(Guid userId, Guid genreId, bool populate, CancellationToken cancellationToken);

        Task<IEnumerable<MediaBundle<Album>>> GetAlphabeticalAlbumsAsync(Guid userId, int size, int offset, string genre, int? fromYear, int? toYear, Guid? collectionId, bool populate, CancellationToken cancellationToken);

        Task<IEnumerable<MediaBundle<Album>>> GetAlphabeticalByArtistAlbumsAsync(Guid userId, int size, int offset, string genre, int? fromYear, int? toYear, Guid? collectionId, bool populate, CancellationToken cancellationToken);

        Task<MediaBundle<Artist>> GetArtistAsync(Guid userId, Guid id, CancellationToken cancellationToken);

        Task<MediaBundle<Artist>> GetArtistAsync(Guid userId, string artist, Guid? collectionId, CancellationToken cancellationToken);

        Task<IEnumerable<MediaBundle<Artist>>> GetArtistsAsync(Guid userId, Guid? collectionId, CancellationToken cancellationToken);

        Task<IEnumerable<MediaBundle<Artist>>> GetArtistsByAlbumAsync(Guid userId, Guid albumId, CancellationToken cancellationToken);

        Task<IEnumerable<MediaBundle<Artist>>> GetArtistsByTrackAsync(Guid userId, Guid trackId, CancellationToken cancellationToken);

        Task<double?> GetAverageRatingAsync(Guid mediaId, CancellationToken cancellationToken);

        Task<IEnumerable<Chat>> GetChatAsync(DateTime? since, CancellationToken cancellationToken);

        Task<IEnumerable<Collection>> GetCollectionsAsync(CancellationToken cancellationToken);

        Task<Disposition> GetDispositionAsync(Guid userId, Guid mediaId, CancellationToken cancellationToken);

        Task<IEnumerable<Disposition>> GetDispositionsAsync(Guid userId, MediaType mediaType, CancellationToken cancellationToken);

        Task<IEnumerable<MediaBundle<Album>>> GetFavoritedAlbumsAsync(Guid userId, int size, int offset, string genre, int? fromYear, int? toYear, Guid? collectionId, bool populate, CancellationToken cancellationToken);

        Task<IEnumerable<MediaBundle<T>>> GetFavoritedAsync<T>(Guid userId, Guid? collectionId, bool populate, CancellationToken cancellationToken) where T : MediaBase, ISearchable, ICollectionIdentifier;

        Task<Genre> GetGenreAsync(string genre, Guid? collectionId, CancellationToken cancellationToken);

        Task<Dictionary<string, Tuple<int, int>>> GetGenreCountsAsync(Guid? collectionId, CancellationToken cancellationToken);

        Task<IEnumerable<Genre>> GetGenresAsync(Guid? collectionId, CancellationToken cancellationToken);

        Task<IEnumerable<Genre>> GetGenresByTrackAsync(Guid trackId, CancellationToken cancellationToken);

        Task<IEnumerable<MediaBundle<Album>>> GetHighestRatedAlbumsAsync(Guid userId, int size, int offset, string genre, int? fromYear, int? toYear, Guid? collectionId, bool populate, CancellationToken cancellationToken);

        Task<IEnumerable<Marker>> GetMarkersAsync(Guid userId, CancellationToken cancellationToken);

        Task<MediaInfo> GetMediaInfoAsync(Guid mediaId, CancellationToken cancellationToken);

        Task<MediaType?> GetMediaTypeAsync(Guid mediaId, CancellationToken cancellationToken);

        Task<IEnumerable<MediaBundle<Album>>> GetMostPlayedAlbumsAsync(Guid userId, int size, int offset, string genre, int? fromYear, int? toYear, Guid? collectionId, bool populate, CancellationToken cancellationToken);

        Task<IEnumerable<MediaBundle<Album>>> GetMostRecentlyPlayedAlbumsAsync(Guid userId, int size, int offset, string genre, int? fromYear, int? toYear, Guid? collectionId, bool populate, CancellationToken cancellationToken);

        Task<IEnumerable<MediaBundle<Album>>> GetNewestAlbumsAsync(Guid userId, int size, int offset, string genre, int? fromYear, int? toYear, Guid? collectionId, bool populate, CancellationToken cancellationToken);

        Task<Playlist> GetPlaylistAsync(Guid userId, Guid id, bool getTracks, CancellationToken cancellationToken);

        Task<List<Playlist>> GetPlaylistsAsync(Guid userId, string username, bool getTracks, CancellationToken cancellationToken);

        Task<PlayQueue> GetPlayQueueAsync(Guid userId, CancellationToken cancellationToken);

        Task<RadioStation> GetRadioStationAsync(Guid id, CancellationToken cancellationToken);

        Task<IEnumerable<RadioStation>> GetRadioStationsAsync(CancellationToken cancellationToken);

        Task<IEnumerable<MediaBundle<Album>>> GetRandomAlbumsAsync(Guid userId, int size, int offset, string genre, int? fromYear, int? toYear, Guid? collectionId, bool populate, CancellationToken cancellationToken);

        Task<IEnumerable<MediaBundle<Track>>> GetRecentPlaybackAsync(Guid userId, bool populate, CancellationToken cancellationToken);

        Task<IEnumerable<Role>> GetRolesForUserAsync(Guid userId, CancellationToken cancellationToken);

        Task<MediaBundle<Track>> GetTrackAsync(Guid userId, string artist, string track, Guid? collectionId, bool populate, CancellationToken cancellationToken);

        Task<MediaBundle<Track>> GetTrackAsync(Guid userId, string path, Guid? collectionId, bool populate, CancellationToken cancellationToken);

        Task<MediaBundle<Track>> GetTrackAsync(Guid userId, Guid id, bool populate, CancellationToken cancellationToken);

        Task<IEnumerable<MediaBundle<Track>>> GetTracksAsync(Guid userId, int size, int offset, string genre, int? fromYear, int? toYear, Guid? collectionId, bool populate, bool randomize, CancellationToken cancellationToken);

        Task<IEnumerable<MediaBundle<Track>>> GetTracksAsync(Guid userId, Guid? collectionId, CancellationToken cancellationToken);

        Task<IEnumerable<MediaBundle<Track>>> GetTracksByAlbumAsync(Guid userId, Guid albumId, bool populate, CancellationToken cancellationToken);

        Task<IEnumerable<MediaBundle<Track>>> GetTracksByGenreAsync(Guid userId, Guid genreId, bool populate, CancellationToken cancellationToken);

        Task<User> GetUserAsync(string username, CancellationToken cancellationToken);

        Task<User> GetUserAsync(Guid userId, CancellationToken cancellationToken);

        Task<IEnumerable<User>> GetUsersAsync(CancellationToken cancellationToken);

        Task InsertOrUpdateAlbumAsync(Album album, CancellationToken cancellationToken);

        Task InsertOrUpdateArtistAsync(Artist artist, CancellationToken cancellationToken);

        Task InsertOrUpdateCollectionAsync(Collection collection, CancellationToken cancellationToken);

        Task InsertOrUpdateDispositionAsync(Disposition disposition, CancellationToken cancellationToken);

        Task InsertOrUpdateFileInfoAsync(Track track, CancellationToken cancellationToken);

        Task InsertOrUpdateGenreAsync(Genre genre, CancellationToken cancellationToken);

        Task InsertOrUpdateMarkerAsync(Marker marker, CancellationToken cancellationToken);

        Task InsertOrUpdateMediaInfoAsync(MediaInfo mediaInfo, CancellationToken cancellationToken);

        Task InsertOrUpdatePlaylistAsync(Playlist playlist, CancellationToken cancellationToken);

        Task InsertOrUpdatePlaylistTrackAsync(Guid playlistId, Guid trackId, int position, CancellationToken cancellationToken);

        Task InsertOrUpdatePlayQueueAsync(PlayQueue playQueue, CancellationToken cancellationToken);

        Task InsertOrUpdatePlayQueueTrackAsync(Guid playQueueId, Guid trackId, int position, CancellationToken cancellationToken);

        Task InsertOrUpdateTrackAsync(Track track, CancellationToken cancellationToken);

        Task InsertOrUpdateUserAsync(User type, CancellationToken cancellationToken);

        Task InsertPlaybackAsync(Playback playback, CancellationToken cancellationToken);

        Task RemoveCollectionAsync(Collection collection, CancellationToken cancellationToken);

        Task RemoveUserAsync(User user, CancellationToken cancellationToken);

        Task<IEnumerable<MediaBundle<T>>> SearchAsync<T>(Guid userId, string query, int size, int offset, Guid? collectionId, bool populate, CancellationToken cancellationToken) where T : MediaBase, ISearchable, ICollectionIdentifier;

        Task SetDispositionAsync(Disposition disposition, CancellationToken cancellationToken);

        Task UpdatePlaylistAsync(Playlist playlist, CancellationToken cancellationToken);

        Task UpdatePlayQueueAsync(PlayQueue playQueue, CancellationToken cancellationToken);

        Task UpdateRadioStationAsync(RadioStation radioStation, CancellationToken cancellationToken);
    }
}