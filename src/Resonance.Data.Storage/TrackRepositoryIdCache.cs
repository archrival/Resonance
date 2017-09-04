using Resonance.Data.Models;
using System;

namespace Resonance.Data.Storage
{
    public class TrackRepositoryIdCache : RepositoryCacheItem<MediaBundle<Track>>
    {
        public TrackRepositoryIdCache(IMetadataRepository metadataRepository, Guid userId, Guid id, bool populate)
        {
            var repositoryDelegate = new TrackRepositoryIdDelegate(userId, id, populate);
            repositoryDelegate.Method = repositoryDelegate.CreateMethod(metadataRepository);
            RepositoryDelegate = repositoryDelegate;
        }
    }
}