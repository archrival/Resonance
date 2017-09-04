using Resonance.Data.Models;
using System;
using System.Collections.Generic;

namespace Resonance.Data.Storage
{
    public class PlaylistsRepositoryIdCache : RepositoryCacheItem<IEnumerable<Playlist>>
    {
        public PlaylistsRepositoryIdCache(IMetadataRepository metadataRepository, Guid userId, string username, bool getTracks)
        {
            var repositoryDelegate = new PlaylistsRepositoryIdDelegate(userId, username, getTracks);
            repositoryDelegate.Method = repositoryDelegate.CreateMethod(metadataRepository);
            RepositoryDelegate = repositoryDelegate;
        }
    }
}