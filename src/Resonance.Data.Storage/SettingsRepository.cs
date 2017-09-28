using Resonance.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Resonance.Data.Storage
{
    public class SettingsRepository : ISettingsRepository
    {
        private readonly IMetadataRepository _metadataRepository;

        public SettingsRepository(IMetadataRepository metadataRepository)
        {
            _metadataRepository = metadataRepository;
        }

        public async Task<Collection> AddCollectionAsync(string name, string path, string filter, bool enabled, CancellationToken cancelToken)
        {
            var collection = new Collection
            {
                DateAdded = DateTime.UtcNow,
                Enabled = enabled,
                Filter = filter,
                Name = name,
                Path = path
            };

            await _metadataRepository.AddCollectionAsync(collection, cancelToken).ConfigureAwait(false);

            return collection;
        }

        public async Task AddUserAsync(string username, string password, CancellationToken cancelToken)
        {
            var user = new User
            {
                Name = username,
                Password = password
            };

            await _metadataRepository.AddUserAsync(user, cancelToken).ConfigureAwait(false);
        }

        public async Task<IEnumerable<Collection>> GetCollectionsAsync(CancellationToken cancelToken)
        {
            return await _metadataRepository.GetCollectionsAsync(cancelToken).ConfigureAwait(false);
        }

        public Task<User> GetUserAsync(string username, CancellationToken cancelToken)
        {
            throw new NotImplementedException();
        }

        public async Task RemoveCollectionAsync(Guid id, CancellationToken cancelToken)
        {
            var collections = await _metadataRepository.GetCollectionsAsync(cancelToken).ConfigureAwait(false);

            var collection = collections.FirstOrDefault(c => c.Id == id);

            if (collection != null)
            {
                await _metadataRepository.RemoveCollectionAsync(collection, cancelToken).ConfigureAwait(false);
            }
        }

        public Task SetPasswordAsync(string username, string password, CancellationToken cancelToken)
        {
            throw new NotImplementedException();
        }

        public Task UpdateCollectionAsync(Guid id, bool enabled, CancellationToken cancelToken)
        {
            throw new NotImplementedException();
        }
    }
}