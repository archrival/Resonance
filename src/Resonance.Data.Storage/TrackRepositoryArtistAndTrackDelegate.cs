using Resonance.Common;
using Resonance.Data.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Resonance.Data.Storage
{
    public class TrackRepositoryArtistAndTrackDelegate : RepositoryCacheDelegate<MediaBundle<Track>>
    {
        public TrackRepositoryArtistAndTrackDelegate(Guid userId, string artist, string track, Guid? collectionId, bool populate)
        {
            UserId = userId;
            Artist = artist;
            Track = track;
            CollectionId = collectionId;
            Populate = populate;
        }

        private string Artist { get; }
        private Guid? CollectionId { get; }
        private bool Populate { get; }
        private string Track { get; }
        private Guid UserId { get; }

        public Func<CancellationToken, Task<MediaBundle<Track>>> CreateMethod(IMetadataRepository metadataRepository)
        {
            return async cancellationToken => await metadataRepository.GetTrackAsync(UserId, Artist, Track, CollectionId, Populate, cancellationToken);
        }

        #region HashCode and Equality Overrides

        public static bool operator !=(TrackRepositoryArtistAndTrackDelegate left, TrackRepositoryArtistAndTrackDelegate right)
        {
            return !(left == right);
        }

        public static bool operator ==(TrackRepositoryArtistAndTrackDelegate left, TrackRepositoryArtistAndTrackDelegate right)
        {
            if (ReferenceEquals(null, left))
                return ReferenceEquals(null, right);

            if (ReferenceEquals(null, right))
                return false;

            return left.PropertiesEqual(right, nameof(Populate), nameof(CollectionId), nameof(UserId), nameof(Artist), nameof(Track));
        }

        public override bool Equals(object obj)
        {
            return obj != null && Equals(obj as TrackRepositoryArtistAndTrackDelegate);
        }

        public override int GetHashCode()
        {
            return this.GetHashCodeForObject(Populate, CollectionId, UserId, Artist, Track);
        }

        private bool Equals(TrackRepositoryArtistAndTrackDelegate item)
        {
            return item != null && this == item;
        }

        #endregion HashCode and Equality Overrides
    }
}