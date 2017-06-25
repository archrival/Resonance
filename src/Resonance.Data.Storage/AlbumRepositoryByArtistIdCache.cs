using Resonance.Data.Media.Common;
using Resonance.Data.Models;
using Resonance.Data.Storage.Common;
using System;
using System.Collections.Generic;

namespace Resonance.Data.Storage
{
    public class AlbumRepositoryByArtistIdCache<TTagReader> : RepositoryCacheItem<IEnumerable<MediaBundle<Album>>> where TTagReader : ITagReader, new()
    {
        public AlbumRepositoryByArtistIdCache(IMetadataRepository metadataRepository, ITagReaderFactory tagReaderFactory, IMediaLibrary mediaLibrary, Guid userId, Guid artistId, bool populate)
        {
            var repositoryDelegate = new AlbumRepositoryByArtistIdDelegate<TTagReader>(userId, artistId, populate);
            repositoryDelegate.Method = repositoryDelegate.CreateMethod(metadataRepository, tagReaderFactory, mediaLibrary);
            RepositoryDelegate = repositoryDelegate;
        }
    }
}