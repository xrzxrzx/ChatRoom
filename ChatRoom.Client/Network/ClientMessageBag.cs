using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace ChatRoom.Client.Network
{
    public class ClientMessageBag
    {
        private string command;
        private Dictionary<string, string> parameters;

        public ClientMessageBag(string command)
        {
            this.command = command;
            this.parameters = new Dictionary<string, string>();
        }

        public ClientMessageBag AddParameter(string key, string value)
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
}
