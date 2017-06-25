using Resonance.Data.Media.Common;
using Resonance.Data.Models;
using Resonance.Data.Storage.Common;
using System;

namespace Resonance.Data.Storage
{
    public class ArtistRepositoryIdCache<TTagReader> : RepositoryCacheItem<MediaBundle<Artist>> where TTagReader : ITagReader, new()
    {
        public ArtistRepositoryIdCache(IMetadataRepository metadataRepository, ITagReaderFactory tagReaderFactory, IMediaLibrary mediaLibrary, Guid userId, Guid id)
        {
            var repositoryDelegate = new ArtistRepositoryIdDelegate<TTagReader>(userId, id);
            repositoryDelegate.Method = repositoryDelegate.CreateMethod(metadataRepository, tagReaderFactory, mediaLibrary);
            RepositoryDelegate = repositoryDelegate;
        }
    }
}