using Newtonsoft.Json;
using Resonance.Common;

namespace Resonance.Data.Models
{
    [JsonObject("radioStation")]
    public class RadioStation : ModelBase
    {
        [JsonProperty("homepageUrl")]
        public string HomepageUrl { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("streamUrl")]
        public string StreamUrl { get; set; }

        public static RadioStation FromDynamic(dynamic result)
        {
            var radioStation = new RadioStation
            {
                Id = DynamicExtensions.GetGuidFromDynamic(result.Id),
                HomepageUrl = result.HomepageUrl,
                Name = result.Name,
                StreamUrl = result.StreamUrl
            };

            return radioStation;
        }
    }
}