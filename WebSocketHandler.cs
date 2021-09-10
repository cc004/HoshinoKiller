using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HoshinoKiller
{
    public class WebSocketHandler
    {
        public long SelfId { get; set; }
        public string Token { get; set; }
        private const string uri = "wss://cloud.hoshinobot.cc/ws/";
        private ClientWebSocket client;

        public bool Running { get; private set; }
        public bool Active { get; private set; }

        public WebSocketHandler()
        {
            Active = false;
            Running = false;
        }

        public event Func<JObject, JObject> OnMessage;

        private static byte[] large = new byte[1 << 19];
        public async Task SendMsg(JObject json)
        {
            await SendMsg(json.ToString());
        }


        public async Task SendMsg(string text)
        {
            if (client.State == WebSocketState.Open)
                await client.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(text)), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        private async Task Connect()
        {
            while (Running && (client == null || client.State != WebSocketState.Open))
            {
                try
                {
                    client = new ClientWebSocket();
                    client.Options.SetRequestHeader("X-Self-ID", SelfId.ToString());
                    client.Options.SetRequestHeader("User-Agent", "CQHttp/4.15.0");
                    client.Options.SetRequestHeader("X-Client-Role", "Universal");
                    client.Options.SetRequestHeader("Authorization", "Token " + Token);
                    using (Task connect = client.ConnectAsync(new Uri(uri), CancellationToken.None))
                        if (await Task.WhenAny(connect, Task.Delay(5000)) != connect)
                            Console.WriteLine("Exception normal : websocket connection timed out.");
                    
                    Console.WriteLine("Connected to ws");
                }
                catch { }
            }
            Active = Running;
        }

        public async Task Stop()
        {
            Running = false;
            await client.CloseAsync(WebSocketCloseStatus.Empty, "connection closed.", CancellationToken.None);
        }

        public void Start()
        {
            Running = true;
            Connect().Wait();
            
            Task.Run(async () =>
            {
                while (Running)
                {
                    try
                    {
                        await SendMsg(new JObject
                        {
                            ["time"] = DateTime.Now.ToTimestamp(),
                            ["self_id"] = SelfId,
                            ["post_type"] = "meta_event",
                            ["meta_event_type"] = "heartbeat",
                            ["status"] = new JObject
                            {
                                ["online"] = true,
                                ["good"] = true
                            },
                            ["interval"] = 15000
                        });

                    }
                    catch { }
                    Thread.Sleep(10000);
                }
            });

            Task.Run(async () =>
            {
                byte[] buffer = new byte[1 << 16];
                while (Running)
                {
                    try
                    {
                        IEnumerable<byte> ms = Array.Empty<byte>();
                        while (true)
                        {
                            WebSocketReceiveResult result = null;
                            while (result == null)
                            {
                                try
                                {
                                    result = await client.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                                }
                                catch (AggregateException)
                                {
                                    Active = false;
                                    if (Running) await Connect();
                                }
                            }

                            ms = ms.Concat(buffer.Take(result.Count));
                            if (result.EndOfMessage) break;
                        }
                        var text = Encoding.UTF8.GetString(ms.ToArray());
                        try
                        {
                            var req = JObject.Parse(text);
                            var ret = OnMessage?.Invoke(req);
                            //Console.WriteLine($"resp = {ret}");
                            
                            await SendMsg(new JObject
                            {
                                ["data"] = ret,
                                ["retcode"] = 0,
                                ["status"] = "ok",
                                ["echo"] = req["echo"]
                            });
                        }
                        catch
                        {
                            //Console.WriteLine(text);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Uncaught exception : " + e.ToString());
                        await Connect();
                    }
                }
            });
        }

    }
}
