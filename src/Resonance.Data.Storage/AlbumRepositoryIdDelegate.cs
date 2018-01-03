using Resonance.Common;
using Resonance.Data.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Resonance.Data.Storage
{
    public class AlbumRepositoryIdDelegate : RepositoryCacheDelegate<MediaBundle<Album>>
    {
        public AlbumRepositoryIdDelegate(Guid userId, Guid id, bool populate)
        {
            UserId = userId;
            Id = id;
            Populate = populate;
        }

        private Guid Id { get; }
        private bool Populate { get; }
        private Guid UserId { get; }

        public Func<CancellationToken, Task<MediaBundle<Album>>> CreateMethod(IMetadataRepository metadataRepository)
        {
            return cancelToken => metadataRepository.GetAlbumAsync(UserId, Id, Populate, cancelToken);
        }

        #region HashCode and Equality Overrides

        public static bool operator !=(AlbumRepositoryIdDelegate left, AlbumRepositoryIdDelegate right)
        {
            return !(left == right);
        }

        public static bool operator ==(AlbumRepositoryIdDelegate left, AlbumRepositoryIdDelegate right)
        {
            if (left is null)
                return right is null;

            if (right is null)
                return false;

            return left.PropertiesEqual(right, nameof(Populate), nameof(UserId), nameof(Id));
        }

        public override bool Equals(object obj)
        {
            return obj != null && Equals(obj as AlbumRepositoryIdDelegate);
        }

        public override int GetHashCode()
        {
            return this.GetHashCodeForObject(Populate, UserId, Id);
        }

        private bool Equals(AlbumRepositoryIdDelegate item)
        {
            return item != null && this == item;
        }

        #endregion HashCode and Equality Overrides
    }
}