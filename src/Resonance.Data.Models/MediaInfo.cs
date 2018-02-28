using Newtonsoft.Json;
using Resonance.Common;
using System;

namespace Resonance.Data.Models
{
    [JsonObject("mediaInfo")]
    public class MediaInfo : ModelBase
    {
        [JsonProperty("dateAdded")]
        public DateTime DateAdded { get; set; }

        [JsonProperty("dateModified")]
        public DateTime? DateModified { get; set; }

        [JsonProperty("lastFm")]
        public LastFm LastFm { get; set; }

        [JsonProperty("mediaId")]
        public Guid MediaId { get; set; }

        [JsonProperty("musicBrainzId")]
        public string MusicBrainzId { get; set; }

        public static MediaInfo FromDynamic(dynamic result)
        {
            var mediaInfo = new MediaInfo();

            var mediaInfoId = result.Id;

            if (mediaInfoId == null)
            {
                return mediaInfo;
            }

            mediaInfo.Id = DynamicExtensions.GetGuidFromDynamic(mediaInfoId);
            mediaInfo.MediaId = DynamicExtensions.GetGuidFromDynamic(result.MediaId);
            mediaInfo.MusicBrainzId = result.MusicBrainzId;
            mediaInfo.LastFm = Models.LastFm.FromDynamic(result);
            mediaInfo.DateAdded = DynamicExtensions.GetDateTimeFromDynamic(result.MediaInfoDateAdded);
            mediaInfo.DateModified = result.MediaInfoDateModified == null ? null : DynamicExtensions.GetDateTimeFromDynamic(result.MediaInfoDateModified);

            return mediaInfo;
        }
    }
}