using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SubscriberAdmin
{
    public class LineNotifyResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; }
    }
}