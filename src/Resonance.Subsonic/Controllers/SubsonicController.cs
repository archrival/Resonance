using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Resonance.Common.Web;
using Resonance.Data.Models;
using Resonance.Data.Storage;

using System;
using System.Threading.Tasks;

namespace Resonance.SubsonicCompat.Controllers
{
    [Route("subsonic")]
    public class SubsonicController : ResonanceControllerBase
    {
        public SubsonicController(IOptions<MetadataRepositorySettings> settings) : base(settings)
        {
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

            var subsonicAuthentication = new SubsonicAuthorization(Settings);
            var authenticationContext = await subsonicAuthentication.AuthorizeRequestAsync(queryParameters, cancellationToken).ConfigureAwait(false);

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