using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Stonks.Models;

namespace Stonks.Configuration
{
    public static class Settings
    {
        private static readonly IConfiguration Configuration = new ConfigurationBuilder()
            .AddJsonFile("appSettings.json")
            .AddEnvironmentVariables()
            .AddUserSecrets(typeof(Settings).Assembly, true)
            .Build();
        
        public static string GetFinnHubConnectionString()
        {
            return $"{FinnHub.WebSocketUri}?token={FinnHub.Token}";
        }

        private static readonly Lazy<List<string>> Symbols = 
            new(() => JsonConvert.DeserializeAnonymousType(
                File.ReadAllText("appSettings.json"),
                new {stocks = new List<string>()}
            )?.stocks);

        public static List<string> StockSymbols => Symbols.Value;

        public static class FinnHub
        {
            public static string WebSocketUri => Configuration["finnhub:ws"];
            public static string ApiUri => Configuration["finnhub:api"];
            public static string Token => Configuration["finnhub:apikey"];
            
            private static readonly HttpClient HttpClient = new();

            public static async Task<StockSymbolQuote> GetQuote(string symbol, CancellationToken cancellationToken)
            {
                var request =
                    $"{Settings.FinnHub.ApiUri}/quote?symbol={symbol}&token={Settings.FinnHub.Token}";
                var json = await HttpClient.GetStringAsync(request, cancellationToken);

                return JsonConvert.DeserializeObject<StockSymbolQuote>(json);
            }
        }

    }
}