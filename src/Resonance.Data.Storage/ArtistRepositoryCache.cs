using Resonance.Data.Models;
using System;

namespace Resonance.Data.Storage
{
    public class ArtistRepositoryCache : RepositoryCacheItem<MediaBundle<Artist>>
    {
        public ArtistRepositoryCache(IMetadataRepository metadataRepository, Guid userId, string artist, Guid collectionId)
        {
            var repositoryDelegate = new ArtistRepositoryDelegate(userId, artist, collectionId);
            repositoryDelegate.Method = repositoryDelegate.CreateMethod(metadataRepository);
            RepositoryDelegate = repositoryDelegate;
        }
    }
}