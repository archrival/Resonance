using Newtonsoft.Json;
using Resonance.Common;
using System;

namespace Resonance.Data.Models
{
    [JsonObject("playback")]
    public class Playback : ModelBase, ICollectionIdentifier
    {
        [JsonProperty("address")]
        public string Address { get; set; }

        [JsonProperty("clientId")]
        public string ClientId { get; set; }

        [JsonProperty("collectionId")]
        public Guid CollectionId { get; set; }

        [JsonProperty("playbackDateTime")]
        public DateTime PlaybackDateTime { get; set; }

        [JsonProperty("trackId")]
        public Guid TrackId { get; set; }

        [JsonProperty("userId")]
        public Guid UserId { get; set; }

        public static Playback FromDynamic(dynamic result)
        {
            if (result.Timestamp == null)
            {
                return null;
            }

            var playback = new Playback
            {
                Address = result.Address,
                ClientId = result.ClientId,
                CollectionId = DynamicExtensions.GetGuidFromDynamic(result.CollectionId),
                PlaybackDateTime = DynamicExtensions.GetDateTimeFromDynamic(result.Timestamp),
                TrackId = DynamicExtensions.GetGuidFromDynamic(result.Id),
                UserId = DynamicExtensions.GetGuidFromDynamic(result.UserId)
            };

            return playback;
        }
    }
}