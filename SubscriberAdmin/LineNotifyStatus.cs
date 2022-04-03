using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SubscriberAdmin
{
    public class LineNotifyStatus
    {
        [JsonPropertyName("status")]
        public string Status { get; set; }
        [JsonPropertyName("message")]
        public string Message { get; set; }
        [JsonPropertyName("targetType")]
        public string TargetType { get; set; }
        [JsonPropertyName("target")]
        public string Target { get; set; }
    }
}