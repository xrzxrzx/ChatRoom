using ChatRoom.Client.Core.Network.MessageBag;
using ChatRoom.Client.Core.Network.MessageBag.APIMessageBag;
using System;
using System.Collections.Generic;
using System.Text;

namespace ChatRoom.Client.Core.Network
{
    public interface IChatClientService : IDisposable
    {
        Task<bool> ConnectAsync();
        void StartReceiving();
        Task<ResponseMessageBag> CallAPIAsync(string apiName, string token, params APIParameter[] parameters);
        void SubscribeToEvent<T>(Action<T> handler) where T : EventMessageBag;
        void AddReconnectHandler(IChatClientCoreService.ReconnectHandler reconnectHandler);
    }
}
