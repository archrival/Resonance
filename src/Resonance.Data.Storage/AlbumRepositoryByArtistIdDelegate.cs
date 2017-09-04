using Resonance.Common;
using Resonance.Data.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Resonance.Data.Storage
{
    public class AlbumRepositoryByArtistIdDelegate : RepositoryCacheDelegate<IEnumerable<MediaBundle<Album>>>
    {
        public AlbumRepositoryByArtistIdDelegate(Guid userId, Guid artistId, bool populate)
        {
            UserId = userId;
            ArtistId = artistId;
            Populate = populate;
        }

        private Guid ArtistId { get; }
        private bool Populate { get; }
        private Guid UserId { get; }

        public Func<CancellationToken, Task<IEnumerable<MediaBundle<Album>>>> CreateMethod(IMetadataRepository metadataRepository)
        {
            return async cancellationToken => await metadataRepository.GetAlbumsByArtistAsync(UserId, ArtistId, Populate, cancellationToken);
        }

        #region HashCode and Equality Overrides

        public static bool operator !=(AlbumRepositoryByArtistIdDelegate left, AlbumRepositoryByArtistIdDelegate right)
        {
            return !(left == right);
        }

        public static bool operator ==(AlbumRepositoryByArtistIdDelegate left, AlbumRepositoryByArtistIdDelegate right)
        {
            if (ReferenceEquals(null, left))
                return ReferenceEquals(null, right);

            if (ReferenceEquals(null, right))
                return false;

            return left.PropertiesEqual(right, nameof(Populate), nameof(UserId), nameof(ArtistId));
        }

        public override bool Equals(object obj)
        {
            return obj != null && Equals(obj as AlbumRepositoryByArtistIdDelegate);
        }

        public override int GetHashCode()
        {
            return this.GetHashCodeForObject(Populate, UserId, ArtistId);
        }

        private bool Equals(AlbumRepositoryByArtistIdDelegate item)
        {
            return item != null && this == item;
        }

        #endregion HashCode and Equality Overrides
    }
}