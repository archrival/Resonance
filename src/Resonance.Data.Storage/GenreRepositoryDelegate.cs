using Resonance.Common;
using Resonance.Data.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Resonance.Data.Storage
{
    public class GenreRepositoryDelegate : RepositoryCacheDelegate<Genre>
    {
        public GenreRepositoryDelegate(string genre, Guid collectionId)
        {
            Genre = genre;
            CollectionId = collectionId;
        }

        private Guid CollectionId { get; }
        private string Genre { get; }

        public Func<CancellationToken, Task<Genre>> CreateMethod(IMetadataRepository metadataRepository)
        {
            return async cancellationToken =>
            {
                var genre = await metadataRepository.GetGenreAsync(Genre, CollectionId, cancellationToken).ConfigureAwait(false);

                if (genre != null)
                {
                    return genre;
                }

                var now = DateTime.UtcNow;

                genre = new Genre
                {
                    Name = Genre,
                    CollectionId = CollectionId,
                    DateAdded = now,
                    DateModified = now
                };

                await metadataRepository.InsertOrUpdateGenreAsync(genre, cancellationToken);

                return genre;
            };
        }

        #region HashCode and Equality Overrides

        public static bool operator !=(GenreRepositoryDelegate left, GenreRepositoryDelegate right)
        {
            return !(left == right);
        }

        public static bool operator ==(GenreRepositoryDelegate left, GenreRepositoryDelegate right)
        {
            if (ReferenceEquals(null, left))
                return ReferenceEquals(null, right);

            if (ReferenceEquals(null, right))
                return false;

            return left.PropertiesEqual(right, nameof(CollectionId), nameof(Genre));
        }

        public override bool Equals(object obj)
        {
            return obj != null && Equals(obj as GenreRepositoryDelegate);
        }

        public override int GetHashCode()
        {
            return this.GetHashCodeForObject(CollectionId, Genre);
        }

        private bool Equals(GenreRepositoryDelegate item)
        {
            return item != null && this == item;
        }

        #endregion HashCode and Equality Overrides
    }
}