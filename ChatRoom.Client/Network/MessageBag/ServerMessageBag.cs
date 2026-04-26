using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ChatRoom.Client.Network.MessageBag
{
    public class ServerMessageBag : ReceiveMessageBagBase
    {
        private string type = string.Empty;

        public string Type { get => type; set => type = value; }

        public ServerMessageBag(JObject recvJson) : base(recvJson)
        {
            type = recvJson.Value<string>("type") ?? string.Empty;
        }
    }
}
