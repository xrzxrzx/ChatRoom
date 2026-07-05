using ChatRoom.Client.Core.Common;
using ChatRoom.Client.Core.Network.MessageBag;
using ChatRoom.Client.Core.Network.MessageBag.APIMessageBag;
using System;
using System.Collections.Generic;
using System.Text;

namespace ChatRoom.Client.Core.Network
{
    public interface IChatClientCoreService : IDisposable
    {
        public Task ConnectAsync(ChatClientConfig chatClientConfig);
        public Task DisconnectAsync();
        public void StartReceive();
        public Task SendMessageAsync(string message);

        public delegate void OnEventReceivedHandler(EventMessageBag messageBag);
        public event OnEventReceivedHandler OnEventReceived;

        public delegate void OnResponseReceivedHandler(ResponseMessageBag messageBag);
        public event OnResponseReceivedHandler OnResponseReceived;

        public delegate void ReconnectHandler();
        public event ReconnectHandler Reconnect;
    }
}
