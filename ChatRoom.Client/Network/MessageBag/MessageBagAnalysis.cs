using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatRoom.Client.Network.MessageBag
{
    public class MessageBagAnalysis
    {
        JObject messageJson;

        public bool IsEvent => messageJson["event"] != null;
        public bool IsResponse => !IsEvent;

        /// <summary>
        /// 解析消息
        /// </summary>
        /// <param name="raw_message">初始消息</param>
        /// <exception cref="Newtonsoft.Json.JsonReaderException">当 raw_message 不是有效的 JSON 格式时抛出。</exception>
        public MessageBagAnalysis(string raw_message)
        {
            messageJson = JObject.Parse(raw_message);
        }

        public ServerMessageBag GetEventMessageBag()
        {
            return new ServerMessageBag(messageJson);
        }

        public ClientMessageBag.ResponseMessageBag GetResponseMessageBag()
        {
            return new ClientMessageBag.ResponseMessageBag(messageJson);
        }
    }
}
