using Newtonsoft.Json;
using Resonance.Common;
using System;
using System.Collections.Generic;

namespace Resonance.Data.Models
{
    [JsonObject("playlist")]
    public class Playlist : ModelBase
    {
        [JsonProperty("accessibility")]
        public Accessibility Accessibility { get; set; }

        [JsonProperty("comment")]
        public string Comment { get; set; }

        [JsonProperty("dateAdded")]
        public DateTime DateAdded { get; set; }

        [JsonProperty("dateModified")]
        public DateTime? DateModified { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("tracks")]
        public List<MediaBundle<Track>> Tracks { get; set; }

        [JsonProperty("user")]
        public User User { get; set; }

        public static Playlist FromDynamic(dynamic result)
        {
            var playlist = new Playlist
            {
                Accessibility = (Accessibility)result.Accessibility,
                Comment = result.Comment,
                Id = DynamicExtensions.GetGuidFromDynamic(result.Id),
                Name = result.Name,
                User = new User { Id = DynamicExtensions.GetGuidFromDynamic(result.UserId) },
                DateAdded = DynamicExtensions.GetDateTimeFromDynamic(result.DateAdded),
                DateModified = result.DateModified == null ? null : DynamicExtensions.GetDateTimeFromDynamic(result.DateModified)
            };

            return playlist;
        }
    }
}