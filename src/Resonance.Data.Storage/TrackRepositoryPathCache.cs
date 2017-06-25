using Resonance.Data.Media.Common;
using Resonance.Data.Models;
using Resonance.Data.Storage.Common;
using System;

namespace Resonance.Data.Storage
{
    public class TrackRepositoryPathCache<TTagReader> : RepositoryCacheItem<MediaBundle<Track>> where TTagReader : ITagReader, new()
    {
        public TrackRepositoryPathCache(IMetadataRepository metadataRepository, ITagReaderFactory tagReaderFactory, IMediaLibrary mediaLibrary, Guid userId, string path, Guid collectionId, bool populate, bool updateCollection)
        {
            var repositoryDelegate = new TrackRepositoryPathDelegate<TTagReader>(userId, path, collectionId, populate, updateCollection);
            repositoryDelegate.Method = repositoryDelegate.CreateMethod(metadataRepository, tagReaderFactory, mediaLibrary);
            RepositoryDelegate = repositoryDelegate;
        }
    }
}