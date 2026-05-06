using ChatRoom.Client.Core.Network.MessageBag;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace ChatRoom.Client.Core.Network
{
    public class ChatClientEventService : IChatClientEventService
    {
        ChatClientEventBus eventBus;

        private delegate void EventHandler(EventMessageBag messageBag);

        public ChatClientEventService()
        {
            eventBus = new ChatClientEventBus();
        }

        public void OnEventReceived(EventMessageBag messageBag)
        {
            dynamic? @event = messageBag.PostType switch
            {
                "message" => messageBag as MessageEvent,
                "notice" => messageBag as NoticeEvent,
                "request" => messageBag as RequestEvent,
                "heartbeat" => messageBag as HeartbeatEvent,
                _ => throw new InvalidOperationException($"Unknown event type: {messageBag.PostType}")
            };

            eventBus.Publish(@event);
        }

        public void StartHandleEvents()
        {
            eventBus.StartConsumeEvents();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void Subscribe<T>(Action<T> handler) where T : EventMessageBag
        {
            eventBus.Subscribe(handler);
        }
    }

    internal class ChatClientEventBus
    {
        Channel<EventMessageBag> eventChannel;
        ConcurrentDictionary<string, List<Action<EventMessageBag>>> subscribers;

        public ChatClientEventBus()
        {
            eventChannel = Channel.CreateUnbounded<EventMessageBag>();
            subscribers = new ConcurrentDictionary<string, List<Action<EventMessageBag>>>();
        }

        public void Publish<T>(T messageBag) where T : EventMessageBag
        {
            eventChannel.Writer.WriteAsync(messageBag);
        }

        public void Subscribe<T>(Action<T> handler) where T : EventMessageBag
        {
            string type = typeof(T).Name;
            subscribers.AddOrUpdate(type, new List<Action<EventMessageBag>> { msg => handler((T)msg) },
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

        private async Task DispatchEventAsync(EventMessageBag message)
        {
            if (subscribers.TryGetValue(message.PostType, out var handlers))
            {
                foreach (var handler in handlers)
                {
                    await Task.Run(() => handler(message));
                }
            }
        }
    }

    #region 事件类型定义
    public class MessageEvent : EventMessageBag
    {
        public MessageEvent(JObject recvJson) : base(recvJson)
        {
        }
    }

    public class NoticeEvent : EventMessageBag
    {
        public NoticeEvent(JObject recvJson) : base(recvJson)
        {
        }
    }

    public class RequestEvent : EventMessageBag
    {
        public RequestEvent(JObject recvJson) : base(recvJson)
        {
        }
    }

    public class HeartbeatEvent : EventMessageBag
    {
        public HeartbeatEvent(JObject recvJson) : base(recvJson)
        {
        }
    }
    #endregion
}
