using Resonance.Data.Media.Common;
using Resonance.Data.Models;
using Resonance.Data.Storage.Common;
using System;

namespace Resonance.Data.Storage
{
    public class TrackRepositoryIdCache<TTagReader> : RepositoryCacheItem<MediaBundle<Track>> where TTagReader : ITagReader, new()
    {
        public TrackRepositoryIdCache(IMetadataRepository metadataRepository, ITagReaderFactory tagReaderFactory, IMediaLibrary mediaLibrary, Guid userId, Guid id, bool populate)
        {
            var repositoryDelegate = new TrackRepositoryIdDelegate<TTagReader>(userId, id, populate);
            repositoryDelegate.Method = repositoryDelegate.CreateMethod(metadataRepository, tagReaderFactory, mediaLibrary);
            RepositoryDelegate = repositoryDelegate;
        }
    }
}