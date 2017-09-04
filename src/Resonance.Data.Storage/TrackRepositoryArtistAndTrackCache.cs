using Resonance.Data.Models;
using System;

namespace Resonance.Data.Storage
{
    public class TrackRepositoryArtistAndTrackCache : RepositoryCacheItem<MediaBundle<Track>>
    {
        public TrackRepositoryArtistAndTrackCache(IMetadataRepository metadataRepository, Guid userId, string artist, string track, Guid? collectionId, bool populate)
        {
            var repositoryDelegate = new TrackRepositoryArtistAndTrackDelegate(userId, artist, track, collectionId, populate);
            repositoryDelegate.Method = repositoryDelegate.CreateMethod(metadataRepository);
            RepositoryDelegate = repositoryDelegate;
        }
    }
}