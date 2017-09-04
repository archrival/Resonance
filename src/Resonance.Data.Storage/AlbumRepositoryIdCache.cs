using Resonance.Data.Models;
using System;

namespace Resonance.Data.Storage
{
    public class AlbumRepositoryIdCache : RepositoryCacheItem<MediaBundle<Album>>
    {
        public AlbumRepositoryIdCache(IMetadataRepository metadataRepository, Guid userId, Guid id, bool populate)
        {
            var repositoryDelegate = new AlbumRepositoryIdDelegate(userId, id, populate);
            repositoryDelegate.Method = repositoryDelegate.CreateMethod(metadataRepository);
            RepositoryDelegate = repositoryDelegate;
        }
    }
}