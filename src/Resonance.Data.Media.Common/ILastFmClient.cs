using Resonance.Data.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Resonance.Data.Media.Common
{
    public interface ILastFmClient
    {
        Task<MediaInfo> GetAlbumInfoAsync(Album album, CancellationToken cancellationToken);

        Task<MediaInfo> GetArtistInfoAsync(Artist artist, CancellationToken cancellationToken);

        Task<IEnumerable<MediaInfo>> GetSimilarArtistsAsync(Artist artist, bool autocorrect, int limit, CancellationToken cancellationToken);

        Task<IEnumerable<MediaInfo>> GetTopTracksAsync(string artist, int count, CancellationToken cancellationToken);
    }
}