using Resonance.Data.Media.Common;
using Resonance.Data.Models;
using Resonance.Data.Storage.Common;
using System;
using System.Collections.Generic;

namespace Resonance.Data.Storage
{
    public class PlaylistsRepositoryIdCache<TTagReader> : RepositoryCacheItem<IEnumerable<Playlist>> where TTagReader : ITagReader, new()
    {
        public PlaylistsRepositoryIdCache(IMetadataRepository metadataRepository, ITagReaderFactory tagReaderFactory, IMediaLibrary mediaLibrary, Guid userId, string username, bool getTracks)
        {
            var repositoryDelegate = new PlaylistsRepositoryIdDelegate<TTagReader>(userId, username, getTracks);
            repositoryDelegate.Method = repositoryDelegate.CreateMethod(metadataRepository, tagReaderFactory, mediaLibrary);
            RepositoryDelegate = repositoryDelegate;
        }
    }
}