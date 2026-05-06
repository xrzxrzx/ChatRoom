using ChatRoom.Client.Core.Network.MessageBag;
using ChatRoom.Client.Core.Network.MessageBag.APIMessageBag;
using System;
using System.Collections.Generic;
using System.Text;

namespace ChatRoom.Client.Core.Network
{
    public interface IChatClientService : IDisposable
    {
        public Task ConnectAsync();
        public void StartReceiving();
        public Task<ResponseMessageBag> CallAPIAsync(string apiName, params APIParameter[] parameters);
        public void SubscribeToEvent<T>(Action<T> handler) where T : EventMessageBag;
    }
}
