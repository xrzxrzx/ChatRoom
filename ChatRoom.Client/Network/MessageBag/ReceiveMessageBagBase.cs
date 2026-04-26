using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatRoom.Client.Network.MessageBag
{
    public class ReceiveMessageBagBase
    {
        protected int recode;
        protected string message = string.Empty;
        protected JObject data;

        public int Recode { get => recode; set => recode = value; }
        public string Message { get => message; set => message = value; }
        public JObject Data { get => data; set => data = value; }

        public ReceiveMessageBagBase()
        {
            data = new JObject();
        }

        public ReceiveMessageBagBase(JObject recvJson)
        {
            recode = recvJson.Value<int>("recode");
            message = recvJson.Value<string>("msg") ?? "JSON解析异常";
            data = recvJson.Value<JObject>("data") ?? new JObject();
        }
    }
}
