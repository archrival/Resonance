using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Resonance.Data.Models;
using Resonance.Data.Storage;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Resonance.Common.Web.Controllers
{
    [Route("rest/settings")]
    public class SettingsController : ResonanceControllerBase
    {
        public SettingsController(IOptions<MetadataRepositorySettings> settings) : base(settings)
        {
        }

        [HttpPost("addCollection")]
        public async Task<Collection> AddCollectionAsync([FromQuery] string name, [FromQuery] string path, [FromQuery] string filter, [FromQuery] bool enabled = true)
        {
            return await SettingsRepository.AddCollectionAsync(name, path, filter, enabled, CancellationToken.None);
        }

        [HttpPost("addUser")]
        public async Task AddUserAsync([FromQuery] string user, [FromQuery] string password)
        {
            await SettingsRepository.AddUserAsync(user, password, CancellationToken.None);
        }

        [HttpGet("getCollections")]
        public async Task<IEnumerable<Collection>> GetCollectionsAsync()
        {
            return await SettingsRepository.GetCollectionsAsync(CancellationToken.None);
        }

        [HttpPost("removeCollection")]
        public async Task RemoveCollectionAsync([FromQuery] Guid id)
        {
            await SettingsRepository.RemoveCollectionAsync(id, CancellationToken.None);
        }
    }
}