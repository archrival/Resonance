using Newtonsoft.Json;
using Resonance.Common;

namespace Resonance.Data.Models
{
    [JsonObject("artist")]
    public class Artist : MediaBase
    {
        public static Artist FromDynamic(dynamic result)
        {
            var artist = new Artist
            {
                Id = DynamicExtensions.GetGuidFromDynamic(result.Id),
                CollectionId = DynamicExtensions.GetGuidFromDynamic(result.CollectionId),
                DateAdded = DynamicExtensions.GetDateTimeFromDynamic(result.DateAdded),
                DateModified = result.DateModified == null ? null : DynamicExtensions.GetDateTimeFromDynamic(result.DateModified),
                Name = result.Name
            };

            return artist;
        }
    }
}