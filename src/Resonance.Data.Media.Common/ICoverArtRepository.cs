using Resonance.Data.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Resonance.Data.Media.Common
{
    public interface ICoverArtRepository
    {
        Task<CoverArt> GetCoverArt(Track track, int? size, CancellationToken cancellationToken);
    }
}