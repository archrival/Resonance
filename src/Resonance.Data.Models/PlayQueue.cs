using Newtonsoft.Json;
using Resonance.Common;
using System;
using System.Collections.Generic;

namespace Resonance.Data.Models
{
    [JsonObject("playQueue")]
    public class PlayQueue : ModelBase
    {
        [JsonProperty("clientName")]
        public string ClientName { get; set; }

        [JsonProperty("currentTrackId")]
        public Guid? CurrentTrackId { get; set; }

        [JsonProperty("dateAdded")]
        public DateTime DateAdded { get; set; }

        [JsonProperty("dateModified")]
        public DateTime? DateModified { get; set; }

        [JsonProperty("position")]
        public long? Position { get; set; }

        [JsonProperty("tracks")]
        public List<MediaBundle<Track>> Tracks { get; set; }

        [JsonProperty("user")]
        public User User { get; set; }

        public static PlayQueue FromDynamic(dynamic result)
        {
            var playQueue = new PlayQueue
            {
                Id = DynamicExtensions.GetGuidFromDynamic(result.Id),
                ClientName = result.ClientId,
                CurrentTrackId = result.CurrentTrackId != null ? DynamicExtensions.GetGuidFromDynamic(result.CurrentTrackId) : null,
                Position = result.Position == null ? DynamicExtensions.GetLongFromDynamic(result.Position) : null,
                User = new User { Id = DynamicExtensions.GetGuidFromDynamic(result.UserId) },
                DateAdded = DynamicExtensions.GetDateTimeFromDynamic(result.DateAdded),
                DateModified = result.DateModified == null ? null : DynamicExtensions.GetDateTimeFromDynamic(result.DateModified)
            };

            return playQueue;
        }
    }
}