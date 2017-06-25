using Resonance.Data.Models;
using System;

namespace Resonance.Data.Storage
{
    public class GenreRepositoryCache : RepositoryCacheItem<Genre>
    {
        public GenreRepositoryCache(IMetadataRepository metadataRepository, string genre, Guid collectionId)
        {
            var repositoryDelegate = new GenreRepositoryDelegate(genre, collectionId);
            repositoryDelegate.Method = repositoryDelegate.CreateMethod(metadataRepository);
            RepositoryDelegate = repositoryDelegate;
        }
    }
}