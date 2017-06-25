using Resonance.Data.Media.Common;
using Resonance.Data.Models;
using Resonance.Data.Storage.Common;
using System;

namespace Resonance.Data.Storage
{
    public class AlbumRepositoryIdCache<TTagReader> : RepositoryCacheItem<MediaBundle<Album>> where TTagReader : ITagReader, new()
    {
        public AlbumRepositoryIdCache(IMetadataRepository metadataRepository, ITagReaderFactory tagReaderFactory, IMediaLibrary mediaLibrary, Guid userId, Guid id, bool populate)
        {
            var repositoryDelegate = new AlbumRepositoryIdDelegate<TTagReader>(userId, id, populate);
            repositoryDelegate.Method = repositoryDelegate.CreateMethod(metadataRepository, tagReaderFactory, mediaLibrary);
            RepositoryDelegate = repositoryDelegate;
        }
    }
}