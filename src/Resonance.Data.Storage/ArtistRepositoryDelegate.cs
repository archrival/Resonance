using Resonance.Common;
using Resonance.Data.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Resonance.Data.Storage
{
    public class ArtistRepositoryDelegate : RepositoryCacheDelegate<MediaBundle<Artist>>
    {
        public ArtistRepositoryDelegate(Guid userId, string artist, Guid collectionId)
        {
            UserId = userId;
            Artist = artist;
            CollectionId = collectionId;
        }

        private string Artist { get; }
        private Guid CollectionId { get; }
        private Guid UserId { get; }

        public Func<CancellationToken, Task<MediaBundle<Artist>>> CreateMethod(IMetadataRepository metadataRepository)
        {
            return async cancelToken =>
            {
                var mediaBundle = await metadataRepository.GetArtistAsync(UserId, Artist, CollectionId, cancelToken).ConfigureAwait(false);

                Artist artist = null;

                if (mediaBundle != null)
                {
                    artist = mediaBundle.Media;
                }

                if (artist != null)
                {
                    return mediaBundle;
                }

                var now = DateTime.UtcNow;

                artist = new Artist
                {
                    Name = Artist,
                    CollectionId = CollectionId,
                    DateAdded = now,
                    DateModified = now
                };

                await metadataRepository.InsertOrUpdateArtistAsync(artist, cancelToken).ConfigureAwait(false);

                mediaBundle = new MediaBundle<Artist>
                {
                    Media = artist,
                    Dispositions = new List<Disposition>(),
                    Playback = new List<Playback>()
                };

                return mediaBundle;
            };
        }

        #region HashCode and Equality Overrides

        public static bool operator !=(ArtistRepositoryDelegate left, ArtistRepositoryDelegate right)
        {
            return !(left == right);
        }

        public static bool operator ==(ArtistRepositoryDelegate left, ArtistRepositoryDelegate right)
        {
            if (left is null)
                return right is null;

            if (right is null)
                return false;

            return left.PropertiesEqual(right, nameof(CollectionId), nameof(UserId), nameof(Artist));
        }

        public override bool Equals(object obj)
        {
            return obj != null && Equals(obj as ArtistRepositoryDelegate);
        }

        public override int GetHashCode()
        {
            return this.GetHashCodeForObject(CollectionId, UserId, Artist);
        }

        private bool Equals(ArtistRepositoryDelegate item)
        {
            return item != null && this == item;
        }

        #endregion HashCode and Equality Overrides
    }
}