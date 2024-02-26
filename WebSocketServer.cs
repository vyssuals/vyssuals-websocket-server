using System;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.Collections.Generic;

namespace Vyssuals.WsServer
{
    public class WebSocketServer
    {
        private List<WebSocket> clients = new List<WebSocket>();

        public async Task RunServerAsync(string url)
        {
            var httpListener = new HttpListener();
            httpListener.Prefixes.Add(url);
            httpListener.Start();
            Console.WriteLine($"Server started");

            while (true)
            {
                var context = await httpListener.GetContextAsync();

                if (context.Request.IsWebSocketRequest)
                {
                    var webSocketContext = await context.AcceptWebSocketAsync(null);
                    var webSocket = webSocketContext.WebSocket;

                    clients.Add(webSocket);

                    _ = HandleIncomingMessagesAsync(webSocket);

                    Console.WriteLine($"Client connected. Total clients: {clients.Count}");
                }
                else
                {
                    context.Response.StatusCode = 400;
                    context.Response.Close();

                    Console.WriteLine($"Bad request. Total clients: {clients.Count}");
                }
            }
        }

        private async Task HandleIncomingMessagesAsync(WebSocket sender)
        {
            var buffer = new byte[1024];

            while (sender.State == WebSocketState.Open)
            {
                var result = await sender.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    Console.WriteLine($"Received message from client: {message}");

                    foreach (var client in clients)
                    {
                        if (client != sender && client.State == WebSocketState.Open)
                        {
                            await client.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count), WebSocketMessageType.Text, result.EndOfMessage, CancellationToken.None);
                        }
                    }
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    await sender.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                    clients.Remove(sender);

                    Console.WriteLine($"Client disconnected. Total clients: {clients.Count}");
                }
            }
        }
    }
}