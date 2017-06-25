using Resonance.Data.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Resonance.Data.Storage
{
    public interface ISettingsRepository
    {
        Task<Collection> AddCollectionAsync(string name, string path, string filter, bool enabled, CancellationToken cancelToken);

        Task AddUserAsync(string username, string password, CancellationToken cancelToken);

        Task<IEnumerable<Collection>> GetCollectionsAsync(CancellationToken cancelToken);

        Task<User> GetUserAsync(string username, CancellationToken cancelToken);

        Task RemoveCollectionAsync(Guid id, CancellationToken cancelToken);

        Task SetPasswordAsync(string username, string password, CancellationToken cancelToken);

        Task UpdateCollectionAsync(Guid id, bool enabled, CancellationToken cancelToken);
    }
}