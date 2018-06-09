using IF.Lastfm.Core.Api;
using IF.Lastfm.Core.Api.Enums;
using IF.Lastfm.Core.Objects;
using Resonance.Data.Media.Common;
using Resonance.Data.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Resonance.Data.Media.LastFm
{
    public class LastFmClient : ILastFmClient
    {
        private readonly LastfmClient _client;

        public LastFmClient(string apiKey, string apiSecret)
        {
            _client = new LastfmClient(apiKey, apiSecret);
        }

        public async Task<MediaInfo> GetAlbumInfoAsync(Album album, CancellationToken cancellationToken)
        {
            var artist = album.Artists.FirstOrDefault();

            if (artist == null)
            {
                return null;
            }

            var response = await _client.Album.GetInfoAsync(artist.Media.Name, album.Name).ConfigureAwait(false);

            if (response.Status != LastResponseStatus.Successful)
            {
                return null;
            }

            var albumInfo = response.Content;

            var mediaInfo = new MediaInfo
            {
                LastFm = new Models.LastFm
                {
                    LastFmId = albumInfo.Id,
                    Url = albumInfo.Url,
                    SmallImageUrl = albumInfo.Images.Small,
                    MediumImageUrl = albumInfo.Images.Medium,
                    LargeImageUrl = albumInfo.Images.Large,
                    LargestImageUrl = albumInfo.Images.Largest
                },
                MediaId = artist.Media.Id,
                MusicBrainzId = albumInfo.Mbid,
            };

            return mediaInfo;
        }

        public async Task<MediaInfo> GetArtistInfoAsync(Artist artist, CancellationToken cancellationToken)
        {
            var response = await _client.Artist.GetInfoAsync(artist.Name, "en", true).ConfigureAwait(false);

            if (response.Status != LastResponseStatus.Successful)
            {
                return null;
            }

            var artistInfo = response.Content;

            var mediaInfo = ConvertFromLastArtist(artistInfo);

            if (mediaInfo != null)
            {
                mediaInfo.MediaId = artist.Id;
            }

            return mediaInfo;
        }

        public async Task<IEnumerable<MediaInfo>> GetSimilarArtistsAsync(Artist artist, bool autocorrect, int limit, CancellationToken cancellationToken)
        {
            var similarArtists = new List<MediaInfo>();

            var response = await _client.Artist.GetSimilarAsync(artist.Name, autocorrect, limit).ConfigureAwait(false);

            if (response.Status != LastResponseStatus.Successful)
            {
                return similarArtists;
            }

            var artists = response.Content;

            similarArtists.AddRange(artists.Select(ConvertFromLastArtist).Where(mediaInfo => mediaInfo != null));

            return similarArtists;
        }

        public async Task<IEnumerable<MediaInfo>> GetTopTracksAsync(string artist, int count, CancellationToken cancellationToken)
        {
            var topSongs = new List<MediaInfo>();

            var response = await _client.Artist.GetTopTracksAsync(artist, true, 1, count).ConfigureAwait(false);

            if (response.Status != LastResponseStatus.Successful)
            {
                return topSongs;
            }

            topSongs.AddRange(response.Content.Select(ConvertFromLastTrack).Where(mediaInfo => mediaInfo != null));

            return topSongs;
        }

        private static MediaInfo ConvertFromLastArtist(LastArtist lastArtist)
        {
            var mediaInfo = new MediaInfo
            {
                LastFm = new Models.LastFm
                {
                    LastFmId = lastArtist.Id,
                    Url = lastArtist.Url,
                    Details = lastArtist.Bio?.Content,
                    Name = lastArtist.Name
                },
                MusicBrainzId = lastArtist.Mbid,
            };

            if (lastArtist.MainImage == null)
            {
                return mediaInfo;
            }

            mediaInfo.LastFm.SmallImageUrl = lastArtist.MainImage.Small;
            mediaInfo.LastFm.MediumImageUrl = lastArtist.MainImage.Medium;
            mediaInfo.LastFm.LargeImageUrl = lastArtist.MainImage.Large;
            mediaInfo.LastFm.LargestImageUrl = lastArtist.MainImage.Largest;

            return mediaInfo;
        }

        private static MediaInfo ConvertFromLastTrack(LastTrack lastTrack)
        {
            MediaInfo mediaInfo = null;

            if (!string.IsNullOrWhiteSpace(lastTrack.Mbid) || !string.IsNullOrWhiteSpace(lastTrack.Id))
            {
                mediaInfo = new MediaInfo
                {
                    LastFm = new Models.LastFm
                    {
                        LastFmId = lastTrack.Id,
                        Url = lastTrack.Url,
                        SmallImageUrl = lastTrack.Images.Small,
                        MediumImageUrl = lastTrack.Images.Medium,
                        LargeImageUrl = lastTrack.Images.Large,
                        LargestImageUrl = lastTrack.Images.Largest,
                        Name = lastTrack.Name
                    },
                    MusicBrainzId = lastTrack.Mbid,
                };
            }

            return mediaInfo;
        }
    }
}