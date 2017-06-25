using Resonance.Common;
using Resonance.Data.Media.Common;
using Resonance.Data.Models;
using Resonance.Data.Storage.Common;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Resonance.Data.Storage
{
    public class TrackRepositoryArtistAndTrackDelegate<TTagReader> : RepositoryCacheDelegate<MediaBundle<Track>> where TTagReader : ITagReader, new()
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

        public Func<CancellationToken, Task<MediaBundle<Track>>> CreateMethod(IMetadataRepository metadataRepository, ITagReaderFactory tagReaderFactory, IMediaLibrary mediaLibrary)
        {
            return async cancellationToken => await metadataRepository.GetTrackAsync(UserId, Artist, Track, CollectionId, Populate, cancellationToken);
        }

        #region HashCode and Equality Overrides

        public static bool operator !=(TrackRepositoryArtistAndTrackDelegate<TTagReader> left, TrackRepositoryArtistAndTrackDelegate<TTagReader> right)
        {
            return !(left == right);
        }

        public static bool operator ==(TrackRepositoryArtistAndTrackDelegate<TTagReader> left, TrackRepositoryArtistAndTrackDelegate<TTagReader> right)
        {
            if (ReferenceEquals(null, left))
                return ReferenceEquals(null, right);

            if (ReferenceEquals(null, right))
                return false;

            return left.PropertiesEqual(right, nameof(Populate), nameof(CollectionId), nameof(UserId), nameof(Artist), nameof(Track));
        }

        public override bool Equals(object obj)
        {
            return obj != null && Equals(obj as TrackRepositoryArtistAndTrackDelegate<TTagReader>);
        }

        public override int GetHashCode()
        {
            return this.GetHashCodeForObject(Populate, CollectionId, UserId, Artist, Track);
        }

        private bool Equals(TrackRepositoryArtistAndTrackDelegate<TTagReader> item)
        {
            return item != null && this == item;
        }

        #endregion HashCode and Equality Overrides
    }
}