using ChatRoom.Client.Core.Network.MessageBag;
using System.Threading.Channels;

namespace ChatRoom.Client.Core.Network
{
    internal interface IChatClientEventService : IDisposable
    {
        public void OnEventReceived(ServerMessageBag messageBag);
        public void StartHandleEvents();
    }

    internal class ChatClientEventService : IChatClientEventService
    {
        private Channel<ServerMessageBag> _eventChannel;

        private delegate void EventHandler(ServerMessageBag messageBag);

        public ChatClientEventService()
        {
            _eventChannel = Channel.CreateUnbounded<ServerMessageBag>();
        }

        public void OnEventReceived(ServerMessageBag messageBag)
        {
            _eventChannel.Writer.WriteAsync(messageBag);
        }

        public void StartHandleEvents()
        {
            Task.Run(() => HandleEvents());
        }

        private async Task HandleEvents()
        {
            await foreach (var messageBag in _eventChannel.Reader.ReadAllAsync())
            {
                var handler = messageBag.Type switch
                {
                    "message" => new EventHandler(HandleMessage),
                    "notice" => new EventHandler(HandleNotice),
                    "request" => new EventHandler(HandleRequest),
                    "heartbeat" => new EventHandler(HandleHeartbeat),
                    _ => new EventHandler(HandleUnknown)
                };
                handler.Invoke(messageBag);
            }
        }

        private void HandleMessage(ServerMessageBag messageBag)
        {
            throw new NotImplementedException();
        }

        private void HandleNotice(ServerMessageBag messageBag)
        {
            throw new NotImplementedException();
        }

        private void HandleRequest(ServerMessageBag messageBag)
        {
            throw new NotImplementedException();
        }

        private void HandleHeartbeat(ServerMessageBag messageBag)
        {
            throw new NotImplementedException();
        }

        private void HandleUnknown(ServerMessageBag messageBag)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
