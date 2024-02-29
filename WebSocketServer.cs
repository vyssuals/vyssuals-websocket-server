using System;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;

namespace Vyssuals.WsServer
{
    public class WebSocketServer
    {
        private List<WebSocket> clients = new List<WebSocket>();
        private CancellationTokenSource cts = new CancellationTokenSource();

        public async Task RunServerAsync(string url, CancellationToken cancellationToken)
        {
            var httpListener = new HttpListener();
            httpListener.Prefixes.Add(url);
            httpListener.Start();
            Debug.WriteLine($"wsServer: Server started");

            try 
            { 
                while (!cancellationToken.IsCancellationRequested)
                {
                    var context = await httpListener.GetContextAsync();

                    if (context.Request.IsWebSocketRequest)
                    {
                        var webSocketContext = await context.AcceptWebSocketAsync(null);
                        var webSocket = webSocketContext.WebSocket;

                        clients.Add(webSocket);

                        _ = HandleIncomingMessagesAsync(webSocket);

                        Debug.WriteLine($"wsServer: Client connected. Total clients: {clients.Count}");
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                        context.Response.Close();

                        Debug.WriteLine($"wsServer: Bad request. Total clients: {clients.Count}");
                    }
                }
                DisconnectClients();
                httpListener.Stop();
                Debug.WriteLine("wsServer: Server stopped");
            }
            catch (Exception e)
            {
                Debug.WriteLine($"wsServer: Server failed to start: {e.Message}");
            }
        }

        private void DisconnectClients()
        {
            if (clients.Count == 0) return;
            var clientsToClose = new List<WebSocket>();

            foreach (var client in clients)
            {
                if (client.State == WebSocketState.Open)
                {
                    clientsToClose.Add(client);
                }
            }

            foreach (var client in clientsToClose)
            {
                var closeTask = client.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                if (!closeTask.Wait(TimeSpan.FromSeconds(5)))  // Wait for up to 5 seconds
                {
                    Console.WriteLine("wsServer: Warning: WebSocket close operation timed out.");
                }
                clients.Remove(client);
            }
        }

        private async Task HandleIncomingMessagesAsync(WebSocket sender)
        {
            var buffer = new byte[1024];

            while (sender.State == WebSocketState.Open)
            {
                try
                {
                    var result = await sender.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        Debug.WriteLine($"wsServer: Received message from client: {message}");

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

                        Debug.WriteLine($"wsServer: Client disconnected. Total clients: {clients.Count}");
                    }
                }
                catch (WebSocketException e)
                {
                    Debug.WriteLine($"WebSocketException: {e.Message}");
                    break;
                }
            }
        }
    }
}