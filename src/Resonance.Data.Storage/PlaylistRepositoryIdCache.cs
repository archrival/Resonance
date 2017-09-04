using Resonance.Data.Models;
using System;

namespace Resonance.Data.Storage
{
    public class PlaylistRepositoryIdCache : RepositoryCacheItem<Playlist>
    {
        public PlaylistRepositoryIdCache(IMetadataRepository metadataRepository, Guid userId, Guid id, bool getTracks)
        {
            var repositoryDelegate = new PlaylistRepositoryIdDelegate(userId, id, getTracks);
            repositoryDelegate.Method = repositoryDelegate.CreateMethod(metadataRepository);
            RepositoryDelegate = repositoryDelegate;
        }
    }
}