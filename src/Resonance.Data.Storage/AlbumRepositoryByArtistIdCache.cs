using Resonance.Data.Models;
using System;
using System.Collections.Generic;

namespace Resonance.Data.Storage
{
    public class AlbumRepositoryByArtistIdCache : RepositoryCacheItem<IEnumerable<MediaBundle<Album>>>
    {
        public AlbumRepositoryByArtistIdCache(IMetadataRepository metadataRepository, Guid userId, Guid artistId, bool populate)
        {
            var repositoryDelegate = new AlbumRepositoryByArtistIdDelegate(userId, artistId, populate);
            repositoryDelegate.Method = repositoryDelegate.CreateMethod(metadataRepository);
            RepositoryDelegate = repositoryDelegate;
        }
    }
}