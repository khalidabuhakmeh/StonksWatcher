using System;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Stonks
{
    public static class WebSocketExtensions
    {
        public static async Task SendAsJsonAsync(this ClientWebSocket client, object target, CancellationToken token)
        {
            var json = JsonConvert.SerializeObject(target);
            var sendBytes = Encoding.UTF8.GetBytes(json);

            await client.SendAsync(sendBytes, WebSocketMessageType.Text, true, token);
        }

        public static async Task<T> ReceiveAsAsync<T>(this ClientWebSocket client, CancellationToken token) where T : new()
        {
            var completeMessage = false;
            var sb = new StringBuilder();

            while (completeMessage == false)
            {
                var bytes = new byte[256];
                var segment = new ArraySegment<byte>(bytes);
                var result = await client.ReceiveAsync(segment, token);
                completeMessage = result.EndOfMessage;
                bytes = bytes.Take(result.Count).ToArray();
                sb.Append(Encoding.ASCII.GetString(bytes));
            }

            var response = sb.ToString();
            
            // ping response
            if (response.Contains("ping"))
                return new T();
            
            var target = JsonConvert.DeserializeObject<T>(response);

            return target;
        }
    }
}