using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ChatRoom.Client.Core.Network.MessageBag.ClientMessageBag
{
    public class RequestMessageBag
    {
        private string command;
        private Dictionary<string, object> parameters;
        private string echo = string.Empty;

        public RequestMessageBag(string command)
        {
            this.command = command;
            this.parameters = new Dictionary<string, object>();
        }

        public RequestMessageBag AddParameter(string key, string value)
        {
            return AddParameter(key, (object)value);
        }

        public RequestMessageBag AddParameter(string key, int value)
        {
            return AddParameter(key, (object)value);
        }

        public RequestMessageBag AddParameter(string key, bool value)
        {
            return AddParameter(key, (object)value);
        }

        public RequestMessageBag AddParameter(string key, float value)
        {
            return AddParameter(key, (object)value);
        }

        private RequestMessageBag AddParameter(string key, object value)
        {
            parameters[key] = value;
            return this;
        }

        public RequestMessageBag SetEcho(string echo)
        {
            this.echo = echo;
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
