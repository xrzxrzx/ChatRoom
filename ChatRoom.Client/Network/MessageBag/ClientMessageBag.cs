using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ChatRoom.Client.Network.MessageBag.ClientMessageBag
{
    public class RequestMessageBag
    {
        private string command;
        private Dictionary<string, string> parameters;

        public RequestMessageBag(string command)
        {
            this.command = command;
            this.parameters = new Dictionary<string, string>();
        }

        public RequestMessageBag AddParameter(string key, string value)
        {
            parameters[key] = value;
            return this;
        }

        public string ToJsonString()
        {
            var dict = new Dictionary<string, object>
        {
            { "command", command },
            { "params", parameters }
        };
            return JsonConvert.SerializeObject(dict);
        }
    }

    public class ResponseMessageBag : ReceiveMessageBagBase
    {
        string echo = string.Empty;

        public string Echo { get => echo; set => echo = value; }

        public ResponseMessageBag(JObject recvJson) : base(recvJson)
        {
            echo = recvJson["echo"]?.ToString() ?? string.Empty;
        }
    }
}
