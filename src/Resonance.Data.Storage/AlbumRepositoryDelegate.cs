using Resonance.Common;
using Resonance.Data.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Resonance.Data.Storage
{
    public class AlbumRepositoryDelegate : RepositoryCacheDelegate<MediaBundle<Album>>
    {
        public AlbumRepositoryDelegate(Guid userId, HashSet<Artist> artists, string name, Guid collectionId, bool populate)
        {
            UserId = userId;
            Artists = artists;
            Name = name;
            CollectionId = collectionId;
            Populate = populate;
        }

        private HashSet<Artist> Artists { get; }
        private Guid CollectionId { get; }
        private string Name { get; }
        private bool Populate { get; }
        private Guid UserId { get; }

        public Func<CancellationToken, Task<MediaBundle<Album>>> CreateMethod(IMetadataRepository metadataRepository)
        {
            return async cancelToken =>
            {
                var mediaBundle = await metadataRepository.GetAlbumAsync(UserId, Artists, Name, CollectionId, Populate, cancelToken);

                Album album = null;

                if (mediaBundle != null)
                {
                    album = mediaBundle.Media;
                }

                if (album != null)
                    return mediaBundle;

                var now = DateTime.UtcNow;

                var artistMediaBundles = new HashSet<MediaBundle<Artist>>();

                foreach (var artist in Artists)
                {
                    var artistMediaBundle = new MediaBundle<Artist>
                    {
                        Media = artist
                    };

                    artistMediaBundles.Add(artistMediaBundle);
                }

                album = new Album
                {
                    Artists = artistMediaBundles,
                    Name = Name,
                    CollectionId = CollectionId,
                    DateAdded = now,
                    DateModified = now
                };

                await metadataRepository.InsertOrUpdateAlbumAsync(album, cancelToken);

                mediaBundle = new MediaBundle<Album>
                {
                    Media = album,
                    Dispositions = new List<Disposition>(),
                    Playback = new List<Playback>()
                };

                return mediaBundle;
            };
        }

        #region HashCode and Equality Overrides

        public static bool operator !=(AlbumRepositoryDelegate left, AlbumRepositoryDelegate right)
        {
            return !(left == right);
        }

        public static bool operator ==(AlbumRepositoryDelegate left, AlbumRepositoryDelegate right)
        {
            if (ReferenceEquals(null, left))
                return ReferenceEquals(null, right);

            if (ReferenceEquals(null, right))
                return false;

            return left.PropertiesEqual(right, nameof(Populate), nameof(CollectionId), nameof(UserId), nameof(Name), nameof(Artists));
        }

        public override bool Equals(object obj)
        {
            return obj != null && Equals(obj as AlbumRepositoryDelegate);
        }

        public override int GetHashCode()
        {
            return this.GetHashCodeForObject(Populate, CollectionId, UserId, Name, Artists);
        }

        private bool Equals(AlbumRepositoryDelegate item)
        {
            return item != null && this == item;
        }

        #endregion HashCode and Equality Overrides
    }
}