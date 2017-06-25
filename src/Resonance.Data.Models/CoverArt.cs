using Newtonsoft.Json;
using Resonance.Common;
using System;

namespace Resonance.Data.Models
{
    [JsonObject("coverArt")]
    public class CoverArt : ModelBase
    {
        [JsonIgnore]
        public byte[] CoverArtData { get; set; }

        [JsonProperty("coverArtType")]
        public CoverArtType CoverArtType { get; set; }

        [JsonProperty("mediaId")]
        public Guid MediaId { get; set; }

        [JsonProperty("coverArtMimeType")]
        public string MimeType { get; set; }

        [JsonProperty("coverArtSize")]
        public int Size { get; set; }

        public static CoverArt FromDynamic(dynamic result)
        {
            var coverArt = new CoverArt
            {
                Id = DynamicExtensions.GetGuidFromDynamic(result.Id),
                CoverArtType = (CoverArtType)result.CoverArtType,
                MediaId = result.MediaId == null ? DynamicExtensions.GetGuidFromDynamic(result.Id) : DynamicExtensions.GetGuidFromDynamic(result.MediaId),
                MimeType = result.CoverArtMimeType,
                Size = DynamicExtensions.GetIntFromDynamic(result.Size)
            };

            return coverArt;
        }
    }
}