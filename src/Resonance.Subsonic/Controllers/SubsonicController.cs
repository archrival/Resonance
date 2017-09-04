using Microsoft.AspNetCore.Mvc;
using Resonance.Common.Web;
using Resonance.Data.Models;
using Resonance.Data.Storage;
using Resonance.Data.Storage.Common;
using System;
using System.Threading.Tasks;

namespace Resonance.SubsonicCompat.Controllers
{
    [Route("subsonic")]
    public class SubsonicController : ResonanceControllerBase
    {
        private readonly SubsonicAuthorization _subsonicAuthorization;

        public SubsonicController(IMediaLibrary mediaLibrary, IMetadataRepository metadataRepository, ISettingsRepository settingsRepository) : base(mediaLibrary, metadataRepository, settingsRepository)
        {
            _subsonicAuthorization = new SubsonicAuthorization(metadataRepository);
        }

        [HttpGet("")]
        public StatusCodeResult Get()
        {
            return StatusCode(204);
        }

        [HttpGet("musicFolderSettings.view"), HttpPost("musicFolderSettings.view")]
        public async Task<IActionResult> MusicFolderSettingsAsync()
        {
            var cancellationToken = ControllerContext.HttpContext.GetCancellationToken();

            var queryParameters = Request.GetSubsonicQueryParameters();

            var authenticationContext = await _subsonicAuthorization.AuthorizeRequestAsync(queryParameters, cancellationToken).ConfigureAwait(false);

            if (!authenticationContext.IsAuthenticated || !authenticationContext.IsInRole(Role.Settings))
            {
                return StatusCode(401);
            }

            if (Request.Query.ContainsKey("scanNow"))
            {
                MediaLibrary.ScanLibrary(Guid.Empty, null, false, cancellationToken);
            }

            return StatusCode(302);
        }
    }
}