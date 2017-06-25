using Newtonsoft.Json;
using Resonance.Common;
using System;

namespace Resonance.Data.Models
{
    [JsonObject("chat")]
    public class Chat
    {
        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonProperty("user")]
        public User User { get; set; }

        public static Chat FromDynamic(dynamic result)
        {
            var chat = new Chat
            {
                User = new User { Id = DynamicExtensions.GetGuidFromDynamic(result.UserId) },
                Timestamp = DynamicExtensions.GetDateTimeFromDynamic(result.Timestamp).ToUniversalTime(),
                Message = result.Message
            };

            return chat;
        }
    }
}