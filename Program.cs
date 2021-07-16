using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using Spectre.Console;
using Stonks;
using Stonks.Models;
using static Stonks.Configuration.Settings;

// allow us to close the web socket when exiting
// use Ctrl+C to send a console interrupt
var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, _) => cts.Cancel();

// get your apiKey from https://finnhub.io/
// and set the user secret of finnhub:apikey from the project terminal
//   ‣ dotnet user-secrets set finnhub:apikey "<your key goes here>"
var connectionString = GetFinnHubConnectionString();
using var ws = new ClientWebSocket();
await ws.ConnectAsync(new Uri(connectionString), cts.Token);

// Add/Remove Stock Symbols from appSettings.json
// default are Apple, Google, Microsoft, and Amazon
var latest = new Dictionary<string, StockSymbolQuote>();

await AnsiConsole.Status()
    .StartAsync("Loading stock symbols", async ctx =>
    {
        foreach (var stockSymbol in StockSymbols)
        {
            AnsiConsole.MarkupLine($"Loading quote for [green]{stockSymbol}[/]...");

            // calling the API to get initial JSON data
            var initial = await FinnHub.GetQuote(stockSymbol, cts.Token);
            latest[stockSymbol] = initial;

            // on success subscribe to changes of symbol
            await ws.SendAsJsonAsync(new {type = "subscribe", symbol = stockSymbol}, cts.Token);
        }
    });

// Now, we wait for responses from the web socked
while (!cts.IsCancellationRequested)
{
    var results = await ws.ReceiveAsAsync<StockSymbolResponses>(cts.Token);

    // this service sends a { "ping" : true }
    // every once in a while 🤷‍
    if (results is not {Data : { }})
        continue;

    foreach (var item in results.Data)
    {
        var symbol = latest[item.Symbol];
        symbol.CurrentPrice = item.CurrentPrice;
        symbol.Occurred = item.Occurred;
    }

    var table = new Table()
        .Title("Stocks to Watch", new Style(Color.Green))
        .Border(TableBorder.Rounded)
        .AddColumn("Symbol")
        .AddColumn("Opening Price ($)")
        .AddColumn("Current Price ($)", t => t.Alignment = Justify.Right)
        .AddColumn("Difference ($/%)")
        .AddColumn("Date / Time");

    foreach (var (symbol, quote) in latest)
    {
        table.AddRow(symbol,
            quote.OpeningPrice.ToString("$0000.0000"),
            $"{quote.CurrentPrice:$0000.0000} {quote.Direction}",
            $"{quote.Difference:$000.0000} ({quote.Percentage:P})",
            quote.Occurred.ToString("HH:mm:ss")
        );
    }
    
    AnsiConsole.Clear();
    AnsiConsole.Render(table);
}
