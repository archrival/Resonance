using Resonance.Data.Models;
using System;

namespace Resonance.Data.Storage
{
    public class ArtistRepositoryIdCache : RepositoryCacheItem<MediaBundle<Artist>>
    {
        public ArtistRepositoryIdCache(IMetadataRepository metadataRepository, Guid userId, Guid id)
        {
            var repositoryDelegate = new ArtistRepositoryIdDelegate(userId, id);
            repositoryDelegate.Method = repositoryDelegate.CreateMethod(metadataRepository);
            RepositoryDelegate = repositoryDelegate;
        }
    }
}