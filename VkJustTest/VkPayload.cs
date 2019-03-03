using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VkJustTest
{
    [Serializable]
    public class VkPayload
    {
        [JsonProperty("button")]
        public string Value { get; set; }
    }
}
