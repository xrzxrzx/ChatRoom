using ChatRoom.Client.Core.Network.MessageBag;
using System;
using System.Collections.Generic;
using System.Text;

namespace ChatRoom.Client.Core.Network
{
    public interface IChatClientEventService : IDisposable
    {
        public void OnEventReceived(EventMessageBag messageBag);
        public void StartHandleEvents();
        public void Subscribe<T>(Action<T> handler) where T : EventMessageBag;
    }
}
