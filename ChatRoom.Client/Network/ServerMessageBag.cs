using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ChatRoom.Client.Network
{
    public class ServerMessageBag
    {
        private int recode;
        private string message = string.Empty;
        private string type = string.Empty;
        private JObject data;

        public int Recode { get => recode; set => recode = value; }
        public string Message { get => message; set => message = value; }
        public string Type { get => type; set => type = value; }
        public JObject Data { get => data; set => data = value; }

        public ServerMessageBag(string jsonString)
        {
            JObject obj = JsonConvert.DeserializeObject<JObject>(jsonString) ?? new JObject();

            recode = obj.Value<int>("recode");
            message = obj.Value<string>("msg") ?? string.Empty;
            type = obj.Value<string>("type") ?? string.Empty;
            data = obj.Value<JObject>("data") ?? new JObject();
        }
    }
}
