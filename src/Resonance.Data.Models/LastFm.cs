using Newtonsoft.Json;
using Resonance.Common;
using System;

namespace Resonance.Data.Models
{
    [JsonObject("lastFm")]
    public class LastFm : ModelBase
    {
        [JsonProperty("dateAdded")]
        public DateTime DateAdded { get; set; }

        [JsonProperty("dateModified")]
        public DateTime? DateModified { get; set; }

        [JsonProperty("details")]
        public string Details { get; set; }

        [JsonProperty("largeImageUrl")]
        public Uri LargeImageUrl { get; set; }

        [JsonProperty("largestImageUrl")]
        public Uri LargestImageUrl { get; set; }

        [JsonProperty("lastFmId")]
        public string LastFmId { get; set; }

        [JsonProperty("mediumImageUrl")]
        public Uri MediumImageUrl { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("smallImageUrl")]
        public Uri SmallImageUrl { get; set; }

        [JsonProperty("url")]
        public Uri Url { get; set; }

        public static LastFm FromDynamic(dynamic result)
        {
            LastFm lastFm = null;

            string lastFmId = result.LastFmId;
            string musicBrainzId = result.MusicBrainzId;

            if (!string.IsNullOrWhiteSpace(lastFmId) || !string.IsNullOrWhiteSpace(musicBrainzId))
            {
                lastFm = new LastFm
                {
                    Details = result.Details,
                    LastFmId = lastFmId,
                    Name = result.Name,
                    LargeImageUrl = new Uri(result.LargeImageUrl),
                    MediumImageUrl = new Uri(result.MediumImageUrl),
                    LargestImageUrl = new Uri(result.LargestImageUrl),
                    SmallImageUrl = new Uri(result.SmallImageUrl),
                    Url = new Uri(result.Url),
                    DateAdded = DynamicExtensions.GetDateTimeFromDynamic(result.LastFmDateAdded),
                    DateModified = result.LastFmDateModified == null ? null : DynamicExtensions.GetDateTimeFromDynamic(result.LastFmDateModified)
                };
            }

            return lastFm;
        }
    }
}