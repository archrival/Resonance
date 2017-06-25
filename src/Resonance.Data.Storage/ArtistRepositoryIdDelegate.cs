using Resonance.Common;
using Resonance.Data.Media.Common;
using Resonance.Data.Models;
using Resonance.Data.Storage.Common;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Resonance.Data.Storage
{
    public class ArtistRepositoryIdDelegate<TTagReader> : RepositoryCacheDelegate<MediaBundle<Artist>> where TTagReader : ITagReader, new()
    {
        public ArtistRepositoryIdDelegate(Guid userId, Guid id)
        {
            UserId = userId;
            Id = id;
        }

        private Guid Id { get; }
        private Guid UserId { get; }

        public Func<CancellationToken, Task<MediaBundle<Artist>>> CreateMethod(IMetadataRepository metadataRepository, ITagReaderFactory tagReaderFactory, IMediaLibrary mediaLibrary)
        {
            return async cancelToken => await metadataRepository.GetArtistAsync(UserId, Id, cancelToken);
        }

        #region HashCode and Equality Overrides

        public static bool operator !=(ArtistRepositoryIdDelegate<TTagReader> left, ArtistRepositoryIdDelegate<TTagReader> right)
        {
            return !(left == right);
        }

        public static bool operator ==(ArtistRepositoryIdDelegate<TTagReader> left, ArtistRepositoryIdDelegate<TTagReader> right)
        {
            if (ReferenceEquals(null, left))
                return ReferenceEquals(null, right);

            if (ReferenceEquals(null, right))
                return false;

            return left.PropertiesEqual(right, nameof(UserId), nameof(Id));
        }

        public override bool Equals(object obj)
        {
            return obj != null && Equals(obj as ArtistRepositoryIdDelegate<TTagReader>);
        }

        public override int GetHashCode()
        {
            return this.GetHashCodeForObject(UserId, Id);
        }

        private bool Equals(ArtistRepositoryIdDelegate<TTagReader> item)
        {
            return item != null && this == item;
        }

        #endregion HashCode and Equality Overrides
    }
}