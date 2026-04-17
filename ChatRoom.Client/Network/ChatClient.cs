using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ChatRoom.Client.Network
{
    public class ChatClient
    {
        private string _serverIp;
        private int _serverPort;
        private TcpClient _tcpClient;
        private Task? receiveTask;

        #region 事件定义
        public delegate void OnMessageEventReceivedHandler(JObject data);
        public event OnMessageEventReceivedHandler? OnMessageEventReceived;

        public delegate void OnNoticeEventReceivedHandler(JObject data);
        public event OnNoticeEventReceivedHandler? OnNoticeEventReceived;

        public delegate void OnErrorEventReceivedHandler(int recode, string message);
        public event OnErrorEventReceivedHandler? OnErrorEventReceived;

        public delegate void OnRequestEventReceivedHandler(JObject data);
        public event OnRequestEventReceivedHandler? OnRequestEventReceived;

        public delegate void OnHeartbeatEventReceivedHandler(JObject data);
        public event OnHeartbeatEventReceivedHandler? OnHeartbeatEventReceived;
        #endregion

        public ChatClient(string serverIp, int serverPort)
        {
            _serverIp = serverIp;
            _serverPort = serverPort;
            _tcpClient = new TcpClient();
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
                        var messageBag = new ServerMessageBag(message);
                        OnMessageReceived(messageBag);
                    }
                }
            }
        }

        private void OnMessageReceived(ServerMessageBag messageBag)
        {
            switch (messageBag.Type)
            {
                case "message":
                    OnMessageEventReceived?.Invoke(messageBag.Data);
                    break;
                case "notice":
                    OnNoticeEventReceived?.Invoke(messageBag.Data);
                    break;
                case "request":
                    OnRequestEventReceived?.Invoke(messageBag.Data);
                    break;
                case "heartbeat":
                    OnHeartbeatEventReceived?.Invoke(messageBag.Data);
                    break;
                default:
                    OnErrorEventReceived?.Invoke(messageBag.Recode, messageBag.Message);
                    break;
            }
        }

        public async Task SendMessageAsync(ClientMessageBag messageBag)
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
