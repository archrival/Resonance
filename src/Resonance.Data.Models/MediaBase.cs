using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Resonance.Data.Models
{
    public class MediaBase : ModelBase, ISearchable, ICollectionIdentifier
    {
        [JsonProperty("collectionId")]
        public Guid CollectionId { get; set; }

        [JsonProperty("dateAdded")]
        public DateTime DateAdded { get; set; }

        [JsonProperty("dateModified")]
        public DateTime? DateModified { get; set; }

        [JsonProperty("genres")]
        public virtual HashSet<Genre> Genres { get; set; }

        [JsonProperty("mediaInfo")]
        public MediaInfo MediaInfo { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("styles")]
        public virtual HashSet<Style> Styles { get; set; }
    }
}