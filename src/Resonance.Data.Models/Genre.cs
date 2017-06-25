using Newtonsoft.Json;
using Resonance.Common;
using System;

namespace Resonance.Data.Models
{
    [JsonObject("genre")]
    public class Genre : ModelBase, ICollectionIdentifier
    {
        [JsonProperty("collectionId")]
        public Guid CollectionId { get; set; }

        [JsonProperty("dateAdded")]
        public DateTime DateAdded { get; set; }

        [JsonProperty("dateModified")]
        public DateTime? DateModified { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        public static Genre FromDynamic(dynamic result)
        {
            var genre = new Genre
            {
                Id = DynamicExtensions.GetGuidFromDynamic(result.Id),
                CollectionId = DynamicExtensions.GetGuidFromDynamic(result.CollectionId),
                DateAdded = DynamicExtensions.GetDateTimeFromDynamic(result.DateAdded),
                DateModified = result.DateModified == null ? null : DynamicExtensions.GetDateTimeFromDynamic(result.DateModified),
                Name = result.Name
            };

            return genre;
        }
    }
}