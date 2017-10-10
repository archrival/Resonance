using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Resonance.Common;
using Resonance.Common.Web;
using Resonance.Data.Media.Audio;
using Resonance.Data.Models;
using Resonance.Data.Storage;
using Resonance.Data.Storage.Common;
using Subsonic.Common.Classes;
using Subsonic.Common.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Resonance.SubsonicCompat.Controllers
{
    [Route("subsonic/rest")]
    public class SubsonicRestController : ResonanceControllerBase
    {
        private static readonly HttpClient HttpClient = new HttpClient();
        private static readonly Regex IndexRegex = new Regex("[a-zA-Z]");
        private readonly SubsonicAuthorization _subsonicAuthorization;
        private readonly Transcode _transcode;

        public SubsonicRestController(IMediaLibrary mediaLibrary, IMetadataRepository metadataRepository, ISettingsRepository settingsRepository) : base(mediaLibrary, metadataRepository, settingsRepository)
        {
            _subsonicAuthorization = new SubsonicAuthorization(metadataRepository);
            _transcode = new Transcode();
        }

        [HttpGet("addChatMessage.view"), HttpPost("addChatMessage.view")]
        [ServiceFilter(typeof(SubsonicAsyncAuthorizationFilter))]
        [ServiceFilter(typeof(SubsonicAsyncResultFilter))]
        public async Task<Response> AddChatMessageAsync([ResonanceParameter] string message, CancellationToken cancellationToken)
        {
            var authorizationContext = ControllerContext.GetAuthorizationContext();

            var chat = new Chat
            {
                Timestamp = DateTime.UtcNow,
                User = authorizationContext.User,
                Message = message
            };

            await MetadataRepository.AddChatAsync(chat, cancellationToken).ConfigureAwait(false);

            return SubsonicControllerExtensions.CreateResponse();
        }

        [HttpGet("changePassword.view"), HttpPost("changePassword.view")]
        [ServiceFilter(typeof(SubsonicAsyncAuthorizationFilter))]
        [ServiceFilter(typeof(SubsonicAsyncResultFilter))]
        [Authorize(Policy = PolicyConstants.ModifyUserSettings)]
        public async Task<Response> ChangePasswordAsync([ResonanceParameter] string username, [ResonanceParameter] string password, CancellationToken cancellationToken)
        {
            var authorizationContext = ControllerContext.GetAuthorizationContext();

            Data.Models.User user;

            if (username != authorizationContext.User.Name)
            {
                if (!authorizationContext.Roles.Contains(Role.Administrator))
                {
                    authorizationContext.ErrorCode = (int)ErrorCode.UserNotAuthorized;
                    authorizationContext.Status = SubsonicConstants.UserIsNotAuthorizedForTheGivenOperation;

                    return authorizationContext.CreateAuthorizationFailureResponse();
                }

                user = await MetadataRepository.GetUserAsync(username, cancellationToken).ConfigureAwait(false);

                if (user == null)
                {
                    return new AuthorizationContext
                    {
                        ErrorCode = (int)ErrorCode.GenericError,
                        Status = SubsonicConstants.UserDoesNotExist
                    }.CreateAuthorizationFailureResponse();
                }

                cancellationToken.ThrowIfCancellationRequested();

                if (user.Roles == null || !user.Roles.Any())
                {
                    user.Roles = await MetadataRepository.GetRolesForUserAsync(user.Id, cancellationToken).ConfigureAwait(false);
                }
            }
            else
            {
                user = authorizationContext.User;
                user.Roles = authorizationContext.Roles;
            }
            
            user.Password = SubsonicControllerExtensions.ParsePassword(password).EncryptString(Constants.ResonanceKey);

            await MetadataRepository.InsertOrUpdateUserAsync(user, cancellationToken).ConfigureAwait(false);

            return SubsonicControllerExtensions.CreateResponse();
        }

        [HttpGet("createBookmark.view"), HttpPost("createBookmark.view")]
        [ServiceFilter(typeof(SubsonicAsyncAuthorizationFilter))]
        [ServiceFilter(typeof(SubsonicAsyncResultFilter))]
        public async Task<Response> CreateBookmarkAsync([ResonanceParameter] Guid id, [ResonanceParameter] long position, [ResonanceParameter] string comment, CancellationToken cancellationToken)
        {
            var authorizationContext = ControllerContext.GetAuthorizationContext();

            var marker = new Marker
            {
                TrackId = id,
                User = authorizationContext.User,
                Position = position,
                Comment = comment
            };

            await MetadataRepository.InsertOrUpdateMarkerAsync(marker, cancellationToken).ConfigureAwait(false);

            return SubsonicControllerExtensions.CreateResponse();
        }

        [HttpGet("createPlaylist.view"), HttpPost("createPlaylist.view")]
        [ServiceFilter(typeof(SubsonicAsyncAuthorizationFilter))]
        [ServiceFilter(typeof(SubsonicAsyncResultFilter))]
        public async Task<Response> CreatePlaylistAsync([ResonanceParameter] Guid? playlistId, [ResonanceParameter] string name, [ResonanceParameter(Name = "songId")] List<Guid> songIds, CancellationToken cancellationToken)
        {
            var authorizationContext = ControllerContext.GetAuthorizationContext();

            var userId = authorizationContext.User.Id;

            Data.Models.Playlist playlist;

            if (playlistId.HasValue)
            {
                // Update existing playlist
                playlist = await MetadataRepository.GetPlaylistAsync(userId, playlistId.Value, true, cancellationToken).ConfigureAwait(false);

                if (playlist == null)
                {
                    return SubsonicControllerExtensions.CreateFailureResponse(ErrorCode.RequestedDataNotFound, SubsonicConstants.PlaylistNotFound);
                }

                // Override playlist name if one is provided
                if (!string.IsNullOrWhiteSpace(name))
                {
                    playlist.Name = name;
                }

                // Override tracks if any are provided
                if (songIds.Count > 0)
                {
                    playlist.Tracks = new List<MediaBundle<Track>>();

                    foreach (var songId in songIds)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var track = await MediaLibrary.GetTrackAsync(userId, songId, false, cancellationToken).ConfigureAwait(false);

                        if (track != null)
                        {
                            playlist.Tracks.Add(track);
                        }
                    }
                }
            }
            else
            {
                // Playlist name is required when creating a playlist
                if (string.IsNullOrWhiteSpace(name))
                {
                    return SubsonicControllerExtensions.CreateFailureResponse(ErrorCode.RequiredParameterMissing, SubsonicConstants.RequiredParameterIsMissing);
                }

                // Tracks are required when creating a playlist
                if (songIds.Count == 0)
                {
                    return SubsonicControllerExtensions.CreateFailureResponse(ErrorCode.RequiredParameterMissing, SubsonicConstants.RequiredParameterIsMissing);
                }

                playlist = new Data.Models.Playlist
                {
                    Name = name,
                    User = authorizationContext.User,
                    Accessibility = Accessibility.Private,
                    Tracks = new List<MediaBundle<Track>>()
                };

                foreach (var songId in songIds)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var track = await MediaLibrary.GetTrackAsync(userId, songId, false, cancellationToken).ConfigureAwait(false);

                    if (track != null)
                    {
                        playlist.Tracks.Add(track);
                    }
                }
            }

            MediaLibrary.RemovePlaylistFromCache(userId, playlist.Id, true);

            await MetadataRepository.UpdatePlaylistAsync(playlist, cancellationToken).ConfigureAwait(false);

            playlist = await MediaLibrary.GetPlaylistAsync(userId, playlist.Id, true, cancellationToken).ConfigureAwait(false);

            var subsonicPlaylist = playlist.ToSubsonicPlaylistWithSongs();

            if (playlist?.Tracks?.Any() == true)
            {
                subsonicPlaylist.Entries = new List<Child>();

                foreach (var track in playlist.Tracks)
                {
                    subsonicPlaylist.Entries.Add(track.ToSubsonicSong(await MediaLibrary.GetAlbumAsync(userId, track.Media.AlbumId, false, cancellationToken).ConfigureAwait(false)));
                }
            }

            return SubsonicControllerExtensions.CreateResponse(ItemChoiceType.Playlist, subsonicPlaylist);
        }

        [HttpGet("createUser.view"), HttpPost("createUser.view")]
        [ServiceFilter(typeof(SubsonicAsyncAuthorizationFilter))]
        [ServiceFilter(typeof(SubsonicAsyncResultFilter))]
        [Authorize(Policy = PolicyConstants.Administration)]
        public async Task<Response> CreateUserAsync([ResonanceParameter] string username, [ResonanceParameter] string password, [ResonanceParameter] string email, [ResonanceParameter] bool? adminRole, [ResonanceParameter] bool? settingsRole, [ResonanceParameter] bool? streamRole, [ResonanceParameter] bool? downloadRole, [ResonanceParameter(Name = "musicFolderId")] List<int> musicFolderIds, CancellationToken cancellationToken)
        {
            var user = await MetadataRepository.GetUserAsync(username, cancellationToken).ConfigureAwait(false);

            if (user != null)
            {
                return new AuthorizationContext
                {
                    ErrorCode = (int)ErrorCode.GenericError,
                    Status = SubsonicConstants.UserAlreadyExists
                }.CreateAuthorizationFailureResponse();
            }

            user = new Data.Models.User
            {
                Enabled = true,
                EmailAddress = email,
                Name = username,
                Password = SubsonicControllerExtensions.ParsePassword(password).EncryptString(Constants.ResonanceKey)
            };

            var roles = new List<Role>();

            if (adminRole.GetValueOrDefault())
            {
                roles.Add(Role.Administrator);
            }

            if (settingsRole.GetValueOrDefault())
            {
                roles.Add(Role.Settings);
            }

            if (streamRole.GetValueOrDefault())
            {
                roles.Add(Role.Playback);
            }

            if (downloadRole.GetValueOrDefault())
            {
                roles.Add(Role.Download);
            }

            user.Roles = roles;

            await MetadataRepository.InsertOrUpdateUserAsync(user, cancellationToken).ConfigureAwait(false);

            return SubsonicControllerExtensions.CreateResponse();
        }

        [HttpGet("deleteBookmark.view"), HttpPost("deleteBookmark.view")]
        [ServiceFilter(typeof(SubsonicAsyncAuthorizationFilter))]
        [ServiceFilter(typeof(SubsonicAsyncResultFilter))]
        public async Task<Response> DeleteBookmarkAsync([ResonanceParameter] Guid id, CancellationToken cancellationToken)
        {
            var userId = ControllerContext.GetAuthorizationContext().User.Id;

            await MetadataRepository.DeleteMarkerAsync(userId, id, cancellationToken).ConfigureAwait(false);

            return SubsonicControllerExtensions.CreateResponse();
        }

        [HttpGet("deletePlaylist.view"), HttpPost("deletePlaylist.view")]
        [ServiceFilter(typeof(SubsonicAsyncAuthorizationFilter))]
        [ServiceFilter(typeof(SubsonicAsyncResultFilter))]
        public async Task<Response> DeletePlaylistAsync([ResonanceParameter] Guid id, CancellationToken cancellationToken)
        {
            var userId = ControllerContext.GetAuthorizationContext().User.Id;

            await MetadataRepository.DeletePlaylistAsync(userId, id, cancellationToken).ConfigureAwait(false);

            return SubsonicControllerExtensions.CreateResponse();
        }

        [HttpGet("deleteUser.view"), HttpPost("deleteUser.view")]
        [ServiceFilter(typeof(SubsonicAsyncAuthorizationFilter))]
        [ServiceFilter(typeof(SubsonicAsyncResultFilter))]
        [Authorize(Policy = PolicyConstants.Administration)]
        public async Task<Response> DeleteUserAsync([ResonanceParameter] string username, CancellationToken cancellationToken)
        {
            var user = await MetadataRepository.GetUserAsync(username, cancellationToken).ConfigureAwait(false);

            if (user == null)
            {
                return new AuthorizationContext
                {
                    ErrorCode = (int)ErrorCode.GenericError,
                    Status = SubsonicConstants.UserDoesNotExist
                }.CreateAuthorizationFailureResponse();
            }

            await MetadataRepository.DeleteUserAsync(user.Id, cancellationToken).ConfigureAwait(false);

            return SubsonicControllerExtensions.CreateResponse();
        }

        [HttpGet("download.view"), HttpPost("download.view")]
        [ServiceFilter(typeof(SubsonicAsyncAuthorizationFilter))]
        [Authorize(Policy = PolicyConstants.Stream)]

        public async Task<IActionResult> DownloadAsync([ResonanceParameter] Guid id, CancellationToken cancellationToken)
        {
            var userId = ControllerContext.GetAuthorizationContext().User.Id;

            var mediaType = await MetadataRepository.GetMediaTypeAsync(id, cancellationToken).ConfigureAwait(false);

            if (!mediaType.HasValue)
            {
                return StatusCode(404);
            }

            switch (mediaType)
            {
                case Data.Models.MediaType.Album:
                    var albumMediaBundle = await MediaLibrary.GetAlbumAsync(userId, id, true, cancellationToken).ConfigureAwait(false);

                    cancellationToken.ThrowIfCancellationRequested();

                    if (albumMediaBundle != null)
                    {
                        var artist = albumMediaBundle.Media.Artists.FirstOrDefault();

                        string artistName = null;

                        if (artist != null)
                        {
                            artistName = $"{artist.Media.Name}-";
                        }

                        var zipFileName = $"{artistName}{albumMediaBundle.Media.Name}.zip";

                        return File(CompressionExtensions.CompressFiles(albumMediaBundle.Media.Tracks.Select(t => t.Media.Path), CompressionLevel.NoCompression), "application/zip", zipFileName);
                    }
                    break;

                case Data.Models.MediaType.Track:
                    var trackMediaBundle = await MediaLibrary.GetTrackAsync(userId, id, false, cancellationToken).ConfigureAwait(false);

                    cancellationToken.ThrowIfCancellationRequested();

                    if (trackMediaBundle != null)
                    {
                        var track = trackMediaBundle.Media;
                        var path = track.Path;

                        return File(System.IO.File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read), MimeType.GetMimeType(path), Path.GetFileName(path));
                    }
                    break;
            }

            return StatusCode(404);
        }

        [HttpGet("getAlbum.view"), HttpPost("getAlbum.view")]
        [ServiceFilter(typeof(SubsonicAsyncAuthorizationFilter))]
        [ServiceFilter(typeof(SubsonicAsyncResultFilter))]
        public async Task<Response> GetAlbumAsync([ResonanceParameter] Guid id, CancellationToken cancellationToken)
        {
            var userId = ControllerContext.GetAuthorizationContext().User.Id;

            var album = await MediaLibrary.GetAlbumAsync(userId, id, true, cancellationToken).ConfigureAwait(false);

            if (album == null)
            {
                return SubsonicControllerExtensions.CreateFailureResponse(ErrorCode.RequestedDataNotFound, SubsonicConstants.AlbumNotFound);
            }

            var subsonicAlbum = album.ToSubsonicAlbumWithSongsID3();

            return SubsonicControllerExtensions.CreateResponse(ItemChoiceType.Album, subsonicAlbum);
        }

        [HttpGet("getAlbumInfo2.view"), HttpPost("getAlbumInfo2.view")]
        [ServiceFilter(typeof(SubsonicAsyncAuthorizationFilter))]
        [ServiceFilter(typeof(SubsonicAsyncResultFilter))]
        public async Task<Response> GetAlbumInfo2Async([ResonanceParameter] Guid id, CancellationToken cancellationToken)
        {
            var userId = ControllerContext.GetAuthorizationContext().User.Id;

            var albumInfo = new AlbumInfo();

            var album = await MediaLibrary.GetAlbumAsync(userId, id, true, cancellationToken).ConfigureAwait(false);

            if (album == null)
            {
                return SubsonicControllerExtensions.CreateFailureResponse(ErrorCode.RequestedDataNotFound, SubsonicConstants.MediaFileNotFound);
            }

            cancellationToken.ThrowIfCancellationRequested();

            var mediaInfo = await MediaLibrary.GetAlbumInfoAsync(album.Media, cancellationToken).ConfigureAwait(false);

            if (mediaInfo != null)
            {
                albumInfo.MusicBrainzId = mediaInfo.MusicBrainzId;

                if (mediaInfo.LastFm != null)
                {
                    albumInfo.Notes = mediaInfo.LastFm?.Details;
                    albumInfo.LargeImageUrl = mediaInfo.LastFm?.LargeImageUrl?.ToString();
                    albumInfo.LastFmUrl = mediaInfo.LastFm?.Url?.ToString();
                    albumInfo.MediumImageUrl = mediaInfo.LastFm?.MediumImageUrl?.ToString();
                    albumInfo.SmallImageUrl = mediaInfo.LastFm?.SmallImageUrl?.ToString();
                }
            }

            return SubsonicControllerExtensions.CreateResponse(ItemChoiceType.AlbumInfo2, albumInfo);
        }

        [HttpGet("getAlbumInfo.view"), HttpPost("getAlbumInfo.view")]
        [ServiceFilter(typeof(SubsonicAsyncAuthorizationFilter))]
        [ServiceFilter(typeof(SubsonicAsyncResultFilter))]
        public async Task<Response> GetAlbumInfoAsync([ResonanceParameter] Guid id, CancellationToken cancellationToken)
        {
            var userId = ControllerContext.GetAuthorizationContext().User.Id;

            var albumInfo = new AlbumInfo();

            var album = await MediaLibrary.GetAlbumAsync(userId, id, true, cancellationToken).ConfigureAwait(false);

            if (album == null)
            {
                return SubsonicControllerExtensions.CreateFailureResponse(ErrorCode.RequestedDataNotFound, SubsonicConstants.MediaFileNotFound);
            }

            cancellationToken.ThrowIfCancellationRequested();

            var mediaInfo = await MediaLibrary.GetAlbumInfoAsync(album.Media, cancellationToken).ConfigureAwait(false);

            if (mediaInfo != null)
            {
                albumInfo.MusicBrainzId = mediaInfo.MusicBrainzId;

                if (mediaInfo.LastFm != null)
                {
                    albumInfo.Notes = mediaInfo.LastFm?.Details;
                    albumInfo.LargeImageUrl = mediaInfo.LastFm?.LargeImageUrl?.ToString();
                    albumInfo.LastFmUrl = mediaInfo.LastFm?.Url?.ToString();
                    albumInfo.MediumImageUrl = mediaInfo.LastFm?.MediumImageUrl?.ToString();
                    albumInfo.SmallImageUrl = mediaInfo.LastFm?.SmallImageUrl?.ToString();
                }
            }

            return SubsonicControllerExtensions.CreateResponse(ItemChoiceType.AlbumInfo, albumInfo);
        }

        [HttpGet("getAlbumList2.view"), HttpPost("getAlbumList2.view")]
        [ServiceFilter(typeof(SubsonicAsyncAuthorizationFilter))]
        [ServiceFilter(typeof(SubsonicAsyncResultFilter))]
        public async Task<Response> GetAlbumList2Async([ResonanceParameter] AlbumListType type, [ResonanceParameter] int? size, [ResonanceParameter] int? offset, [ResonanceParameter] int? fromYear, [ResonanceParameter] int? toYear, [ResonanceParameter] string genre, [ResonanceParameter] Guid? musicFolderId, CancellationToken cancellationToken)
        {
            var userId = ControllerContext.GetAuthorizationContext().User.Id;

            size = SetBounds(size, 10, 500);

            switch (type)
            {
                case AlbumListType.ByYear:
                    if (!fromYear.HasValue || !toYear.HasValue)
                    {
                        return SubsonicControllerExtensions.CreateFailureResponse(ErrorCode.RequiredParameterMissing, SubsonicConstants.RequiredParameterIsMissing);
                    }
                    break;

                case AlbumListType.ByGenre:
                    if (string.IsNullOrWhiteSpace(genre))
                    {
                        return SubsonicControllerExtensions.CreateFailureResponse(ErrorCode.RequiredParameterMissing, SubsonicConstants.RequiredParameterIsMissing);
                    }
                    break;
            }

            var albumMediaBundles = await GetAlbumListInternalAsync(userId, type, size, offset, fromYear, toYear, genre, musicFolderId, cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            var children = albumMediaBundles.Select(albumMediaBundle => albumMediaBundle.ToSubsonicAlbumID3()).ToList();

            var albumList2 = new AlbumList2
            {
                Albums = children
            };

            return SubsonicControllerExtensions.CreateResponse(ItemChoiceType.AlbumList2, albumList2);
        }

        [HttpGet("getAlbumList.view"), HttpPost("getAlbumList.view")]
        [ServiceFilter(typeof(SubsonicAsyncAuthorizationFilter))]
        [ServiceFilter(typeof(SubsonicAsyncResultFilter))]
        public async Task<Response> GetAlbumListAsync([ResonanceParameter] AlbumListType type, [ResonanceParameter] int? size, [ResonanceParameter] int? offset, [ResonanceParameter] int? fromYear, [ResonanceParameter] int? toYear, [ResonanceParameter] string genre, [ResonanceParameter] Guid? musicFolderId, CancellationToken cancellationToken)
        {
            var userId = ControllerContext.GetAuthorizationContext().User.Id;

            size = SetBounds(size, 10, 500);

            switch (type)
            {
                case AlbumListType.ByYear:
                    if (!fromYear.HasValue || !toYear.HasValue)
                    {
                        return SubsonicControllerExtensions.CreateFailureResponse(ErrorCode.RequiredParameterMissing, SubsonicConstants.RequiredParameterIsMissing);
                    }
                    break;

                case AlbumListType.ByGenre:
                    if (string.IsNullOrWhiteSpace(genre))
                    {
                        return SubsonicControllerExtensions.CreateFailureResponse(ErrorCode.RequiredParameterMissing, SubsonicConstants.RequiredParameterIsMissing);
                    }
                    break;
            }

            var albumMediaBundles = await GetAlbumListInternalAsync(userId, type, size, offset, fromYear, toYear, genre, musicFolderId, cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            var children = albumMediaBundles.Select(albumMediaBundle => albumMediaBundle.ToSubsonicChild()).ToList();

            var albumList = new AlbumList
            {
                Albums = children
            };

            return SubsonicControllerExtensions.CreateResponse(ItemChoiceType.AlbumList, albumList);
        }

        [HttpGet("getArtist.view"), HttpPost("getArtist.view")]
        [ServiceFilter(typeof(SubsonicAsyncAuthorizationFilter))]
        [ServiceFilter(typeof(SubsonicAsyncResultFilter))]
        public async Task<Response> GetArtistAsync([ResonanceParameter] Guid id, CancellationToken cancellationToken)
        {
            var userId = ControllerContext.GetAuthorizationContext().User.Id;

            var artist = await MediaLibrary.GetArtistAsync(userId, id, cancellationToken).ConfigureAwait(false);

            if (artist == null)
            {
                return SubsonicControllerExtensions.CreateFailureResponse(ErrorCode.RequestedDataNotFound, SubsonicConstants.ArtistNotFound);
            }

            var albumMediaBundles = await MediaLibrary.GetAlbumsByArtistAsync(userId, id, true, cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            var subsonicArtist = artist.ToSubsonicArtistWithAlbumsID3(albumMediaBundles);

            return SubsonicControllerExtensions.CreateResponse(ItemChoiceType.Artist, subsonicArtist);
        }

        [HttpGet("getArtistInfo2.view"), HttpPost("getArtistInfo2.view")]
        [ServiceFilter(typeof(SubsonicAsyncAuthorizationFilter))]
        [ServiceFilter(typeof(SubsonicAsyncResultFilter))]
        public async Task<Response> GetArtistInfo2Async([ResonanceParameter] Guid id, [ResonanceParameter] int? count, [ResonanceParameter] bool includeNotPresent, CancellationToken cancellationToken)
        {
            var userId = ControllerContext.GetAuthorizationContext().User.Id;

            count = SetBounds(count, 20, 500);

            var artistInfo = new ArtistInfo2();

            var artist = await MediaLibrary.GetArtistAsync(userId, id, cancellationToken).ConfigureAwait(false);

            if (artist == null)
            {
                return SubsonicControllerExtensions.CreateFailureResponse(ErrorCode.RequestedDataNotFound, SubsonicConstants.MediaFileNotFound);
            }

            cancellationToken.ThrowIfCancellationRequested();

            var mediaInfo = await MediaLibrary.GetArtistInfoAsync(artist.Media, cancellationToken).ConfigureAwait(false);

            if (mediaInfo != null)
            {
                artistInfo.MusicBrainzId = mediaInfo.MusicBrainzId;

                if (mediaInfo.LastFm != null)
                {
                    artistInfo.Biography = mediaInfo.LastFm?.Details;
                    artistInfo.LargeImageUrl = mediaInfo.LastFm?.LargeImageUrl?.ToString();
                    artistInfo.LastFmUrl = mediaInfo.LastFm?.Url?.ToString();
                    artistInfo.MediumImageUrl = mediaInfo.LastFm?.MediumImageUrl?.ToString();
                    artistInfo.SmallImageUrl = mediaInfo.LastFm?.SmallImageUrl?.ToString();
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            var similarArtists = await MediaLibrary.GetSimilarArtistsAsync(userId, artist.Media, true, count.Value, artist.Media.CollectionId, cancellationToken).ConfigureAwait(false);

            var subsonicSimilarArtists = new List<ArtistID3>();

            foreach (var similarArtist in similarArtists.Where(sa => sa.MediaId != Guid.Empty))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var artistModel = await MediaLibrary.GetArtistAsync(userId, similarArtist.MediaId, cancellationToken).ConfigureAwait(false);
                subsonicSimilarArtists.Add(artistModel.ToSubsonicArtistID3());
            }

            artistInfo.SimilarArtists = subsonicSimilarArtists;

            return SubsonicControllerExtensions.CreateResponse(ItemChoiceType.ArtistInfo2, artistInfo);
        }

        [HttpGet("getArtistInfo.view"), HttpPost("getArtistInfo.view")]
        [ServiceFilter(typeof(SubsonicAsyncAuthorizationFilter))]
        [ServiceFilter(typeof(SubsonicAsyncResultFilter))]
        public async Task<Response> GetArtistInfoAsync([ResonanceParameter] Guid id, [ResonanceParameter] int? count, [ResonanceParameter] bool includeNotPresent, CancellationToken cancellationToken)
        {
            var userId = ControllerContext.GetAuthorizationContext().User.Id;

            count = SetBounds(count, 20, 500);

            var artistInfo = new ArtistInfo();

            var artist = await MediaLibrary.GetArtistAsync(userId, id, cancellationToken).ConfigureAwait(false);

            if (artist == null)
            {
                return SubsonicControllerExtensions.CreateFailureResponse(ErrorCode.RequestedDataNotFound, SubsonicConstants.MediaFileNotFound);
            }

            cancellationToken.ThrowIfCancellationRequested();

            var mediaInfo = await MediaLibrary.GetArtistInfoAsync(artist.Media, cancellationToken).ConfigureAwait(false);

            if (mediaInfo != null)
            {
                artistInfo.MusicBrainzId = mediaInfo.MusicBrainzId;

                if (mediaInfo.LastFm != null)
                {
                    artistInfo.Biography = mediaInfo.LastFm?.Details;
                    artistInfo.LargeImageUrl = mediaInfo.LastFm?.LargeImageUrl?.ToString();
                    artistInfo.LastFmUrl = mediaInfo.LastFm?.Url?.ToString();
                    artistInfo.MediumImageUrl = mediaInfo.LastFm?.MediumImageUrl?.ToString();
                    artistInfo.SmallImageUrl = mediaInfo.LastFm?.SmallImageUrl?.ToString();
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            var similarArtists = await MediaLibrary.GetSimilarArtistsAsync(userId, artist.Media, true, count.Value, artist.Media.CollectionId, cancellationToken).ConfigureAwait(false);

            var subsonicSimilarArtists = new List<Subsonic.Common.Classes.Artist>();

            foreach (var similarArtist in similarArtists.Where(sa => sa.MediaId != Guid.Empty))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var artistModel = await MediaLibrary.GetArtistAsync(userId, similarArtist.MediaId, cancellationToken).ConfigureAwait(false);
                subsonicSimilarArtists.Add(artistModel.ToSubsonicArtist());
            }

            artistInfo.SimilarArtists = subsonicSimilarArtists;

            return SubsonicControllerExtensions.CreateResponse(ItemChoiceType.ArtistInfo, artistInfo);
        }

        [HttpGet("getArtists.view"), HttpPost("getArtists.view")]
        [ServiceFilter(typeof(SubsonicAsyncAuthorizationFilter))]
        [ServiceFilter(typeof(SubsonicAsyncResultFilter))]
        public async Task<Response> GetArtistsAsync([ResonanceParameter(Name = "musicFolderId")] Guid? collectionId, CancellationToken cancellationToken)
        {
            var userId = ControllerContext.GetAuthorizationContext().User.Id;

            var artists = await MediaLibrary.GetArtistsAsync(userId, collectionId, cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            var subsonicArtists = new ArtistsID3 { Indexes = new List<IndexID3>() };

            var indexDictionary = new Dictionary<char, List<ArtistID3>>();

            foreach (var artist in artists)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var subsonicArtist = artist.ToSubsonicArtistID3();

                var firstChar = artist.Media.Name.ToUpperInvariant().First();
                var indexKey = firstChar;

                if (!IndexRegex.IsMatch(firstChar.ToString()))
                {
                    indexKey = '#';
                }

                if (indexDictionary.ContainsKey(indexKey))
                {
                    indexDictionary[indexKey].Add(subsonicArtist);
                }
                else
                {
                    indexDictionary[indexKey] = new List<ArtistID3> { subsonicArtist };
                }
            }

            foreach (var key in indexDictionary.Keys.OrderBy(k => k))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var index = new IndexID3
                {
                    Name = key.ToString(),
                    Artists = indexDictionary[key].OrderBy(k => k.Name).ToList()
                };

                subsonicArtists.Indexes.Add(index);
            }

            return SubsonicControllerExtensions.CreateResponse(ItemChoiceType.Artists, subsonicArtists);
        }

        [HttpGet("getAvatar.view"), HttpPost("getAvatar.view")]
        public async Task<ActionResult> GetAvatarAsync([ResonanceParameter] string user, CancellationToken cancellationToken)
        {
            var queryParameters = Request.GetSubsonicQueryParameters();

            var authorizationContext = await _subsonicAuthorization.AuthorizeRequestAsync(queryParameters, cancellationToken).ConfigureAwait(false);

            return StatusCode(!authorizationContext.IsAuthenticated ? 401 : 204);
        }

        [HttpGet("getBookmarks.view"), HttpPost("getBookmarks.view")]
        [ServiceFilter(typeof(SubsonicAsyncAuthorizationFilter))]
        [ServiceFilter(typeof(SubsonicAsyncResultFilter))]
        public async Task<Response> GetBookmarksAsync(CancellationToken cancellationToken)
        {
            var userId = ControllerContext.GetAuthorizationContext().User.Id;

            var markers = await MetadataRepository.GetMarkersAsync(userId, cancellationToken).ConfigureAwait(false);

            var subsonicBookmarks = await markers.ToSubsonicBookmarksAsync(MediaLibrary, cancellationToken).ConfigureAwait(false);

            return SubsonicControllerExtensions.CreateResponse(ItemChoiceType.Bookmarks, subsonicBookmarks);
        }

        [HttpGet("getChatMessages.view"), HttpPost("getChatMessages.view")]
        [ServiceFilter(typeof(SubsonicAsyncAuthorizationFilter))]
        [ServiceFilter(typeof(SubsonicAsyncResultFilter))]
        public async Task<Response> GetChatMessagesAsync([ResonanceParameter] long? since, CancellationToken cancellationToken)
        {
            DateTime? dateTimeSince = null;

            if (since.HasValue)
            {
                dateTimeSince = DateTimeExtensions.DateTimeFromUnixTimestampMilliseconds(since.Value);
            }

            var messages = await MetadataRepository.GetChatAsync(dateTimeSince, cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            var subsonicChatMessages = new ChatMessages { Items = new List<ChatMessage>() };

            foreach (var message in messages)
            {
                cancellationToken.ThrowIfCancellationRequested();

                subsonicChatMessages.Items.Add(message.ToSubsonicChatMessage());
            }

            return SubsonicControllerExtensions.CreateResponse(ItemChoiceType.ChatMessages, subsonicChatMessages);
        }

        [HttpGet("getCoverArt.view"), HttpPost("getCoverArt.view")]
        public async Task<IActionResult> GetCoverArtAsync([ResonanceParameter] string id, [ResonanceParameter] int? size, CancellationToken cancellationToken)
        {
            var queryParameters = Request.GetSubsonicQueryParameters();

            var authorizationContext = await _subsonicAuthorization.AuthorizeRequestAsync(queryParameters, cancellationToken).ConfigureAwait(false);

            if (!authorizationContext.IsAuthenticated)
            {
                return StatusCode(401);
            }

            var userId = authorizationContext.User.Id;
            byte[] coverArtData = null;
            string contentType = null;

            Data.Models.MediaType? mediaType = null;

            if (Guid.TryParse(id, out var mediaId))
            {
                mediaType = await MetadataRepository.GetMediaTypeAsync(mediaId, cancellationToken);
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (id.StartsWith("pl-"))
            {
                var playlistId = new Guid(id.Remove(0, 3));

                var playlist = await MediaLibrary.GetPlaylistAsync(userId, playlistId, true, cancellationToken).ConfigureAwait(false);

                if (playlist == null)
                {
                    return StatusCode(404);
                }

                var random = new Random();

                id = playlist.Tracks[random.Next(0, playlist.Tracks.Count - 1)].Media.Id.ToString("n");
            }
            else if (id.StartsWith("al-") || mediaType == Data.Models.MediaType.Album)
            {
                var albumId = new Guid(id.Replace("al-", string.Empty));

                var album = await MediaLibrary.GetAlbumAsync(userId, albumId, true, cancellationToken).ConfigureAwait(false);

                if (album == null)
                {
                    return StatusCode(404);
                }

                id = album.Media.Tracks.FirstOrDefault().Media.Id.ToString("n");
            }
            else if (id.StartsWith("ar-") || mediaType == Data.Models.MediaType.Artist)
            {
                var artistId = new Guid(id.Replace("ar-", string.Empty));

                var artist = await MediaLibrary.GetArtistAsync(userId, artistId, cancellationToken).ConfigureAwait(false);

                if (artist == null)
                {
                    return StatusCode(404);
                }

                var artistInfo = await MediaLibrary.GetArtistInfoAsync(artist.Media, cancellationToken);

                if (artistInfo?.LastFm?.LargestImageUrl != null)
                {
                    coverArtData = await HttpClient.GetByteArrayAsync(artistInfo.LastFm.LargestImageUrl);

                    if (coverArtData == null)
                    {
                        return StatusCode(404);
                    }

                    contentType = MimeType.GetMimeType(coverArtData, Path.GetFileName(artistInfo.LastFm.LargestImageUrl.ToString()));
                }
                else
                {
                    return StatusCode(404);
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (coverArtData == null)
            {
                var trackId = new Guid(id);

                var coverArt = await MediaLibrary.GetCoverArtAsync(userId, trackId, size, cancellationToken).ConfigureAwait(false);

                if (coverArt == null)
                {
                    return new StatusCodeResult(404);
                }

                coverArtData = coverArt.CoverArtData;
                contentType = coverArt.MimeType;
            }

            return File(coverArtData, contentType);
        }

        [HttpGet("getGenres.view"), HttpPost("getGenres.view")]
        [ServiceFilter(typeof(SubsonicAsyncAuthorizationFilter))]
        [ServiceFilter(typeof(SubsonicAsyncResultFilter))]
        public async Task<Response> GetGenresAsync(CancellationToken cancellationToken)
        {
            var genres = await MediaLibrary.GetGenresAsync(null, cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            var genreCounts = await MediaLibrary.GetGenreCountsAsync(null, cancellationToken).ConfigureAwait(false);

            var subsonicGenres = new Genres { Items = genres.Select(g => g.ToSubsonicGenre(genreCounts[g.Name].Item2, genreCounts[g.Name].Item1)).ToList() };

            return SubsonicControllerExtensions.CreateResponse(ItemChoiceType.Genres, subsonicGenres);
        }

        [HttpGet("getIndexes.view"), HttpPost("getIndexes.view")]
        [ServiceFilter(typeof(SubsonicAsyncAuthorizationFilter))]
        [ServiceFilter(typeof(SubsonicAsyncResultFilter))]
        public async Task<Response> GetIndexesAsync([ResonanceParameter] Guid? musicFolderId, [ResonanceParameter] long? ifModifiedSince, CancellationToken cancellationToken)
        {
            var userId = ControllerContext.GetAuthorizationContext().User.Id;

            var artists = await MediaLibrary.GetArtistsAsync(userId, musicFolderId, cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            var indexDictionary = new Dictionary<char, List<Subsonic.Common.Classes.Artist>>();

            var indexes = new Indexes { Items = new List<Index>() };

            if (ifModifiedSince.HasValue)
            {
                var dateTime = DateTimeExtensions.DateTimeFromUnixTimestampMilliseconds(ifModifiedSince.Value);

                if (!artists.All(a => (a.Media.DateModified ?? a.Media.DateAdded) > dateTime))
                {
                    return SubsonicControllerExtensions.CreateResponse(ItemChoiceType.Indexes, indexes);
                }
            }

            foreach (var artist in artists)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var subsonicArtist = artist.ToSubsonicArtist();

                var firstChar = artist.Media.Name.ToUpperInvariant().First();
                var indexKey = firstChar;

                if (!IndexRegex.IsMatch(firstChar.ToString()))
                {
                    indexKey = '#';
                }

                if (indexDictionary.ContainsKey(indexKey))
                {
                    indexDictionary[indexKey].Add(subsonicArtist);
                }
                else
                {
                    indexDictionary[indexKey] = new List<Subsonic.Common.Classes.Artist> { subsonicArtist };
                }
            }

            foreach (var key in indexDictionary.Keys.OrderBy(k => k))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var index = new Index
                {
                    Name = key.ToString(),
                    Artists = indexDictionary[key].OrderBy(k => k.Name).ToList()
                };

                indexes.Items.Add(index);
            }

            return SubsonicControllerExtensions.CreateResponse(ItemChoiceType.Indexes, indexes);
        }

        [HttpGet("getLicense.view"), HttpPost("getLicense.view")]
        [ServiceFilter(typeof(SubsonicAsyncAuthorizationFilter))]
        [ServiceFilter(typeof(SubsonicAsyncResultFilter))]
        public async Task<Response> GetLicense(CancellationToken cancellationToken)
        {
            await Task.CompletedTask;

            var license = new License
            {
                Valid = true
            };

            return SubsonicControllerExtensions.CreateResponse(ItemChoiceType.License, license);
        }

        [HttpGet("getMusicDirectory.view"), HttpPost("getMusicDirectory.view")]
        [ServiceFilter(typeof(SubsonicAsyncAuthorizationFilter))]
        [ServiceFilter(typeof(SubsonicAsyncResultFilter))]
        public async Task<Response> GetMusicDirectoryAsync([ResonanceParameter] Guid id, CancellationToken cancellationToken)
        {
            var userId = ControllerContext.GetAuthorizationContext().User.Id;

            var mediaType = await MetadataRepository.GetMediaTypeAsync(id, cancellationToken).ConfigureAwait(false);

            if (!mediaType.HasValue)
            {
                return SubsonicControllerExtensions.CreateFailureResponse(ErrorCode.RequestedDataNotFound, SubsonicConstants.DirectoryNotFound);
            }

            cancellationToken.ThrowIfCancellationRequested();

            var directory = new Subsonic.Common.Classes.Directory { Id = id.ToString("n") };

            switch (mediaType)
            {
                case Data.Models.MediaType.Artist:
                    var artistMediaBundle = await MediaLibrary.GetArtistAsync(userId, id, cancellationToken).ConfigureAwait(false);

                    if (artistMediaBundle == null)
                    {
                        return SubsonicControllerExtensions.CreateFailureResponse(ErrorCode.RequestedDataNotFound, SubsonicConstants.DirectoryNotFound);
                    }

                    cancellationToken.ThrowIfCancellationRequested();

                    var albumMediaBundles = await MediaLibrary.GetAlbumsByArtistAsync(userId, id, true, cancellationToken).ConfigureAwait(false);

                    if (albumMediaBundles == null || !albumMediaBundles.Any())
                    {
                        return SubsonicControllerExtensions.CreateFailureResponse(ErrorCode.RequestedDataNotFound, SubsonicConstants.DirectoryNotFound);
                    }

                    cancellationToken.ThrowIfCancellationRequested();

                    directory.Name = artistMediaBundle.Media.Name;

                    directory.Children = new List<Child>();

                    foreach (var album in albumMediaBundles)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        directory.Children.Add(album.ToSubsonicChild());
                    }
                    break;

                case Data.Models.MediaType.Album:
                    var albumMediaBundle = await MediaLibrary.GetAlbumAsync(userId, id, true, cancellationToken).ConfigureAwait(false);

                    if (albumMediaBundle == null)
                    {
                        return SubsonicControllerExtensions.CreateFailureResponse(ErrorCode.RequestedDataNotFound, SubsonicConstants.DirectoryNotFound);
                    }

                    cancellationToken.ThrowIfCancellationRequested();

                    directory.Name = albumMediaBundle.Media.Name;

                    directory.Children = new List<Child>();

                    foreach (var track in albumMediaBundle.Media.Tracks)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        directory.Children.Add(track.ToSubsonicSong(albumMediaBundle));
                    }

                    directory.Parent = albumMediaBundle.Media.Artists.First().Media.Id.ToString("n");
                    break;
            }

            return SubsonicControllerExtensions.CreateResponse(ItemChoiceType.Directory, directory);
        }

        [HttpGet("getMusicFolders.view"), HttpPost("getMusicFolders.view")]
        [ServiceFilter(typeof(SubsonicAsyncAuthorizationFilter))]
        [ServiceFilter(typeof(SubsonicAsyncResultFilter))]
        public async Task<Response> GetMusicFoldersAsync(CancellationToken cancellationToken)
        {
            var collections = await SettingsRepository.GetCollectionsAsync(cancellationToken).ConfigureAwait(false);

            var musicFolders = collections.Select(collection => new MusicFolder
            {
                Id = collection.Id.ToString("n"),
                Name = collection.Name
            }).ToList();

            var musicFolder = new MusicFolders
            {
                Items = musicFolders
            };

            return SubsonicControllerExtensions.CreateResponse(ItemChoiceType.MusicFolders, musicFolder);
        }

        [HttpGet("getNowPlaying.view"), HttpPost("getNowPlaying.view")]
        [ServiceFilter(typeof(SubsonicAsyncAuthorizationFilter))]
        [ServiceFilter(typeof(SubsonicAsyncResultFilter))]
        public async Task<Response> GetNowPlayingAsync(CancellationToken cancellationToken)
        {
            var userId = ControllerContext.GetAuthorizationContext().User.Id;

            var recentPlayback = await MetadataRepository.GetRecentPlaybackAsync(userId, true, cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            var nowPlaying = new NowPlaying { Entries = new List<NowPlayingEntry>() };

            var users = new Dictionary<Guid, Data.Models.User>();

            foreach (var playback in recentPlayback)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var album = await MediaLibrary.GetAlbumAsync(userId, playback.Media.AlbumId, true, cancellationToken).ConfigureAwait(false);

                Data.Models.User user;

                if (users.ContainsKey(playback.Playback.First().UserId))
                {
                    user = users[playback.Playback.First().UserId];
                }
                else
                {
                    user = await MetadataRepository.GetUserAsync(playback.Playback.First().UserId, cancellationToken).ConfigureAwait(false);
                    users.Add(playback.Playback.First().UserId, user);
                }

                var child = playback.ToSubsonicNowPlayingEntry(album, playback.Dispositions.FirstOrDefault(d => d.UserId == userId), playback.Playback.First(), user);

                nowPlaying.Entries.Add(child);
            }

            return SubsonicControllerExtensions.CreateResponse(ItemChoiceType.NowPlaying, nowPlaying);
        }

        [HttpGet("getPlaylist.view"), HttpPost("getPlaylist.view")]
        [ServiceFilter(typeof(SubsonicAsyncAuthorizationFilter))]
        [ServiceFilter(typeof(SubsonicAsyncResultFilter))]
        public async Task<Response> GetPlaylistAsync([ResonanceParameter] Guid id, CancellationToken cancellationToken)
        {
            var userId = ControllerContext.GetAuthorizationContext().User.Id;

            var playlist = await MediaLibrary.GetPlaylistAsync(userId, id, true, cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            var subsonicPlaylist = playlist.ToSubsonicPlaylistWithSongs();

            if (playlist?.Tracks?.Any() == true)
            {
                subsonicPlaylist.Entries = new List<Child>();

                foreach (var track in playlist.Tracks)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    subsonicPlaylist.Entries.Add(track.ToSubsonicSong(await MediaLibrary.GetAlbumAsync(userId, track.Media.AlbumId, false, cancellationToken).ConfigureAwait(false)));
                }
            }

            return SubsonicControllerExtensions.CreateResponse(ItemChoiceType.Playlist, subsonicPlaylist);
        }

        [HttpGet("getPlaylists.view"), HttpPost("getPlaylists.view")]
        [ServiceFilter(typeof(SubsonicAsyncAuthorizationFilter))]
        [ServiceFilter(typeof(SubsonicAsyncResultFilter))]
        public async Task<Response> GetPlaylistsAsync([ResonanceParameter] string username, CancellationToken cancellationToken)
        {
            var userId = ControllerContext.GetAuthorizationContext().User.Id;

            var playlistResults = await MediaLibrary.GetPlaylistsAsync(userId, username, true, cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            var playlists = new Playlists { Items = new List<Subsonic.Common.Classes.Playlist>() };

            foreach (var playlist in playlistResults.OrderBy(p => p.Name))
            {
                playlists.Items.Add(playlist.ToSubsonicPlaylist());
            }

            return SubsonicControllerExtensions.CreateResponse(ItemChoiceType.Playlists, playlists);
        }

        [HttpGet("getPlayQueue.view"), HttpPost("getPlayQueue.view")]
        [ServiceFilter(typeof(SubsonicAsyncAuthorizationFilter))]
        [ServiceFilter(typeof(SubsonicAsyncResultFilter))]
        public async Task<Response> GetPlayQueueAsync(CancellationToken cancellationToken)
        {
            var userId = ControllerContext.GetAuthorizationContext().User.Id;

            var playQueue = await MetadataRepository.GetPlayQueueAsync(userId, cancellationToken).ConfigureAwait(false);

            return SubsonicControllerExtensions.CreateResponse(ItemChoiceType.PlayQueue, playQueue.ToSubsonicPlayQueue());
        }

        [HttpGet("getPodcasts.view"), HttpPost("getPodcasts.view")]
        [ServiceFilter(typeof(SubsonicAsyncAuthorizationFilter))]
        [ServiceFilter(typeof(SubsonicAsyncResultFilter))]
        public async Task<Response> GetPodcasts(CancellationToken cancellationToken)
        {
            await Task.CompletedTask;

            // TODO: GetPodcasts

            return SubsonicControllerExtensions.CreateResponse(ItemChoiceType.Podcasts, new Podcasts());
        }

        [HttpGet("getRandomSongs.view"), HttpPost("getRandomSongs.view")]
        [ServiceFilter(typeof(SubsonicAsyncAuthorizationFilter))]
        [ServiceFilter(typeof(SubsonicAsyncResultFilter))]
        public async Task<Response> GetRandomSongsAsync([ResonanceParameter] int? size, [ResonanceParameter] string genre, [ResonanceParameter] int? fromYear, [ResonanceParameter] int? toYear, [ResonanceParameter] Guid? musicFolderId, CancellationToken cancellationToken)
        {
            var userId = ControllerContext.GetAuthorizationContext().User.Id;

            size = SetBounds(size, 10, 500);

            var trackMediaBundles = await MediaLibrary.GetTracksAsync(userId, size.Value, 0, genre, fromYear, toYear, musicFolderId, true, true, cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            var subsonicSongs = new RandomSongs { Songs = new List<Child>() };

            foreach (var trackMediaBundle in trackMediaBundles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var subsonicSong = trackMediaBundle.ToSubsonicSong(await MediaLibrary.GetAlbumAsync(userId, trackMediaBundle.Media.AlbumId, false, cancellationToken).ConfigureAwait(false));
                subsonicSongs.Songs.Add(subsonicSong);
            }

            return SubsonicControllerExtensions.CreateResponse(ItemChoiceType.RandomSongs, subsonicSongs);
        }

        [HttpGet("getScanStatus.view"), HttpPost("getScanStatus.view")]
        [ServiceFilter(typeof(SubsonicAsyncAuthorizationFilter))]
        [ServiceFilter(typeof(SubsonicAsyncResultFilter))]
        public async Task<Response> GetScanStatusAsync(CancellationToken cancellationToken)
        {
            await Task.CompletedTask;

            var scanProgress = MediaLibrary.ScanProgress;

            var scanInProgress = scanProgress != null;

            var scanStatus = new ScanStatus
            {
                Scanning = scanInProgress
            };

            if (scanInProgress)
            {
                scanStatus.Count = scanProgress.CurrentFile;
            }

            return SubsonicControllerExtensions.CreateResponse(ItemChoiceType.ScanStatus, scanStatus);
        }

        [HttpGet("getSimilarSongs2.view"), HttpPost("getSimilarSongs2.view")]
        [ServiceFilter(typeof(SubsonicAsyncAuthorizationFilter))]
        [ServiceFilter(typeof(SubsonicAsyncResultFilter))]
        public async Task<Response> GetSimilarSongs2Async([ResonanceParameter] Guid id, [ResonanceParameter] int? count, CancellationToken cancellationToken)
        {
            var userId = ControllerContext.GetAuthorizationContext().User.Id;

            var similarSongs = new SimilarSongs2();

            var track = await MediaLibrary.GetTrackAsync(userId, id, false, cancellationToken).ConfigureAwait(false);

            if (track == null)
            {
                return SubsonicControllerExtensions.CreateFailureResponse(ErrorCode.RequestedDataNotFound, SubsonicConstants.MediaFileNotFound);
            }

            // TODO: Support similar tracks

            return SubsonicControllerExtensions.CreateResponse(ItemChoiceType.SimilarSongs2, similarSongs);
        }

        [HttpGet("getSimilarSongs.view"), HttpPost("getSimilarSongs.view")]
        [ServiceFilter(typeof(SubsonicAsyncAuthorizationFilter))]
        [ServiceFilter(typeof(SubsonicAsyncResultFilter))]
        public async Task<Response> GetSimilarSongsAsync([ResonanceParameter] Guid id, [ResonanceParameter] int? count, CancellationToken cancellationToken)
        {
            var userId = ControllerContext.GetAuthorizationContext().User.Id;

            var similarSongs = new SimilarSongs();

            var track = await MediaLibrary.GetTrackAsync(userId, id, false, cancellationToken).ConfigureAwait(false);

            if (track == null)
            {
                return SubsonicControllerExtensions.CreateFailureResponse(ErrorCode.RequestedDataNotFound, SubsonicConstants.MediaFileNotFound);
            }

            // TODO: Support similar tracks

            return SubsonicControllerExtensions.CreateResponse(ItemChoiceType.SimilarSongs, similarSongs);
        }

        [HttpGet("getSong.view"), HttpPost("getSong.view")]
        [ServiceFilter(typeof(SubsonicAsyncAuthorizationFilter))]
        [ServiceFilter(typeof(SubsonicAsyncResultFilter))]
        public async Task<Response> GetSongAsync([ResonanceParameter] Guid id, CancellationToken cancellationToken)
        {
            var userId = ControllerContext.GetAuthorizationContext().User.Id;

            var trackMediaBundle = await MediaLibrary.GetTrackAsync(userId, id, true, cancellationToken).ConfigureAwait(false);

            if (trackMediaBundle == null)
            {
                return SubsonicControllerExtensions.CreateFailureResponse(ErrorCode.RequestedDataNotFound, SubsonicConstants.SongNotFound);
            }

            cancellationToken.ThrowIfCancellationRequested();

            var subsonicSong = trackMediaBundle.ToSubsonicSong(await MediaLibrary.GetAlbumAsync(userId, trackMediaBundle.Media.AlbumId, false, cancellationToken).ConfigureAwait(false));

            return SubsonicControllerExtensions.CreateResponse(ItemChoiceType.Song, subsonicSong);
        }

        [HttpGet("getSongsByGenre.view"), HttpPost("getSongsByGenre.view")]
        [ServiceFilter(typeof(SubsonicAsyncAuthorizationFilter))]
        [ServiceFilter(typeof(SubsonicAsyncResultFilter))]
        public async Task<Response> GetSongsByGenreAsync([ResonanceParameter] string genre, [ResonanceParameter] int? count, [ResonanceParameter] int? offset, [ResonanceParameter] Guid? musicFolderId, CancellationToken cancellationToken)
        {
            var userId = ControllerContext.GetAuthorizationContext().User.Id;

            count = SetBounds(count, 10, 500);

            var trackMediaBundles = await MediaLibrary.GetTracksAsync(userId, count.GetValueOrDefault(), offset.GetValueOrDefault(), genre, null, null, musicFolderId, true, false, cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            var subsonicSongs = new SongsByGenre { Songs = new List<Child>() };

            foreach (var trackMediaBundle in trackMediaBundles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var subsonicSong = trackMediaBundle.ToSubsonicSong(await MediaLibrary.GetAlbumAsync(userId, trackMediaBundle.Media.AlbumId, false, cancellationToken).ConfigureAwait(false));
                subsonicSongs.Songs.Add(subsonicSong);
            }

            return SubsonicControllerExtensions.CreateResponse(ItemChoiceType.SongsByGenre, subsonicSongs);
        }

        [HttpGet("getStarred2.view"), HttpPost("getStarred2.view")]
        [ServiceFilter(typeof(SubsonicAsyncAuthorizationFilter))]
        [ServiceFilter(typeof(SubsonicAsyncResultFilter))]
        public async Task<Response> GetStarred2Async([ResonanceParameter(Name = "musicFolderId")] Guid? collectionId, CancellationToken cancellationToken)
        {
            var userId = ControllerContext.GetAuthorizationContext().User.Id;

            var artists = await MetadataRepository.GetFavoritedAsync<Data.Models.Artist>(userId, collectionId, true, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            var albums = await MetadataRepository.GetFavoritedAsync<Album>(userId, collectionId, true, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            var tracks = await MetadataRepository.GetFavoritedAsync<Track>(userId, collectionId, true, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            var starred = new Starred2();

            if (artists != null)
            {
                starred.Artists = artists.Select(a => a.ToSubsonicArtistID3()).ToList();
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (albums != null)
            {
                starred.Albums = albums.Select(a => a.ToSubsonicAlbumID3()).ToList();
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (tracks != null)
            {
                starred.Songs = tracks.Select(t => t.ToSubsonicSong(MediaLibrary.GetAlbumAsync(userId, t.Media.AlbumId, false, cancellationToken).Result)).ToList();
            }

            return SubsonicControllerExtensions.CreateResponse(ItemChoiceType.Starred2, starred);
        }

        [HttpGet("getStarred.view"), HttpPost("getStarred.view")]
        [ServiceFilter(typeof(SubsonicAsyncAuthorizationFilter))]
        [ServiceFilter(typeof(SubsonicAsyncResultFilter))]
        public async Task<Response> GetStarredAsync([ResonanceParameter(Name = "musicFolderId")] Guid? collectionId, CancellationToken cancellationToken)
        {
            var userId = ControllerContext.GetAuthorizationContext().User.Id;

            var artists = await MetadataRepository.GetFavoritedAsync<Data.Models.Artist>(userId, collectionId, true, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            var albums = await MetadataRepository.GetFavoritedAsync<Album>(userId, collectionId, true, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            var tracks = await MetadataRepository.GetFavoritedAsync<Track>(userId, collectionId, true, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            var starred = new Starred();

            if (artists != null)
            {
                starred.Artists = artists.Select(a => a.ToSubsonicArtist()).ToList();
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (albums != null)
            {
                starred.Albums = albums.Select(a => a.ToSubsonicChild()).ToList();
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (tracks != null)
            {
                starred.Songs = tracks.Select(t => t.ToSubsonicSong(MediaLibrary.GetAlbumAsync(userId, t.Media.AlbumId, false, cancellationToken).Result)).ToList();
            }

            return SubsonicControllerExtensions.CreateResponse(ItemChoiceType.Starred, starred);
        }

        [HttpGet("getTopSongs.view"), HttpPost("getTopSongs.view")]
        [ServiceFilter(typeof(SubsonicAsyncAuthorizationFilter))]
        [ServiceFilter(typeof(SubsonicAsyncResultFilter))]
        public async Task<Response> GetTopSongsAsync([ResonanceParameter] string artist, [ResonanceParameter] int? count, CancellationToken cancellationToken)
        {
            var userId = ControllerContext.GetAuthorizationContext().User.Id;

            count = SetBounds(count, 50, 500);

            var topSongs = new TopSongs { Songs = new List<Child>() };

            var topTracks = await MediaLibrary.GetTopTracksAsync(artist, count.Value, cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            foreach (var topTrack in topTracks)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var trackModel = await MediaLibrary.GetTrackAsync(userId, artist, topTrack.LastFm.Name, null, true, cancellationToken).ConfigureAwait(false);

                if (trackModel != null)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var subsonicSong = trackModel.ToSubsonicSong(await MediaLibrary.GetAlbumAsync(userId, trackModel.Media.AlbumId, false, cancellationToken).ConfigureAwait(false));
                    topSongs.Songs.Add(subsonicSong);
                }
            }

            return SubsonicControllerExtensions.CreateResponse(ItemChoiceType.TopSongs, topSongs);
        }

        [HttpGet("getUser.view"), HttpPost("getUser.view")]
        [ServiceFilter(typeof(SubsonicAsyncAuthorizationFilter))]
        [ServiceFilter(typeof(SubsonicAsyncResultFilter))]
        public async Task<Response> GetUserAsync([ResonanceParameter] string username, CancellationToken cancellationToken)
        {
            var authorizationContext = ControllerContext.GetAuthorizationContext();

            Data.Models.User user;

            if (username != authorizationContext.User.Name)
            {
                if (!authorizationContext.Roles.Contains(Role.Administrator))
                {
                    authorizationContext.ErrorCode = (int)ErrorCode.UserNotAuthorized;
                    authorizationContext.Status = SubsonicConstants.UserIsNotAuthorizedForTheGivenOperation;

                    return authorizationContext.CreateAuthorizationFailureResponse();
                }

                user = await MetadataRepository.GetUserAsync(username, cancellationToken).ConfigureAwait(false);

                if (user.Roles == null || !user.Roles.Any())
                {
                    user.Roles = await MetadataRepository.GetRolesForUserAsync(user.Id, cancellationToken).ConfigureAwait(false);
                }
            }
            else
            {
                user = authorizationContext.User;
                user.Roles = authorizationContext.Roles;
            }

            return SubsonicControllerExtensions.CreateResponse(ItemChoiceType.User, user.ToSubsonicUser());
        }

        [HttpGet("getUsers.view"), HttpPost("getUsers.view")]
        [ServiceFilter(typeof(SubsonicAsyncAuthorizationFilter))]
        [ServiceFilter(typeof(SubsonicAsyncResultFilter))]
        [Authorize(Policy = PolicyConstants.Administration)]
        public async Task<Response> GetUsersAsync(CancellationToken cancellationToken)
        {
            var subsonicUsers = new Users { Items = new List<Subsonic.Common.Classes.User>() };

            var users = await MetadataRepository.GetUsersAsync(cancellationToken).ConfigureAwait(false);

            foreach (var user in users)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (user.Roles == null || !user.Roles.Any())
                {
                    user.Roles = await MetadataRepository.GetRolesForUserAsync(user.Id, cancellationToken).ConfigureAwait(false);
                }

                var subsonicUser = user.ToSubsonicUser();

                subsonicUsers.Items.Add(subsonicUser);
            }

            return SubsonicControllerExtensions.CreateResponse(ItemChoiceType.Users, subsonicUsers);
        }

        [HttpGet("getVideoInfo.view"), HttpPost("getVideoInfo.view")]
        [ServiceFilter(typeof(SubsonicAsyncAuthorizationFilter))]
        [ServiceFilter(typeof(SubsonicAsyncResultFilter))]
        public async Task<Response> GetVideoInfo([ResonanceParameter] Guid id, CancellationToken cancellationToken)
        {
            await Task.CompletedTask;

            // TODO: GetVideoInfo
            return SubsonicControllerExtensions.CreateFailureResponse(ErrorCode.RequestedDataNotFound, SubsonicConstants.VideoNotFound);
        }

        [HttpGet("getVideos.view"), HttpPost("getVideos.view")]
        [ServiceFilter(typeof(SubsonicAsyncAuthorizationFilter))]
        [ServiceFilter(typeof(SubsonicAsyncResultFilter))]
        public async Task<Response> GetVideos(CancellationToken cancellationToken)
        {
            await Task.CompletedTask;

            // TODO: GetVideos
            return SubsonicControllerExtensions.CreateResponse(ItemChoiceType.Videos, new Videos());
        }

        [HttpGet("hls.view"), HttpPost("hls.view")]
        [ServiceFilter(typeof(SubsonicAsyncAuthorizationFilter))]
        [Authorize(Policy = PolicyConstants.Stream)]
        public async Task<IActionResult> HlsAsync([ResonanceParameter] Guid id, [ResonanceParameter] int? bitRate, [ResonanceParameter] int? audioTrack, CancellationToken cancellationToken)
        {
            var queryParameters = Request.GetSubsonicQueryParameters();

            var userId = ControllerContext.GetAuthorizationContext().User.Id;

            var trackMediaBundle = await MediaLibrary.GetTrackAsync(userId, id, false, cancellationToken).ConfigureAwait(false);

            if (trackMediaBundle == null)
            {
                return StatusCode(404);
            }

            var track = trackMediaBundle.Media;

            var playback = new Playback
            {
                ClientId = queryParameters.ClientName,
                CollectionId = track.CollectionId,
                PlaybackDateTime = DateTime.UtcNow,
                TrackId = track.Id,
                UserId = userId,
                Address = Request.HttpContext.Connection.RemoteIpAddress.ToString()
            };

            await MetadataRepository.AddPlaybackAsync(playback, cancellationToken).ConfigureAwait(false);

            return File(System.IO.File.Open(track.Path, FileMode.Open, FileAccess.Read, FileShare.Read), MimeType.GetMimeType(track.Path), Path.GetFileName(track.Path));
        }

        [HttpGet("ping.view"), HttpPost("ping.view")]
        [ServiceFilter(typeof(SubsonicAsyncAuthorizationFilter))]
        [ServiceFilter(typeof(SubsonicAsyncResultFilter))]
        public async Task<Response> PingAsync(CancellationToken cancellationToken)
        {
            await Task.CompletedTask;

            return SubsonicControllerExtensions.CreateResponse();
        }

        [HttpGet("savePlayQueue.view"), HttpPost("savePlayQueue.view")]
        [ServiceFilter(typeof(SubsonicAsyncAuthorizationFilter))]
        [ServiceFilter(typeof(SubsonicAsyncResultFilter))]
        public async Task<Response> SavePlayQueueAsync([ResonanceParameter(Name = "id")] List<Guid> ids, [ResonanceParameter] Guid? current, [ResonanceParameter] int? position, CancellationToken cancellationToken)
        {
            var queryParameters = ControllerContext.GetSubsonicQueryParameters();
            var authorizationContext = ControllerContext.GetAuthorizationContext();

            var userId = authorizationContext.User.Id;

            if (ids.Count == 0)
            {
                await MetadataRepository.DeletePlayQueueAsync(userId, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                var playQueue = await MetadataRepository.GetPlayQueueAsync(userId, cancellationToken).ConfigureAwait(false) ?? new Data.Models.PlayQueue();

                playQueue.ClientName = queryParameters.ClientName;
                playQueue.CurrentTrackId = current;
                playQueue.Position = position;
                playQueue.User = authorizationContext.User;
                playQueue.Tracks = new List<MediaBundle<Track>>();

                foreach (var id in ids)
                {
                    var track = await MediaLibrary.GetTrackAsync(userId, id, false, cancellationToken).ConfigureAwait(false);

                    if (track != null)
                    {
                        playQueue.Tracks.Add(track);
                    }
                }

                await MetadataRepository.UpdatePlayQueueAsync(playQueue, cancellationToken).ConfigureAwait(false);
            }

            return SubsonicControllerExtensions.CreateResponse();
        }

        [HttpGet("scrobble.view"), HttpPost("scrobble.view")]
        [ServiceFilter(typeof(SubsonicAsyncAuthorizationFilter))]
        [ServiceFilter(typeof(SubsonicAsyncResultFilter))]
        [Authorize(Policy = PolicyConstants.Stream)]
        public async Task<Response> ScrobbleAsync([ResonanceParameter] Guid id, [ResonanceParameter] long? time, CancellationToken cancellationToken, [ResonanceParameter] bool? submission)
        {
            var userId = ControllerContext.GetAuthorizationContext().User.Id;

            var queryParameters = ControllerContext.GetSubsonicQueryParameters();

            var track = await MediaLibrary.GetTrackAsync(userId, id, false, cancellationToken).ConfigureAwait(false);

            var playback = new Playback
            {
                ClientId = queryParameters.ClientName,
                CollectionId = track.Media.CollectionId,
                PlaybackDateTime = time.HasValue ? DateTimeExtensions.DateTimeFromUnixTimestampMilliseconds(time.Value) : DateTime.UtcNow,
                TrackId = track.Media.Id,
                UserId = userId,
                Address = Request.HttpContext.Connection.RemoteIpAddress.ToString()
            };

            await MetadataRepository.AddPlaybackAsync(playback, cancellationToken).ConfigureAwait(false);

            return SubsonicControllerExtensions.CreateResponse();
        }

        [HttpGet("search.view"), HttpPost("search.view")]
        [ServiceFilter(typeof(SubsonicAsyncAuthorizationFilter))]
        [ServiceFilter(typeof(SubsonicAsyncResultFilter))]
        public async Task<Response> Search([ResonanceParameter] string artist, [ResonanceParameter] string album, [ResonanceParameter] string title, [ResonanceParameter] string any, [ResonanceParameter] int? count, [ResonanceParameter] int? offset, [ResonanceParameter] long? newerThan, CancellationToken cancellationToken)
        {
            await Task.CompletedTask;

            // TODO: Search

            return SubsonicControllerExtensions.CreateResponse(ItemChoiceType.SearchResult, new SearchResult());
        }

        [HttpGet("search2.view"), HttpPost("search2.view")]
        [ServiceFilter(typeof(SubsonicAsyncAuthorizationFilter))]
        [ServiceFilter(typeof(SubsonicAsyncResultFilter))]
        public async Task<Response> Search2Async([ResonanceParameter] string query, [ResonanceParameter] int? artistCount, [ResonanceParameter] int? artistOffset, [ResonanceParameter] int? albumCount, [ResonanceParameter] int? albumOffset, [ResonanceParameter] int? songCount, [ResonanceParameter] int? songOffset, [ResonanceParameter] Guid? musicFolderId, CancellationToken cancellationToken)
        {
            if (query.EndsWith("*") && query.Length == 2)
            {
                return SubsonicControllerExtensions.CreateResponse(ItemChoiceType.SearchResult2, new SearchResult2());
            }

            IEnumerable<MediaBundle<Data.Models.Artist>> artists = null;
            IEnumerable<MediaBundle<Album>> albums = null;
            IEnumerable<MediaBundle<Track>> tracks = null;

            if (!artistCount.HasValue || artistCount < 0)
            {
                artistCount = 20;
            }

            if (!albumCount.HasValue || albumCount < 0)
            {
                albumCount = 20;
            }

            if (!songCount.HasValue || songCount < 0)
            {
                songCount = 20;
            }

            var userId = ControllerContext.GetAuthorizationContext().User.Id;

            if (artistCount > 0)
            {
                artists = await MediaLibrary.SearchArtistsAsync(userId, query, artistCount.GetValueOrDefault(), artistOffset.GetValueOrDefault(), musicFolderId, true, cancellationToken).ConfigureAwait(false);
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (albumCount > 0)
            {
                albums = await MediaLibrary.SearchAlbumsAsync(userId, query, albumCount.GetValueOrDefault(), albumOffset.GetValueOrDefault(), musicFolderId, true, cancellationToken).ConfigureAwait(false);
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (songCount > 0)
            {
                tracks = await MediaLibrary.SearchTracksAsync(userId, query, songCount.GetValueOrDefault(), songOffset.GetValueOrDefault(), musicFolderId, true, cancellationToken).ConfigureAwait(false);
            }

            cancellationToken.ThrowIfCancellationRequested();

            var searchResult2 = new SearchResult2();

            if (artists != null)
            {
                searchResult2.Artists = artists.Select(a => a.ToSubsonicArtist()).ToList();
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (albums != null)
            {
                searchResult2.Albums = albums.Select(a => a.ToSubsonicChild()).ToList();
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (tracks != null)
            {
                searchResult2.Songs = tracks.Select(t => t.ToSubsonicSong(MediaLibrary.GetAlbumAsync(userId, t.Media.AlbumId, false, cancellationToken).Result)).ToList();
            }

            return SubsonicControllerExtensions.CreateResponse(ItemChoiceType.SearchResult2, searchResult2);
        }

        [HttpGet("search3.view"), HttpPost("search3.view")]
        [ServiceFilter(typeof(SubsonicAsyncAuthorizationFilter))]
        [ServiceFilter(typeof(SubsonicAsyncResultFilter))]
        public async Task<Response> Search3Async([ResonanceParameter] string query, [ResonanceParameter] int? artistCount, [ResonanceParameter] int? artistOffset, [ResonanceParameter] int? albumCount, [ResonanceParameter] int? albumOffset, [ResonanceParameter] int? songCount, [ResonanceParameter] int? songOffset, [ResonanceParameter] Guid? musicFolderId, CancellationToken cancellationToken)
        {
            if (query.EndsWith("*") && query.Length == 2)
            {
                return SubsonicControllerExtensions.CreateResponse(ItemChoiceType.SearchResult3, new SearchResult3());
            }

            IEnumerable<MediaBundle<Data.Models.Artist>> artists = null;
            IEnumerable<MediaBundle<Album>> albums = null;
            IEnumerable<MediaBundle<Track>> tracks = null;

            if (!artistCount.HasValue || artistCount < 0)
            {
                artistCount = 20;
            }

            if (!albumCount.HasValue || albumCount < 0)
            {
                albumCount = 20;
            }

            if (!songCount.HasValue || songCount < 0)
            {
                songCount = 20;
            }

            var userId = ControllerContext.GetAuthorizationContext().User.Id;

            if (artistCount > 0)
            {
                artists = await MediaLibrary.SearchArtistsAsync(userId, query, artistCount.GetValueOrDefault(), artistOffset.GetValueOrDefault(), musicFolderId, true, cancellationToken).ConfigureAwait(false);
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (albumCount > 0)
            {
                albums = await MediaLibrary.SearchAlbumsAsync(userId, query, albumCount.GetValueOrDefault(), albumOffset.GetValueOrDefault(), musicFolderId, true, cancellationToken).ConfigureAwait(false);
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (songCount > 0)
            {
                tracks = await MediaLibrary.SearchTracksAsync(userId, query, songCount.GetValueOrDefault(), songOffset.GetValueOrDefault(), musicFolderId, true, cancellationToken).ConfigureAwait(false);
            }

            cancellationToken.ThrowIfCancellationRequested();

            var searchResult3 = new SearchResult3();

            if (artists != null)
            {
                searchResult3.Artists = artists.Select(a => a.ToSubsonicArtistID3()).ToList();
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (albums != null)
            {
                searchResult3.Albums = albums.Select(a => a.ToSubsonicAlbumID3()).ToList();
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (tracks != null)
            {
                searchResult3.Songs = tracks.Select(t => t.ToSubsonicSong(MediaLibrary.GetAlbumAsync(userId, t.Media.AlbumId, false, cancellationToken).Result)).ToList();
            }

            return SubsonicControllerExtensions.CreateResponse(ItemChoiceType.SearchResult3, searchResult3);
        }

        [HttpGet("setRating.view"), HttpPost("setRating.view")]
        [ServiceFilter(typeof(SubsonicAsyncAuthorizationFilter))]
        [ServiceFilter(typeof(SubsonicAsyncResultFilter))]
        public async Task<Response> SetRatingAsync([ResonanceParameter] Guid id, [ResonanceParameter] int rating, CancellationToken cancellationToken)
        {
            var userId = ControllerContext.GetAuthorizationContext().User.Id;

            var disposition = await MetadataRepository.GetDispositionAsync(userId, id, cancellationToken).ConfigureAwait(false) ??
                              new Disposition
                              {
                                  MediaId = id,
                                  UserId = userId,
                                  MediaType = await MetadataRepository.GetMediaTypeAsync(id, cancellationToken).ConfigureAwait(false)
                              };

            if (disposition.CollectionId == Guid.Empty)
            {
                await SetDipositionCollectionIdAsync(disposition, id, userId, cancellationToken).ConfigureAwait(false);
            }

            disposition.UserRating = rating;

            await MetadataRepository.SetDispositionAsync(disposition, cancellationToken).ConfigureAwait(false);

            return SubsonicControllerExtensions.CreateResponse();
        }

        [HttpGet("star.view"), HttpPost("star.view")]
        [ServiceFilter(typeof(SubsonicAsyncAuthorizationFilter))]
        [ServiceFilter(typeof(SubsonicAsyncResultFilter))]
        [ResponseCache(NoStore = true)]
        public async Task<Response> StarAsync([ResonanceParameter] Guid? id, [ResonanceParameter] Guid? albumId, [ResonanceParameter] Guid? artistId, CancellationToken cancellationToken)
        {
            var userId = ControllerContext.GetAuthorizationContext().User.Id;

            Guid mediaId;

            if (id.HasValue)
            {
                mediaId = id.Value;
            }
            else if (albumId.HasValue)
            {
                mediaId = albumId.Value;
            }
            else if (artistId.HasValue)
            {
                mediaId = artistId.Value;
            }

            var disposition = await MetadataRepository.GetDispositionAsync(userId, mediaId, cancellationToken).ConfigureAwait(false) ??
                              new Disposition
                              {
                                  MediaId = mediaId,
                                  UserId = userId,
                                  MediaType = await MetadataRepository.GetMediaTypeAsync(mediaId, cancellationToken).ConfigureAwait(false)
                              };

            if (disposition.CollectionId == Guid.Empty)
            {
                await SetDipositionCollectionIdAsync(disposition, mediaId, userId, cancellationToken).ConfigureAwait(false);
            }

            disposition.Favorited = DateTime.UtcNow;

            await MetadataRepository.SetDispositionAsync(disposition, cancellationToken).ConfigureAwait(false);

            return SubsonicControllerExtensions.CreateResponse();
        }

        [HttpGet("startScan.view"), HttpPost("startScan.view")]
        [ServiceFilter(typeof(SubsonicAsyncAuthorizationFilter))]
        [ServiceFilter(typeof(SubsonicAsyncResultFilter))]
        public async Task<Response> StartScanAsync(CancellationToken cancellationToken)
        {
            await Task.CompletedTask;

            var userId = ControllerContext.GetAuthorizationContext().User.Id;

            var scanProgress = MediaLibrary.ScanProgress;

            var scanInProgress = scanProgress != null;

            if (!scanInProgress)
            {
                MediaLibrary.ScanLibrary(userId, null, false, cancellationToken);

                SpinWait.SpinUntil(() => MediaLibrary.ScanProgress != null, TimeSpan.FromSeconds(5));

                scanProgress = MediaLibrary.ScanProgress;
                scanInProgress = scanProgress != null;
            }

            var scanStatus = new ScanStatus
            {
                Scanning = scanInProgress
            };

            if (scanInProgress)
            {
                scanStatus.Count = scanProgress.CurrentFile;
            }

            return SubsonicControllerExtensions.CreateResponse(ItemChoiceType.ScanStatus, scanStatus);
        }

        [HttpGet("stream.view"), HttpPost("stream.view")]
        [ServiceFilter(typeof(SubsonicAsyncAuthorizationFilter))]
        [Authorize(Policy = PolicyConstants.Stream)]
        public async Task<IActionResult> StreamAsync([ResonanceParameter] Guid id, [ResonanceParameter] int? maxBitRate, [ResonanceParameter(Name = "format")] string streamFormat, [ResonanceParameter] int? timeOffset, [ResonanceParameter] string size, [ResonanceParameter] bool estimateContentLength, [ResonanceParameter] bool converted, CancellationToken cancellationToken)
        {
            var queryParameters = Request.GetSubsonicQueryParameters();

            var userId = ControllerContext.GetAuthorizationContext().User.Id;

            var trackMediaBundle = await MediaLibrary.GetTrackAsync(userId, id, false, cancellationToken).ConfigureAwait(false);

            if (trackMediaBundle == null)
            {
                return StatusCode(404);
            }

            var track = trackMediaBundle.Media;

            var playback = new Playback
            {
                ClientId = queryParameters.ClientName,
                CollectionId = track.CollectionId,
                PlaybackDateTime = DateTime.UtcNow,
                TrackId = track.Id,
                UserId = userId,
                Address = Request.HttpContext.Connection.RemoteIpAddress.ToString()
            };

            await MetadataRepository.AddPlaybackAsync(playback, cancellationToken).ConfigureAwait(false);

            if (!string.Equals(streamFormat, "raw", StringComparison.InvariantCultureIgnoreCase) && maxBitRate.HasValue && maxBitRate.Value > 0)
            {
                if (string.IsNullOrWhiteSpace(streamFormat))
                {
                    streamFormat = "mp3";
                }

                var convertedStream = _transcode.Convert(track.Path, streamFormat, maxBitRate.Value, cancellationToken);

                return File(convertedStream, "audio/mpeg", Path.ChangeExtension(Path.GetFileName(track.Path), streamFormat));
            }

            return File(System.IO.File.Open(track.Path, FileMode.Open, FileAccess.Read, FileShare.Read), MimeType.GetMimeType(track.Path), Path.GetFileName(track.Path));
        }

        [HttpGet("unstar.view"), HttpPost("unstar.view")]
        [ServiceFilter(typeof(SubsonicAsyncAuthorizationFilter))]
        [ServiceFilter(typeof(SubsonicAsyncResultFilter))]
        [ResponseCache(NoStore = true)]
        public async Task<Response> UnStarAsync([ResonanceParameter] Guid? id, [ResonanceParameter] Guid? albumId, [ResonanceParameter] Guid? artistId, CancellationToken cancellationToken)
        {
            var userId = ControllerContext.GetAuthorizationContext().User.Id;

            Guid mediaId;

            if (id.HasValue)
            {
                mediaId = id.Value;
            }
            else if (albumId.HasValue)
            {
                mediaId = albumId.Value;
            }
            else if (artistId.HasValue)
            {
                mediaId = artistId.Value;
            }

            var disposition = await MetadataRepository.GetDispositionAsync(userId, mediaId, cancellationToken).ConfigureAwait(false) ??
                              new Disposition
                              {
                                  MediaId = mediaId,
                                  UserId = userId,
                                  MediaType = await MetadataRepository.GetMediaTypeAsync(mediaId, cancellationToken).ConfigureAwait(false)
                              };

            if (disposition.CollectionId == Guid.Empty)
            {
                await SetDipositionCollectionIdAsync(disposition, mediaId, userId, cancellationToken).ConfigureAwait(false);
            }

            disposition.Favorited = null;

            await MetadataRepository.SetDispositionAsync(disposition, cancellationToken).ConfigureAwait(false);

            return SubsonicControllerExtensions.CreateResponse();
        }

        [HttpGet("updatePlaylist.view"), HttpPost("updatePlaylist.view")]
        [ServiceFilter(typeof(SubsonicAsyncAuthorizationFilter))]
        [ServiceFilter(typeof(SubsonicAsyncResultFilter))]
        public async Task<Response> UpdatePlaylistAsync([ResonanceParameter] Guid playlistId, [ResonanceParameter] string name, [ResonanceParameter] string comment, [ResonanceParameter(Name = "public")] bool? isPublic, [ResonanceParameter(Name = "songIdToAdd")] List<Guid> songIdsToAdd, [ResonanceParameter(Name = "songIndexToRemove")] List<int> songIndexesToRemove, CancellationToken cancellationToken)
        {
            var userId = ControllerContext.GetAuthorizationContext().User.Id;

            var playlist = await MetadataRepository.GetPlaylistAsync(userId, playlistId, true, cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            if (!string.IsNullOrWhiteSpace(name))
            {
                playlist.Name = name;
            }

            if (!string.IsNullOrEmpty(comment))
            {
                playlist.Comment = comment;
            }

            playlist.Accessibility = isPublic.GetValueOrDefault() ? Accessibility.Public : Accessibility.Private;

            if (songIdsToAdd.Count > 0 || songIndexesToRemove.Count > 0)
            {
                var updatedTrackListing = new List<MediaBundle<Track>>();

                if (songIndexesToRemove.Count > 0)
                {
                    updatedTrackListing.AddRange(playlist.Tracks.Where((t, i) => !songIndexesToRemove.Contains(i)));
                }

                foreach (var songId in songIdsToAdd)
                {
                    var track = await MediaLibrary.GetTrackAsync(userId, songId, false, cancellationToken).ConfigureAwait(false);
                    updatedTrackListing.Add(track);
                }

                playlist.Tracks = updatedTrackListing;
            }

            await MetadataRepository.UpdatePlaylistAsync(playlist, cancellationToken).ConfigureAwait(false);

            MediaLibrary.RemovePlaylistFromCache(userId, playlist.Id, true);

            return SubsonicControllerExtensions.CreateResponse();
        }

        [HttpGet("updateUser.view"), HttpPost("updateUser.view")]
        [ServiceFilter(typeof(SubsonicAsyncAuthorizationFilter))]
        [ServiceFilter(typeof(SubsonicAsyncResultFilter))]
        [Authorize(Policy = PolicyConstants.Administration)]
        public async Task<Response> UpdateUserAsync([ResonanceParameter] string username, [ResonanceParameter] string password, [ResonanceParameter] string email, [ResonanceParameter] bool? adminRole, [ResonanceParameter] bool? settingsRole, [ResonanceParameter] bool? streamRole, [ResonanceParameter] bool? downloadRole, [ResonanceParameter(Name = "musicFolderId")] List<int> musicFolderIds, CancellationToken cancellationToken)
        {
            var user = await MetadataRepository.GetUserAsync(username, cancellationToken).ConfigureAwait(false);

            if (user == null)
            {
                return new AuthorizationContext
                {
                    ErrorCode = (int)ErrorCode.GenericError,
                    Status = SubsonicConstants.UserDoesNotExist
                }.CreateAuthorizationFailureResponse();
            }

            if (user.Roles == null || !user.Roles.Any())
            {
                user.Roles = await MetadataRepository.GetRolesForUserAsync(user.Id, cancellationToken).ConfigureAwait(false);
            }

            if (email != null)
            {
                user.EmailAddress = email;
            }

            if (password != null)
            {
                user.Password = SubsonicControllerExtensions.ParsePassword(password).EncryptString(Constants.ResonanceKey);
            }

            var newRoles = user.Roles.ToList();

            if (adminRole != null)
            {
                if (adminRole.GetValueOrDefault())
                {
                    if (!newRoles.Contains(Role.Administrator))
                    {
                        newRoles.Add(Role.Administrator);
                    }
                }
                else
                {
                    newRoles.Remove(Role.Administrator);
                }
            }

            if (settingsRole != null)
            {
                if (settingsRole.GetValueOrDefault())
                {
                    if (!newRoles.Contains(Role.Settings))
                    {
                        newRoles.Add(Role.Settings);
                    }
                }
                else
                {
                    newRoles.Remove(Role.Settings);
                }
            }

            if (streamRole != null)
            {
                if (streamRole.GetValueOrDefault())
                {
                    if (!newRoles.Contains(Role.Playback))
                    {
                        newRoles.Add(Role.Playback);
                    }
                }
                else
                {
                    newRoles.Remove(Role.Playback);
                }
            }

            if (downloadRole != null)
            {
                if (downloadRole.GetValueOrDefault())
                {
                    if (!newRoles.Contains(Role.Download))
                    {
                        newRoles.Add(Role.Download);
                    }
                }
                else
                {
                    newRoles.Remove(Role.Download);
                }
            }

            user.Roles = newRoles;

            await MetadataRepository.InsertOrUpdateUserAsync(user, cancellationToken).ConfigureAwait(false);

            return SubsonicControllerExtensions.CreateResponse();
        }

        private static int? SetBounds(int? size, int min, int max)
        {
            if (!size.HasValue)
            {
                size = min;
            }

            if (size > max)
            {
                size = max;
            }

            if (size < min)
            {
                size = min;
            }

            return size;
        }

        private async Task<IEnumerable<MediaBundle<Album>>> GetAlbumListInternalAsync(Guid userId, AlbumListType type, int? size, int? offset, int? fromYear, int? toYear, string genre, Guid? musicFolderId, CancellationToken cancellationToken)
        {
            IEnumerable<MediaBundle<Album>> albumMediaBundles = new List<MediaBundle<Album>>();

            switch (type)
            {
                case AlbumListType.ByYear:
                case AlbumListType.ByGenre:
                case AlbumListType.AlphabeticalByName:
                    albumMediaBundles = await MetadataRepository.GetAlphabeticalAlbumsAsync(userId, size.GetValueOrDefault(), offset.GetValueOrDefault(), genre, fromYear, toYear, musicFolderId, true, cancellationToken).ConfigureAwait(false);
                    break;

                case AlbumListType.Random:
                    albumMediaBundles = await MediaLibrary.GetRandomAlbumsAsync(userId, size.GetValueOrDefault(), offset.GetValueOrDefault(), genre, fromYear, toYear, musicFolderId, true, cancellationToken).ConfigureAwait(false);
                    break;

                case AlbumListType.Newest:
                    albumMediaBundles = await MediaLibrary.GetNewestAlbumsAsync(userId, size.GetValueOrDefault(), offset.GetValueOrDefault(), genre, fromYear, toYear, musicFolderId, true, cancellationToken).ConfigureAwait(false);
                    break;

                case AlbumListType.Starred:
                    albumMediaBundles = await MediaLibrary.GetFavoritedAlbumsAsync(userId, size.GetValueOrDefault(), offset.GetValueOrDefault(), genre, fromYear, toYear, musicFolderId, true, cancellationToken).ConfigureAwait(false);
                    break;

                case AlbumListType.AlphabeticalByArtist:
                    albumMediaBundles = await MetadataRepository.GetAlphabeticalByArtistAlbumsAsync(userId, size.GetValueOrDefault(), offset.GetValueOrDefault(), genre, fromYear, toYear, musicFolderId, true, cancellationToken).ConfigureAwait(false);
                    break;

                case AlbumListType.Highest:
                    albumMediaBundles = await MediaLibrary.GetHighestRatedAlbumsAsync(userId, size.GetValueOrDefault(), offset.GetValueOrDefault(), genre, fromYear, toYear, musicFolderId, true, cancellationToken).ConfigureAwait(false);
                    break;

                case AlbumListType.Frequent:
                    albumMediaBundles = await MetadataRepository.GetMostPlayedAlbumsAsync(userId, size.GetValueOrDefault(), offset.GetValueOrDefault(), genre, fromYear, toYear, musicFolderId, true, cancellationToken).ConfigureAwait(false);
                    break;

                case AlbumListType.Recent:
                    albumMediaBundles = await MetadataRepository.GetMostRecentlyPlayedAlbumsAsync(userId, size.GetValueOrDefault(), offset.GetValueOrDefault(), genre, fromYear, toYear, musicFolderId, true, cancellationToken).ConfigureAwait(false);
                    break;
            }

            return albumMediaBundles;
        }

        private async Task SetDipositionCollectionIdAsync(Disposition disposition, Guid id, Guid userId, CancellationToken cancellationToken)
        {
            switch (disposition.MediaType)
            {
                case Data.Models.MediaType.Album:
                    var album = await MediaLibrary.GetAlbumAsync(userId, id, false, cancellationToken).ConfigureAwait(false);
                    disposition.CollectionId = album.Media.CollectionId;
                    break;

                case Data.Models.MediaType.Artist:
                    var artist = await MediaLibrary.GetArtistAsync(userId, id, cancellationToken).ConfigureAwait(false);
                    disposition.CollectionId = artist.Media.CollectionId;
                    break;

                case Data.Models.MediaType.Track:
                    var track = await MediaLibrary.GetTrackAsync(userId, id, false, cancellationToken).ConfigureAwait(false);
                    disposition.CollectionId = track.Media.CollectionId;
                    break;
            }
        }
    }
}