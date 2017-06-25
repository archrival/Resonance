using Resonance.Common;
using Resonance.Data.Media.Common;
using Resonance.Data.Models;
using Resonance.Data.Storage.Common;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Resonance.Data.Storage
{
    public class PlaylistRepositoryIdDelegate<TTagReader> : RepositoryCacheDelegate<Playlist> where TTagReader : ITagReader, new()
    {
        public PlaylistRepositoryIdDelegate(Guid userId, Guid id, bool getTracks)
        {
            UserId = userId;
            Id = id;
            GetTracks = getTracks;
        }

        private bool GetTracks { get; }
        private Guid Id { get; }
        private Guid UserId { get; }

        public Func<CancellationToken, Task<Playlist>> CreateMethod(IMetadataRepository metadataRepository, ITagReaderFactory tagReaderFactory, IMediaLibrary mediaLibrary)
        {
            return async cancellationToken => await metadataRepository.GetPlaylistAsync(UserId, Id, GetTracks, cancellationToken);
        }

        #region HashCode and Equality Overrides

        public static bool operator !=(PlaylistRepositoryIdDelegate<TTagReader> left, PlaylistRepositoryIdDelegate<TTagReader> right)
        {
            return !(left == right);
        }

        public static bool operator ==(PlaylistRepositoryIdDelegate<TTagReader> left, PlaylistRepositoryIdDelegate<TTagReader> right)
        {
            if (ReferenceEquals(null, left))
                return ReferenceEquals(null, right);

            if (ReferenceEquals(null, right))
                return false;

            return left.PropertiesEqual(right, nameof(GetTracks), nameof(UserId), nameof(Id));
        }

        public override bool Equals(object obj)
        {
            return obj != null && Equals(obj as PlaylistRepositoryIdDelegate<TTagReader>);
        }

        public override int GetHashCode()
        {
            return this.GetHashCodeForObject(GetTracks, UserId, Id);
        }

        private bool Equals(PlaylistRepositoryIdDelegate<TTagReader> item)
        {
            return item != null && this == item;
        }

        #endregion HashCode and Equality Overrides
    }
}