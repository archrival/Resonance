using Resonance.Common;
using Resonance.Data.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Resonance.Data.Storage
{
    public class PlaylistsRepositoryIdDelegate : RepositoryCacheDelegate<IEnumerable<Playlist>>
    {
        public PlaylistsRepositoryIdDelegate(Guid userId, string username, bool getTracks)
        {
            UserId = userId;
            Username = username;
            GetTracks = getTracks;
        }

        private bool GetTracks { get; }
        private Guid UserId { get; }
        private string Username { get; }

        public Func<CancellationToken, Task<IEnumerable<Playlist>>> CreateMethod(IMetadataRepository metadataRepository)
        {
            return async cancellationToken => await metadataRepository.GetPlaylistsAsync(UserId, Username, GetTracks, cancellationToken).ConfigureAwait(false);
        }

        #region HashCode and Equality Overrides

        public static bool operator !=(PlaylistsRepositoryIdDelegate left, PlaylistsRepositoryIdDelegate right)
        {
            return !(left == right);
        }

        public static bool operator ==(PlaylistsRepositoryIdDelegate left, PlaylistsRepositoryIdDelegate right)
        {
            if (ReferenceEquals(null, left))
                return ReferenceEquals(null, right);

            if (ReferenceEquals(null, right))
                return false;

            var propertiesEqual = left.PropertiesEqual(right, nameof(GetTracks), nameof(UserId), nameof(Username));

            return propertiesEqual;
        }

        public override bool Equals(object obj)
        {
            var equals = obj != null && Equals(obj as PlaylistsRepositoryIdDelegate);

            return equals;
        }

        public override int GetHashCode()
        {
            var hashCode = this.GetHashCodeForObject(GetTracks, UserId, Username);

            return hashCode;
        }

        private bool Equals(PlaylistsRepositoryIdDelegate item)
        {
            var equals = item != null && this == item;

            return equals;
        }

        #endregion HashCode and Equality Overrides
    }
}