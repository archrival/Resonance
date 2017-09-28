using Resonance.Common;
using Resonance.Data.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Resonance.Data.Storage
{
    public class PlaylistRepositoryIdDelegate : RepositoryCacheDelegate<Playlist>
    {
        public PlaylistRepositoryIdDelegate(Guid userId, Guid id, bool getTracks)
        {
            UserId = userId;
            Id = id;
            GetTracks = getTracks;
        }

        private bool GetTracks { get; }
        private Guid Id { get; }
        private Guid UserId { get; }

        public Func<CancellationToken, Task<Playlist>> CreateMethod(IMetadataRepository metadataRepository)
        {
            return async cancellationToken => await metadataRepository.GetPlaylistAsync(UserId, Id, GetTracks, cancellationToken).ConfigureAwait(false);
        }

        #region HashCode and Equality Overrides

        public static bool operator !=(PlaylistRepositoryIdDelegate left, PlaylistRepositoryIdDelegate right)
        {
            return !(left == right);
        }

        public static bool operator ==(PlaylistRepositoryIdDelegate left, PlaylistRepositoryIdDelegate right)
        {
            if (ReferenceEquals(null, left))
                return ReferenceEquals(null, right);

            if (ReferenceEquals(null, right))
                return false;

            return left.PropertiesEqual(right, nameof(GetTracks), nameof(UserId), nameof(Id));
        }

        public override bool Equals(object obj)
        {
            return obj != null && Equals(obj as PlaylistRepositoryIdDelegate);
        }

        public override int GetHashCode()
        {
            return this.GetHashCodeForObject(GetTracks, UserId, Id);
        }

        private bool Equals(PlaylistRepositoryIdDelegate item)
        {
            return item != null && this == item;
        }

        #endregion HashCode and Equality Overrides
    }
}