using Newtonsoft.Json;
using Resonance.Common;
using System;

namespace Resonance.Data.Models
{
    [JsonObject("marker")]
    public class Marker
    {
        [JsonProperty("comment")]
        public string Comment { get; set; }

        [JsonProperty("dateAdded")]
        public DateTime DateAdded { get; set; }

        [JsonProperty("dateModified")]
        public DateTime? DateModified { get; set; }

        [JsonProperty("position")]
        public long Position { get; set; }

        [JsonProperty("trackId")]
        public Guid TrackId { get; set; }

        [JsonProperty("user")]
        public User User { get; set; }

        public static Marker FromDynamic(dynamic result)
        {
            var marker = new Marker
            {
                TrackId = DynamicExtensions.GetGuidFromDynamic(result.TrackId),
                User = new User { Id = DynamicExtensions.GetGuidFromDynamic(result.UserId) },
                Position = DynamicExtensions.GetLongFromDynamic(result.Position),
                Comment = result.Comment,
                DateAdded = DynamicExtensions.GetDateTimeFromDynamic(result.DateAdded),
                DateModified = result.DateModified == null ? null : DynamicExtensions.GetDateTimeFromDynamic(result.DateModified)
            };

            return marker;
        }
    }
}