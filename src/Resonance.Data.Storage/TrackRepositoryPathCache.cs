using Resonance.Data.Media.Common;
using Resonance.Data.Models;
using System;

namespace Resonance.Data.Storage
{
    public class TrackRepositoryPathCache : RepositoryCacheItem<MediaBundle<Track>>
    {
        public TrackRepositoryPathCache(IMetadataRepository metadataRepository, IMetadataRepositoryCache metadataRepositoryCache, ITagReaderFactory tagReaderFactory, Guid userId, string path, Guid collectionId, bool populate, bool updateCollection)
        {
            var repositoryDelegate = new TrackRepositoryPathDelegate(userId, path, collectionId, populate, updateCollection);
            repositoryDelegate.Method = repositoryDelegate.CreateMethod(metadataRepository, metadataRepositoryCache, tagReaderFactory);
            RepositoryDelegate = repositoryDelegate;
        }
    }
}