using Microsoft.AspNetCore.Mvc;
using Resonance.Common.Web;
using Resonance.Data.Models;
using Resonance.Data.Storage;
using Resonance.Data.Storage.Common;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Resonance.Web.Controllers
{
    [Route("rest/settings")]
    public class SettingsController : ResonanceControllerBase
    {
        public SettingsController(IMediaLibrary mediaLibrary, IMetadataRepository metadataRepository, ISettingsRepository settingsRepository) : base(mediaLibrary, metadataRepository, settingsRepository)
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