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

await AnsiConsole
    .Status()
    .StartAsync("Loading stock symbols", async ctx =>
    {
        var count = 1;
        foreach (var stockSymbol in StockSymbols)
        {
            ctx.Status($"Loading stock symbol {stockSymbol} (#{count} of {StockSymbols.Count})");
            // calling the API to get initial JSON data
            var initial = await FinnHub.GetQuote(stockSymbol, cts.Token);
            latest[stockSymbol] = initial;

            // on success subscribe to changes of symbol
            await ws.SendAsJsonAsync(new {type = "subscribe", symbol = stockSymbol}, cts.Token);
            ctx.Status($"Subscribed to stock symbol {stockSymbol} (#{count} of {StockSymbols.Count})");
            count += 1;
        }
    });

// Now, we wait for responses from the web socked
await AnsiConsole.Live(Text.Empty)
    .StartAsync(async ctx =>
    {
        while (!cts.IsCancellationRequested)
        {
            var results = await ws.ReceiveAsAsync<StockSymbolResponses>(cts.Token);

            // this service sends a { "ping" : true }
            // every once in a while 🤷‍
            if (results is not { Data: { } })
                continue;

            foreach (var item in results.Data)
            {
                var symbol = latest[item.Symbol];
                symbol.CurrentPrice = item.CurrentPrice;
                symbol.Occurred = item.Occurred;
            }

            var table = new Table()
                .Title("🤑 Stocks to Watch", new Style(Color.Green))
                .Border(TableBorder.Rounded)
                .AddColumn("Symbol")
                .AddColumn("Opening Price ($)", t => t.Alignment = Justify.Right)
                .AddColumn("Current Price ($)", t => t.Alignment = Justify.Right)
                .AddColumn("Difference ($/%)", t => t.Alignment = Justify.Right)
                .AddColumn("Updated");

            foreach (var (symbol, quote) in latest)
            {
                table.AddRow(
                    $"[bold]{symbol}[/]",
                    $"{quote.OpeningPrice:$0.0000}",
                    $"{quote.CurrentPrice:$0.0000}",
                    $"{quote.Difference:$0.0000} [dim][italic]({quote.Percentage:P})[/][/] {quote.Direction}",
                    $"{quote.Occurred:HH:mm:ss}"
                );
            }

            ctx.UpdateTarget(table);
        }
    });
