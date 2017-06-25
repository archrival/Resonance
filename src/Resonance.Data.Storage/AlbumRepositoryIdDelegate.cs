using Resonance.Common;
using Resonance.Data.Media.Common;
using Resonance.Data.Models;
using Resonance.Data.Storage.Common;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Resonance.Data.Storage
{
    public class AlbumRepositoryIdDelegate<TTagReader> : RepositoryCacheDelegate<MediaBundle<Album>> where TTagReader : ITagReader, new()
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

        public Func<CancellationToken, Task<MediaBundle<Album>>> CreateMethod(IMetadataRepository metadataRepository, ITagReaderFactory tagReaderFactory, IMediaLibrary mediaLibrary)
        {
            return async cancelToken => await metadataRepository.GetAlbumAsync(UserId, Id, Populate, cancelToken);
        }

        #region HashCode and Equality Overrides

        public static bool operator !=(AlbumRepositoryIdDelegate<TTagReader> left, AlbumRepositoryIdDelegate<TTagReader> right)
        {
            return !(left == right);
        }

        public static bool operator ==(AlbumRepositoryIdDelegate<TTagReader> left, AlbumRepositoryIdDelegate<TTagReader> right)
        {
            if (ReferenceEquals(null, left))
                return ReferenceEquals(null, right);

            if (ReferenceEquals(null, right))
                return false;

            return left.PropertiesEqual(right, nameof(Populate), nameof(UserId), nameof(Id));
        }

        public override bool Equals(object obj)
        {
            return obj != null && Equals(obj as AlbumRepositoryIdDelegate<TTagReader>);
        }

        public override int GetHashCode()
        {
            return this.GetHashCodeForObject(Populate, UserId, Id);
        }

        private bool Equals(AlbumRepositoryIdDelegate<TTagReader> item)
        {
            return item != null && this == item;
        }

        #endregion HashCode and Equality Overrides
    }
}