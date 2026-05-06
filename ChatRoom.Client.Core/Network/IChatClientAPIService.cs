using ChatRoom.Client.Core.Network.MessageBag.APIMessageBag;
using System;
using System.Collections.Generic;
using System.Text;

namespace ChatRoom.Client.Core.Network
{
    public interface IChatClientAPIService : IDisposable
    {
        Task<ResponseMessageBag> CallAPIAsync(string apiName, IEnumerable<APIParameter> parameters);
        void OnResponseReceived(ResponseMessageBag messageBag);

        delegate Task SendMessageAsyncDelegate(string message);
        event SendMessageAsyncDelegate SendMessageAsync;
    }
}
