using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using ChatRoom.Client.Network.MessageBag;
using ChatRoom.Client.Network.MessageBag.ClientMessageBag;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ChatRoom.Client.Network
{
    public class ChatClientCore
    {
        private string _serverIp;
        private int _serverPort;
        private TcpClient _tcpClient;
        private Task? receiveTask;
        
        public delegate void OutputMethodDelegate(string message);
        OutputMethodDelegate outputMethod;

        #region 事件定义
        public delegate string OnMessageEventReceivedHandler(JObject data);
        public event OnMessageEventReceivedHandler? OnMessageEventReceived;

        public delegate string OnNoticeEventReceivedHandler(JObject data);
        public event OnNoticeEventReceivedHandler? OnNoticeEventReceived;

        public delegate string OnErrorEventReceivedHandler(int recode, string message);
        public event OnErrorEventReceivedHandler? OnErrorEventReceived;

        public delegate string OnRequestEventReceivedHandler(JObject data);
        public event OnRequestEventReceivedHandler? OnRequestEventReceived;

        public delegate string OnHeartbeatEventReceivedHandler(JObject data);
        public event OnHeartbeatEventReceivedHandler? OnHeartbeatEventReceived;
        #endregion

        public ChatClientCore(string serverIp, int serverPort, OutputMethodDelegate outputMethod)
        {
            _serverIp = serverIp;
            _serverPort = serverPort;
            _tcpClient = new TcpClient();
            this.outputMethod = outputMethod;
        }

        public async Task ConnectAsync()
        {
            if (!_tcpClient.Connected)
            {
                await _tcpClient.ConnectAsync(_serverIp, _serverPort);
            }
        }

        public void StartReceive()
        {
            receiveTask = Task.Run(Receive);
        }

        private async void Receive()
        {
            if (_tcpClient.Connected)
            {
                NetworkStream stream = _tcpClient.GetStream();
                byte[] buffer = new byte[1024];
                while (true)
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        MessageBagAnalysis bagAnalysis = new MessageBagAnalysis(message);
                        if (bagAnalysis.IsEvent)//事件消息
                        {
                            ServerMessageBag messageBag = bagAnalysis.GetEventMessageBag();
                            OnEventReceived(messageBag);
                        }
                        else//API响应消息
                        {
                            ResponseMessageBag responseBag = bagAnalysis.GetResponseMessageBag();
                            OnResponseReceived(responseBag);
                        }
                    }
                }
            }
        }

        //接收事件消息
        private void OnEventReceived(ServerMessageBag messageBag)
        {
            string outputMessage = string.Empty;

            switch (messageBag.Type)
            {
                case "message":
                    outputMessage = OnMessageEventReceived?.Invoke(messageBag.Data) ?? string.Empty;
                    break;
                case "notice":
                    outputMessage = OnNoticeEventReceived?.Invoke(messageBag.Data) ?? string.Empty;
                    break;
                case "request":
                    outputMessage = OnRequestEventReceived?.Invoke(messageBag.Data) ?? string.Empty;
                    break;
                case "heartbeat":
                    outputMessage = OnHeartbeatEventReceived?.Invoke(messageBag.Data) ?? string.Empty;
                    break;
                default:
                    outputMessage = OnErrorEventReceived?.Invoke(messageBag.Recode, messageBag.Message) ?? string.Empty;
                    break;
            }
            outputMethod?.Invoke(outputMessage);
        }

        //接收API响应消息
        private void OnResponseReceived(ResponseMessageBag messageBag)
        {
            
        }

        public async Task SendMessageAsync(RequestMessageBag messageBag)
        {
            if (_tcpClient.Connected)
            {
                NetworkStream stream = _tcpClient.GetStream();
                string message = messageBag.ToJsonString();
                byte[] data = Encoding.UTF8.GetBytes(message);
                await stream.WriteAsync(data, 0, data.Length);
            }
        }
    }
}
