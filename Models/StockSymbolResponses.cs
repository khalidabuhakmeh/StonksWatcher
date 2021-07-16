using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Stonks.Models
{
    public class StockSymbolResponses
    {
        [JsonProperty("data")]
        public List<StockSymbolItem> Data { get; set; }
            = new();
        
        public class StockSymbolItem
        {
            [JsonProperty("s")] public string Symbol { get; set; }
            [JsonProperty("p")] public decimal CurrentPrice { get; set; }
            [JsonProperty("t")] public double UnixMillisecondsTimestamp { get; set; }
            [JsonIgnore] public DateTime Occurred => DateTime.UnixEpoch.AddMilliseconds(UnixMillisecondsTimestamp);
        }
    }
}