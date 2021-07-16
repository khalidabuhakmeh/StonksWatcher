using System;
using Newtonsoft.Json;

namespace Stonks.Models
{
    public class StockSymbolQuote
    {
        [JsonProperty("o")] public decimal OpeningPrice { get; set; }
        [JsonProperty("c")] public decimal CurrentPrice { get; set; }
        [JsonIgnore] public DateTime Occurred { get; set; }
        [JsonIgnore] public decimal Difference => CurrentPrice - OpeningPrice;
        [JsonIgnore] public decimal Percentage => Difference / OpeningPrice;

        [JsonIgnore]
        public string Direction => Difference switch
        {
            > 0 => "[green]⬆[/]",
            < 0 => "[red]⬇[/]",
            _ => "[yellow]┅[/]"
        };
    }
}