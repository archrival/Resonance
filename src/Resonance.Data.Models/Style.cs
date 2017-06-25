using Newtonsoft.Json;
using System;

namespace Resonance.Data.Models
{
    [JsonObject("style")]
    public class Style : ModelBase, ICollectionIdentifier
    {
        [JsonProperty("collectionId")]
        public Guid CollectionId { get; set; }

        [JsonProperty("dateAdded")]
        public DateTime DateAdded { get; set; }

        [JsonProperty("dateModified")]
        public DateTime? DateModified { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }
}