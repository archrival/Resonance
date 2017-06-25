using Resonance.Data.Models;
using System;
using System.Collections.Generic;

namespace Resonance.Data.Storage
{
    public class AlbumRepositoryCache : RepositoryCacheItem<MediaBundle<Album>>
    {
        public AlbumRepositoryCache(IMetadataRepository metadataRepository, Guid userId, HashSet<Artist> artists, string name, Guid collectionId, bool populate)
        {
            var repositoryDelegate = new AlbumRepositoryDelegate(userId, artists, name, collectionId, populate);
            repositoryDelegate.Method = repositoryDelegate.CreateMethod(metadataRepository);
            RepositoryDelegate = repositoryDelegate;
        }
    }
}