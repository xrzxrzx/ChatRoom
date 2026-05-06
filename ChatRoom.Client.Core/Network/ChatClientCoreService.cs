using ChatRoom.Client.Core.Common;
using ChatRoom.Client.Core.Network.MessageBag;
using ChatRoom.Client.Core.Network.MessageBag.APIMessageBag;
using System.Net.Sockets;
using System.Text;
using Serilog;

namespace ChatRoom.Client.Core.Network
{
    public class ChatClientCoreService : IChatClientCoreService
    {
        private TcpClient tcpClient;

        private ILogger logger;

        public event IChatClientCoreService.OnEventReceivedHandler? OnEventReceived;
        public event IChatClientCoreService.OnResponseReceivedHandler? OnResponseReceived;

        public ChatClientCoreService(ILogger logger)
        {
            tcpClient = new TcpClient();
            this.logger = logger;
        }

        public async Task ConnectAsync(ChatClientConfig chatClientConfig)
        {
            if (!tcpClient.Connected)
            {
                await tcpClient.ConnectAsync(chatClientConfig.ServerIp, chatClientConfig.ServerPort);
            }
        }

        public async Task SendMessageAsync(string message)
        {
            if (!tcpClient.Connected)
                return;

            NetworkStream stream = tcpClient.GetStream();
            byte[] buffer = Encoding.UTF8.GetBytes(message);
            await stream.WriteAsync(buffer, 0, buffer.Length);
        }

        public void StartReceive()
        {
            if (!tcpClient.Connected)
                return;

            Task.Run(() => Receive());
        }

        private async void Receive()
        {
            NetworkStream stream = tcpClient.GetStream();
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
                        EventMessageBag messageBag = bagAnalysis.GetEventMessageBag();
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
