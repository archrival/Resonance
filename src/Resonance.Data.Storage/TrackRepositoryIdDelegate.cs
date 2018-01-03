using Resonance.Common;
using Resonance.Data.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Resonance.Data.Storage
{
    public class TrackRepositoryIdDelegate : RepositoryCacheDelegate<MediaBundle<Track>>
    {
        public TrackRepositoryIdDelegate(Guid userId, Guid id, bool populate)
        {
            UserId = userId;
            Id = id;
            Populate = populate;
        }

        private Guid Id { get; }
        private bool Populate { get; }
        private Guid UserId { get; }

        public Func<CancellationToken, Task<MediaBundle<Track>>> CreateMethod(IMetadataRepository metadataRepository)
        {
            return cancellationToken => metadataRepository.GetTrackAsync(UserId, Id, Populate, cancellationToken);
        }

        #region HashCode and Equality Overrides

        public static bool operator !=(TrackRepositoryIdDelegate left, TrackRepositoryIdDelegate right)
        {
            return !(left == right);
        }

        public static bool operator ==(TrackRepositoryIdDelegate left, TrackRepositoryIdDelegate right)
        {
            if (left is null)
                return right is null;

            if (right is null)
                return false;

            return left.PropertiesEqual(right, nameof(Populate), nameof(UserId), nameof(Id));
        }

        public override bool Equals(object obj)
        {
            return obj != null && Equals(obj as TrackRepositoryIdDelegate);
        }

        public override int GetHashCode()
        {
            return this.GetHashCodeForObject(Populate, UserId, Id);
        }

        private bool Equals(TrackRepositoryIdDelegate item)
        {
            return item != null && this == item;
        }

        #endregion HashCode and Equality Overrides
    }
}