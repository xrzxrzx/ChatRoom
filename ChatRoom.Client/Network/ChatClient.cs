using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;
using ChatRoom.Client.Network.MessageBag.ClientMessageBag;

namespace ChatRoom.Client.Network
{
    public class ChatClient
    {
        //ChatClient
        private ChatClientAPI _api;
        private ChatClientEvent _event;
        private ChatClientCore _core;

        public ChatClient(string serverIp, int serverPort, ChatClientCore.OutputMethodDelegate outputMethod)
        {
            _core = new ChatClientCore(serverIp, serverPort, outputMethod);

            _core.OnMessageEventReceived += MessageEventHandler;
            _core.OnNoticeEventReceived += NoticeEventHandler;
            _core.OnRequestEventReceived += RequestEventHandler;
            _core.OnErrorEventReceived += ErrorEventHandler;
            _core.OnHeartbeatEventReceived += HeartbeatEventHandler;
        }

        public async Task ConnectAsync()
        {
            await _core.ConnectAsync();
        }

        public void StartReceiving()
        {
            _core.StartReceive();
        }

        public async Task SendMessageAsync(ChatRoomAPIName apiName, )
        {
            await _core.SendMessageAsync(messageBag);
        }

        private string MessageEventHandler(JObject data)
        {
            string sender = data["sender"]?.ToString() ?? "Unknown";
            string message = data["message"]?.ToString() ?? string.Empty;
            return $"{sender}: {message}";
        }

        private string NoticeEventHandler(JObject data)
        {
            string notice = data["notice"]?.ToString() ?? string.Empty;
            return $"[Notice] {notice}";
        }

        private string RequestEventHandler(JObject data)
        {
            string request = data["request"]?.ToString() ?? string.Empty;
            return $"[Request] {request}";
        }

        private string ErrorEventHandler(int recode, string message)
        {
            return $"[Error {recode}] {message}";
        }

        private string HeartbeatEventHandler(JObject data)
        {
            // 心跳事件通常不包含具体数据，这里仅记录接收时间
            return $"[Heartbeat] {DateTime.Now}";
        }
    }
}
