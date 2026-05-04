using ChatRoom.Client.Core.Network.MessageBag;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace ChatRoom.Client.Core.Network
{
    internal interface IChatClientEventService : IDisposable
    {
        public void OnEventReceived(ServerMessageBag messageBag);
        public void StartConsumeEvents();
    }

    internal class ChatClientEventService : IChatClientEventService
    {
        ChatClientEventBus eventBus;

        private delegate void EventHandler(ServerMessageBag messageBag);

        public ChatClientEventService()
        {
            eventBus = new ChatClientEventBus();
        }

        public void OnEventReceived(ServerMessageBag messageBag)
        {
            dynamic? @event = messageBag.Type switch
            {
                "message" => messageBag as MessageEvent,
                "notice" => messageBag as NoticeEvent,
                "request" => messageBag as RequestEvent,
                "heartbeat" => messageBag as HeartbeatEvent,
                _ => throw new InvalidOperationException($"Unknown event type: {messageBag.Type}")
            };

            eventBus.Publish(@event);
        }

        public void StartConsumeEvents()
        {
            eventBus.StartConsumeEvents();
        }

        internal class ChatClientEventBus
        {
            Channel<ServerMessageBag> eventChannel;
            ConcurrentDictionary<string, List<Action<ServerMessageBag>>> subscribers;

            public ChatClientEventBus()
            {
                eventChannel = Channel.CreateUnbounded<ServerMessageBag>();
                subscribers = new ConcurrentDictionary<string, List<Action<ServerMessageBag>>>();
            }

            public void Publish<T>(T messageBag) where T : ServerMessageBag
            {
                eventChannel.Writer.WriteAsync(messageBag);
            }

            public void Subscribe<T>(Action<T> handler) where T : ServerMessageBag
            {
                string type = typeof(T).Name;
                subscribers.AddOrUpdate(type, new List<Action<ServerMessageBag>> { msg => handler((T)msg) },
                    (key, existingHandlers) =>
                    {
                        existingHandlers.Add(msg => handler((T)msg));
                        return existingHandlers;
                    });
            }

            public void StartConsumeEvents()
            {
                Task.Run(() => ConsumeEventsAsync());
            }

            private async Task ConsumeEventsAsync()
            {
                await foreach (var messageBag in eventChannel.Reader.ReadAllAsync())
                {
                    await DispatchEventAsync(messageBag);
                }
            }

            private async Task DispatchEventAsync(ServerMessageBag message)
            {
                if (subscribers.TryGetValue(message.Type, out var handlers))
                {
                    foreach (var handler in handlers)
                    {
                        await Task.Run(() => handler(message));
                    }
                }
            }
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        #region 事件类型定义
        public class MessageEvent : ServerMessageBag
        {
            public MessageEvent(JObject recvJson) : base(recvJson)
            {
            }
        }

        public class NoticeEvent : ServerMessageBag
        {
            public NoticeEvent(JObject recvJson) : base(recvJson)
            {
            }
        }

        public class RequestEvent : ServerMessageBag
        {
            public RequestEvent(JObject recvJson) : base(recvJson)
            {
            }
        }

        public class HeartbeatEvent : ServerMessageBag
        {
            public HeartbeatEvent(JObject recvJson) : base(recvJson)
            {
            }
        }
        #endregion
    }
}
