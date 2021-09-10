using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;

namespace HoshinoKiller
{
    class Program
    {
        private static Random rnd => _rnd ??= new Random();
        [ThreadStatic]
        private static Random _rnd;

        private static HashSet<string> sets = new();
        private class Account
        {
            public long selfid;
            public string token;
        }
        static void Main(string[] args)
        {
            var acc = JsonConvert.DeserializeObject<Account>(File.ReadAllText("accounts.json"));
            try
            {
                sets = new HashSet<string>(JArray.Parse(File.ReadAllText("image.json")).Select(i => (string)i));
            }
            catch (Exception e)
            {
            }
            var handler = new WebSocketHandler
            {
                SelfId = acc.selfid,
                Token = acc.token
            };
            handler.OnMessage += msg =>
            {
                //Console.WriteLine(msg);
                switch (msg.Value<string>("action"))
                {
                    case "get_group_member_info":
                        return new JObject
                        {
                            ["group_id"] = msg["params"]["group_id"],
                            ["user_id"] = msg["params"]["user_id"],
                            ["nickname"] = "",
                            ["card"] = "",
                            ["nickname"] = "",
                            ["sex"] = "female",
                            ["age"] = 1,
                            ["area"] = "",
                            ["level"] = "100",
                            ["role"] = "owner",
                            ["title"] = "null",
                            ["title_expire_time"] = 0,
                            ["card_changeable"] = true,
                            ["unfriendly"] = false,
                            ["last_sent_time"] = 0,
                            ["join_time"] = 0,
                            ["shut_up_timestamp"] = 0
                        };
                    case "send_msg":
                        try
                        {
                           foreach (var i in msg["params"]["message"].Where(t => (string)t["type"] == "image")
                                .Select(t => t["data"]["file"].ToString()))
                            sets.Add(i);
                           File.WriteAllText("image.json", new JArray(sets).ToString());
                           Console.WriteLine(string.Concat(msg["params"]["message"].Select(t => t?["data"]?["text"] ?? "")));
                        }
                        catch
                        {

                        }
                        return new JObject
                        {
                            ["message_id"] = rnd.Next()
                        };
                }

                return null;
            };
            handler.Start();

            handler.SendMsg(new JObject
            {
                ["time"] = DateTime.Now.ToTimestamp(),
                ["self_id"] = handler.SelfId,
                ["post_type"] = "meta_event",
                ["message_type"] = "lifecycle",
                ["sub_type"] = "connect"
            }).Wait();

            while (true)
            {
                try
                {
                    var msg = Console.ReadLine();
                    //var msg = "lssv";
                    //var msg = "色图"; //"谁是" + new string(Enumerable.Range(0, 1 << 10).Select(_ => (char)(rnd.Next() % 26 + 'A')).ToArray());
                    handler.SendMsg(new JObject
                    {
                        ["time"] = DateTime.Now.ToTimestamp(),
                        ["self_id"] = handler.SelfId,
                        ["post_type"] = "message",
                        ["message_type"] = "group",
                        ["sub_type"] = "normal",
                        ["message_id"] = rnd.Next(),
                        ["group_id"] = 190534564,
                        ["user_id"] = rnd.Next(),
                        ["anonymous"] = null,
                        ["message"] = msg,
                        ["raw_message"] = msg,
                        ["font"] = 0,

                        ["sender"] = new JObject
                        {
                            ["user_id"] = rnd.Next(),
                            ["nickname"] = "咖啡佬",
                            ["card"] = "咖啡佬",
                            ["sex"] = "female",
                            ["age"] = 10,
                            ["area"] = "null",
                            ["level"] = "100",
                            ["role"] = "owner",
                            ["title"] = "null"
                        }
                    }).Wait();
                }
                catch
                {
                }
            }
        }
    }
}
