using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ChatRoom.Client.Core.Network.MessageBag
{
    public class EventMessageBag
    {
        private string post_type = string.Empty;
        private JObject data = new JObject();
        private JObject rawJson = new JObject();

        public string PostType { get => post_type; set => post_type = value; }
        public JObject Data { get => data; set => data = value; }
        public JObject RawJson { get => rawJson; set => rawJson = value; }

        public EventMessageBag(JObject recvJson)
        {
            post_type = recvJson.Value<string>("post_type") ?? "JSON解析异常";
            data = recvJson.Value<JObject>("data") ?? new JObject();
            rawJson = recvJson;
        }
    }
}
