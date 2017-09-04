using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Resonance.Common.Web;
using Resonance.Data.Models;
using Resonance.Data.Storage;
using Resonance.Data.Storage.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Resonance.Web.Controllers
{
    [Route("rest/mediaLibrary")]
    public class MediaLibraryController : ResonanceControllerBase
    {
        public MediaLibraryController(IMediaLibrary mediaLibrary, IMetadataRepository metadataRepository, ISettingsRepository settingsRepository) : base(mediaLibrary, metadataRepository, settingsRepository)
        {
        }

        [HttpPost("clearLibrary")]
        public void ClearLibrary([FromQuery] Guid? collectionId, [FromQuery] bool areYouSure)
        {
            var cancellationToken = new CancellationTokenSource();

            if (areYouSure)
                MediaLibrary.ClearLibraryAsync(Guid.Empty, collectionId, cancellationToken.Token);
        }

        [HttpGet("scanProgress")]
        public ScanProgress GetScanProgress()
        {
            return MediaLibrary.ScanProgress;
        }

        [HttpPost("importPlaylist")]
        public async Task ImportPlaylistAsync([FromQuery] string username, [FromQuery] string playlistName, [FromBody] List<string> files)
        {
            if (files == null)
            {
                return;
            }

            var user = await MetadataRepository.GetUserAsync(username, CancellationToken.None);

            if (user == null)
            {
                return;
            }

            var playlists = await MetadataRepository.GetPlaylistsAsync(user.Id, null, true, CancellationToken.None);

            if (playlists.Any(p => p.Name == playlistName))
            {
                return;
            }

            var playlist = new Playlist
            {
                Name = playlistName,
                User = user,
                Accessibility = Accessibility.Public,
                Tracks = new List<MediaBundle<Track>>()
            };

            foreach (var file in files)
            {
                var track = await MetadataRepository.GetTrackAsync(user.Id, file, null, false, CancellationToken.None);

                if (track == null)
                {
                    continue;
                }

                playlist.Tracks.Add(track);
            }

            if (playlist.Tracks.Any())
            {
                await MetadataRepository.UpdatePlaylistAsync(playlist, CancellationToken.None);
            }
        }

        [HttpPost("scanLibrary")]
        public void ScanLibrary([FromQuery] Guid? collectionId, [FromQuery] bool clearLibrary = false)
        {
            var cancellationToken = new CancellationTokenSource();

            if (clearLibrary)
                MediaLibrary.ClearLibraryAsync(Guid.Empty, collectionId, cancellationToken.Token);

            MediaLibrary.ScanLibrary(Guid.Empty, collectionId, clearLibrary, cancellationToken.Token);
        }

        [HttpPost("stopScanningLibrary")]
        public void StopScanningLibrary()
        {
            var cancellationToken = new CancellationTokenSource();

            MediaLibrary.StopScanningLibrary(Guid.Empty, cancellationToken.Token);
        }

        [XmlRoot("files")]
        [JsonObject("files")]
        public class Files
        {
            [XmlElement("file")]
            [JsonProperty("file")]
            public List<string> File;
        }
    }
}