using Resonance.Data.Media.Common;
using Resonance.Data.Models;
using Resonance.Data.Storage.Common;
using System;

namespace Resonance.Data.Storage
{
    public class PlaylistRepositoryIdCache<TTagReader> : RepositoryCacheItem<Playlist> where TTagReader : ITagReader, new()
    {
        public PlaylistRepositoryIdCache(IMetadataRepository metadataRepository, ITagReaderFactory tagReaderFactory, IMediaLibrary mediaLibrary, Guid userId, Guid id, bool getTracks)
        {
            var repositoryDelegate = new PlaylistRepositoryIdDelegate<TTagReader>(userId, id, getTracks);
            repositoryDelegate.Method = repositoryDelegate.CreateMethod(metadataRepository, tagReaderFactory, mediaLibrary);
            RepositoryDelegate = repositoryDelegate;
        }
    }
}