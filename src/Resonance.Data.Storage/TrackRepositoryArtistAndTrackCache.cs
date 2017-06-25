using Resonance.Data.Media.Common;
using Resonance.Data.Models;
using Resonance.Data.Storage.Common;
using System;

namespace Resonance.Data.Storage
{
    public class TrackRepositoryArtistAndTrackCache<TTagReader> : RepositoryCacheItem<MediaBundle<Track>> where TTagReader : ITagReader, new()
    {
        public TrackRepositoryArtistAndTrackCache(IMetadataRepository metadataRepository, ITagReaderFactory tagReaderFactory, IMediaLibrary mediaLibrary, Guid userId, string artist, string track, Guid? collectionId, bool populate)
        {
            var repositoryDelegate = new TrackRepositoryArtistAndTrackDelegate<TTagReader>(userId, artist, track, collectionId, populate);
            repositoryDelegate.Method = repositoryDelegate.CreateMethod(metadataRepository, tagReaderFactory, mediaLibrary);
            RepositoryDelegate = repositoryDelegate;
        }
    }
}