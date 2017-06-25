using Newtonsoft.Json;
using Resonance.Common;
using System;

namespace Resonance.Data.Models
{
    [JsonObject("disposition")]
    public class Disposition : ModelBase, ICollectionIdentifier
    {
        [JsonProperty("averageRating")]
        public double? AverageRating { get; set; }

        [JsonProperty("collectionId")]
        public Guid CollectionId { get; set; }

        [JsonProperty("favorited")]
        public DateTime? Favorited { get; set; }

        [JsonProperty("mediaId")]
        public Guid MediaId { get; set; }

        [JsonProperty("mediaType")]
        public MediaType? MediaType { get; set; }

        [JsonProperty("userId")]
        public Guid UserId { get; set; }

        [JsonProperty("userRating")]
        public int? UserRating { get; set; }

        public static Disposition FromDynamic(dynamic result)
        {
            var disposition = new Disposition
            {
                AverageRating = result.AverageRating == null ? null : DynamicExtensions.GetDoubleFromDynamic(result.AverageRating),
                CollectionId = DynamicExtensions.GetGuidFromDynamic(result.CollectionId),
                MediaType = (MediaType?)result.MediaTypeId,
                Favorited = result.Favorited == null ? null : DynamicExtensions.GetDateTimeFromDynamic(result.Favorited),
                Id = DynamicExtensions.GetGuidFromDynamic(result.DispositionId),
                MediaId = result.MediaId == null ? DynamicExtensions.GetGuidFromDynamic(result.Id) : DynamicExtensions.GetGuidFromDynamic(result.MediaId),
                UserId = DynamicExtensions.GetGuidFromDynamic(result.UserId),
                UserRating = result.Rating == null ? null : DynamicExtensions.GetIntFromDynamic(result.Rating)
            };

            return disposition;
        }
    }
}