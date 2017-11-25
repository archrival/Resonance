using Resonance.Data.Models;

namespace Resonance.Data.Media.Common
{
    public interface ICoverArtRepository
    {
        CoverArt GetCoverArt(Track track, int? size);
    }
}