using ChatRoom.Client.Core.Common;
using ChatRoom.Client.Core.Network.MessageBag;
using ChatRoom.Client.Core.Network.MessageBag.ClientMessageBag;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ChatRoom.Client.Core.Network
{
    internal interface IChatClientCoreService : IDisposable
    {
        public Task ConnectAsync(ChatClientConfig chatClientConfig);
        public void StartReceive();
        public Task SendMessageAsync(string message);

        public delegate void OnEventReceivedHandler(ServerMessageBag messageBag);
        public event OnEventReceivedHandler OnEventReceived;

        public delegate void OnResponseReceivedHandler(ResponseMessageBag messageBag);
        public event OnResponseReceivedHandler OnResponseReceived;
    }

    internal class ChatClientCoreService : IChatClientCoreService
    {
        private TcpClient _tcpClient;

        public event IChatClientCoreService.OnEventReceivedHandler? OnEventReceived;
        public event IChatClientCoreService.OnResponseReceivedHandler? OnResponseReceived;

        public ChatClientCoreService()
        {
            _tcpClient = new TcpClient();
        }

        public async Task ConnectAsync(ChatClientConfig chatClientConfig)
        {
            if (!_tcpClient.Connected)
            {
                await _tcpClient.ConnectAsync(chatClientConfig.ServerIp, chatClientConfig.ServerPort);
            }
        }

        public async Task SendMessageAsync(string message)
        {
            if (!_tcpClient.Connected)
                return;

            NetworkStream stream = _tcpClient.GetStream();
            byte[] buffer = Encoding.UTF8.GetBytes(message);
            await stream.WriteAsync(buffer, 0, buffer.Length);
        }

        public void StartReceive()
        {
            if (!_tcpClient.Connected)
                return;

            Task.Run(() => Receive());
        }

        private async void Receive()
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
                        OnEventReceived?.Invoke(messageBag);
                    }
                    else//API响应消息
                    {
                        ResponseMessageBag responseBag = bagAnalysis.GetResponseMessageBag();
                        OnResponseReceived?.Invoke(responseBag);
                    }
                }
            }
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
