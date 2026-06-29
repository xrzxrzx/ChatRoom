using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ChatRoom.Client.Core.Network.MessageBag.APIMessageBag
{
    public class RequestMessageBag
    {
        private string action;
        private Dictionary<string, object> parameters;
        private string echo = string.Empty;
        private string token = string.Empty;

        public RequestMessageBag(string action)
        {
            this.action = action;
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

        public RequestMessageBag SetToken(string token)
        {
            this.token = token;
            return this;
        }

        public string ToJsonString()
        {
            var dict = new Dictionary<string, object>
            {
                { "action", action },
                { "params", parameters },
                { "echo", echo },
                { "token", token }
            };
            return JsonConvert.SerializeObject(dict);
        }
    }

    public class ResponseMessageBag
    {
        public string Echo { get; private set; } = string.Empty;
        public bool Success => Recode == 0;
        public string ErrorMessage { get; private set; } = string.Empty;
        public int Recode { get; private set; } = 0;
        public JObject Data { get; private set; } = new JObject();
        public JObject RawJson { get; private set; } = new JObject();

        public ResponseMessageBag(JObject recvJson)
        {
            Echo = recvJson["echo"]?.ToString() ?? string.Empty;
            Recode = recvJson.Value<int?>("recode") ?? 0;
            ErrorMessage = recvJson.Value<string>("message") ?? string.Empty;
            Data = recvJson.Value<JObject>("data") ?? new JObject();
            RawJson = recvJson ?? new JObject();
        }

        public ResponseMessageBag(bool success = false, string errorMessage = "")
        {
            Recode = success ? 0 : -1;
            ErrorMessage = errorMessage;
            Echo = string.Empty;
            Data = new JObject();
            RawJson = new JObject();
        }
    }
}
