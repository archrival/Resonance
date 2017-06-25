using Newtonsoft.Json;
using System;

namespace Resonance.Data.Models
{
    public abstract class SettingBase : ModelBase
    {
        [JsonProperty("dateAdded")]
        public DateTime DateAdded { get; set; }

        [JsonProperty("dateModified")]
        public DateTime? DateModified { get; set; }

        [JsonProperty("enabled")]
        public bool Enabled { get; set; }
    }
}