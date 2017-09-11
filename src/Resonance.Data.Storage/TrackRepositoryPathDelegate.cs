using Resonance.Common;
using Resonance.Data.Media.Common;
using Resonance.Data.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Resonance.Data.Storage
{
    public class TrackRepositoryPathDelegate : RepositoryCacheDelegate<MediaBundle<Track>>
    {
        public TrackRepositoryPathDelegate(Guid userId, string path, Guid collectionId, bool populate, bool updateCollection)
        {
            UserId = userId;
            Path = path;
            CollectionId = collectionId;
            Populate = populate;
            UpdateCollection = updateCollection;
        }

        private Guid CollectionId { get; }
        private string Path { get; }
        private bool Populate { get; }
        private bool UpdateCollection { get; }
        private Guid UserId { get; }

        public Func<CancellationToken, Task<MediaBundle<Track>>> CreateMethod(IMetadataRepository metadataRepository, IMetadataRepositoryCache metadataRepositoryCache, ITagReaderFactory tagReaderFactory)
        {
            return async cancellationToken =>
            {
                var mediaBundle = await metadataRepository.GetTrackAsync(UserId, Path, CollectionId, Populate, cancellationToken).ConfigureAwait(false);

                Track track = null;

                if (mediaBundle != null)
                {
                    track = mediaBundle.Media;
                }
                else if (!UpdateCollection)
                {
                    return null;
                }

                if (track != null && track.DateFileModified.ToUniversalTime() >= File.GetLastWriteTimeUtc(Path))
                {
                    return mediaBundle;
                }

                var now = DateTime.UtcNow;
                var dateAdded = now;
                Guid? trackId = null;

                if (track != null)
                {
                    trackId = track.Id;
                    dateAdded = track.DateAdded;
                }

                var tagReader = tagReaderFactory.CreateTagReader(Path);

                track = await metadataRepositoryCache.TagReaderToTrackModelAsync(UserId, tagReader, CollectionId, cancellationToken).ConfigureAwait(false);

                if (trackId.HasValue)
                {
                    track.Id = trackId.Value;
                }

                track.CollectionId = CollectionId;
                track.DateAdded = dateAdded;
                track.DateModified = now;
                track.Visible = true;

                await metadataRepository.InsertOrUpdateTrackAsync(track, cancellationToken).ConfigureAwait(false);

                mediaBundle = new MediaBundle<Track>
                {
                    Media = track,
                    Dispositions = new List<Disposition>(),
                    Playback = new List<Playback>()
                };

                return mediaBundle;
            };
        }

        #region HashCode and Equality Overrides

        public static bool operator !=(TrackRepositoryPathDelegate left, TrackRepositoryPathDelegate right)
        {
            return !(left == right);
        }

        public static bool operator ==(TrackRepositoryPathDelegate left, TrackRepositoryPathDelegate right)
        {
            if (ReferenceEquals(null, left))
                return ReferenceEquals(null, right);

            if (ReferenceEquals(null, right))
                return false;

            return left.PropertiesEqual(right, nameof(Populate), nameof(CollectionId), nameof(UserId), nameof(Path));
        }

        public override bool Equals(object obj)
        {
            return obj != null && Equals(obj as TrackRepositoryPathDelegate);
        }

        public override int GetHashCode()
        {
            return this.GetHashCodeForObject(Populate, CollectionId, UserId, Path);
        }

        private bool Equals(TrackRepositoryPathDelegate item)
        {
            return item != null && this == item;
        }

        #endregion HashCode and Equality Overrides
    }
}