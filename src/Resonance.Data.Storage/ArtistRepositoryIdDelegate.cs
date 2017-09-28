using Resonance.Common;
using Resonance.Data.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Resonance.Data.Storage
{
    public class ArtistRepositoryIdDelegate : RepositoryCacheDelegate<MediaBundle<Artist>>
    {
        public ArtistRepositoryIdDelegate(Guid userId, Guid id)
        {
            UserId = userId;
            Id = id;
        }

        private Guid Id { get; }
        private Guid UserId { get; }

        public Func<CancellationToken, Task<MediaBundle<Artist>>> CreateMethod(IMetadataRepository metadataRepository)
        {
            return async cancelToken => await metadataRepository.GetArtistAsync(UserId, Id, cancelToken).ConfigureAwait(false);
        }

        #region HashCode and Equality Overrides

        public static bool operator !=(ArtistRepositoryIdDelegate left, ArtistRepositoryIdDelegate right)
        {
            return !(left == right);
        }

        public static bool operator ==(ArtistRepositoryIdDelegate left, ArtistRepositoryIdDelegate right)
        {
            if (ReferenceEquals(null, left))
                return ReferenceEquals(null, right);

            if (ReferenceEquals(null, right))
                return false;

            return left.PropertiesEqual(right, nameof(UserId), nameof(Id));
        }

        public override bool Equals(object obj)
        {
            return obj != null && Equals(obj as ArtistRepositoryIdDelegate);
        }

        public override int GetHashCode()
        {
            return this.GetHashCodeForObject(UserId, Id);
        }

        private bool Equals(ArtistRepositoryIdDelegate item)
        {
            return item != null && this == item;
        }

        #endregion HashCode and Equality Overrides
    }
}