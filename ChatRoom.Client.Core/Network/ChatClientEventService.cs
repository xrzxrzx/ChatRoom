using ChatRoom.Client.Core.Network.MessageBag;
using Newtonsoft.Json.Linq;
using System.Threading.Channels;

namespace ChatRoom.Client.Core.Network
{
    public class ChatClientEventService : IChatClientEventService
    {
        private readonly ChatClientEventBus eventBus;
        private readonly IReadOnlyDictionary<string, Func<JObject, EventMessageBag>> eventFactoryMap;

        public ChatClientEventService()
        {
            eventBus = new ChatClientEventBus();
            eventFactoryMap = new Dictionary<string, Func<JObject, EventMessageBag>>(StringComparer.OrdinalIgnoreCase)
            {
                ["message"] = json => new MessageEvent(json),
                ["notice"] = json => new NoticeEvent(json),
                ["update"] = json => new UpdateEvent(json),
                ["request"] = json => new RequestEvent(json),
                ["heartbeat"] = json => new HeartbeatEvent(json)
            };
        }

        public void OnEventReceived(EventMessageBag messageBag)
        {
            //未知事件类型直接忽略，避免影响接收循环
            if (!eventFactoryMap.TryGetValue(messageBag.PostType, out var factory))
            {
                return;
            }

            var @event = factory(messageBag.RawJson);
            eventBus.Publish(@event);
        }

        public void StartHandleEvents()
        {
            eventBus.StartConsumeEvents();
        }

        public void Subscribe<T>(Action<T> handler) where T : EventMessageBag
        {
            eventBus.Subscribe(handler);
        }

        public void Unsubscribe<T>(Action<T> handler) where T : EventMessageBag
        {
            eventBus.Unsubscribe(handler);
        }

        public void Dispose()
        {
            eventBus.Close();
        }
    }

    internal class ChatClientEventBus
    {
        private readonly Channel<EventMessageBag> eventChannel;
        private readonly object sync = new();
        private readonly Dictionary<string, List<Action<EventMessageBag>>> subscribers = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<Delegate, Action<EventMessageBag>> handlerWrappers = new();
        private bool closed;

        public ChatClientEventBus()
        {
            eventChannel = Channel.CreateUnbounded<EventMessageBag>();
        }

        public void Publish<T>(T messageBag) where T : EventMessageBag
        {
            if (closed)
            {
                return;
            }
            eventChannel.Writer.TryWrite(messageBag);
        }

        public void Subscribe<T>(Action<T> handler) where T : EventMessageBag
        {
            var wrapper = new Action<EventMessageBag>(msg => handler((T)msg));
            string key = GetPostType<T>();
            lock (sync)
            {
                handlerWrappers[handler] = wrapper;
                if (!subscribers.TryGetValue(key, out var list))
                {
                    list = new List<Action<EventMessageBag>>();
                    subscribers[key] = list;
                }
                list.Add(wrapper);
            }
        }

        public void Unsubscribe<T>(Action<T> handler) where T : EventMessageBag
        {
            lock (sync)
            {
                if (!handlerWrappers.TryGetValue(handler, out var wrapper))
                {
                    return;
                }

                handlerWrappers.Remove(handler);
                string key = GetPostType<T>();
                if (subscribers.TryGetValue(key, out var list))
                {
                    list.Remove(wrapper);
                    if (list.Count == 0)
                    {
                        subscribers.Remove(key);
                    }
                }
            }
        }

        public void StartConsumeEvents()
        {
            Task.Run(() => ConsumeEventsAsync());
        }

        public void Close()
        {
            lock (sync)
            {
                closed = true;
                subscribers.Clear();
                handlerWrappers.Clear();
            }
            eventChannel.Writer.TryComplete();
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
            List<Action<EventMessageBag>> handlers;
            lock (sync)
            {
                if (!subscribers.TryGetValue(message.PostType, out var list))
                {
                    return;
                }
                handlers = list.ToList();
            }

            foreach (var handler in handlers)
            {
                await Task.Run(() => handler(message));
            }
        }

        //事件类型名去掉 Event 后缀并转小写，与服务端 post_type 对齐：
        //MessageEvent -> message、NoticeEvent -> notice、UpdateEvent -> update ...
        private static string GetPostType<T>() where T : EventMessageBag
        {
            const string suffix = "Event";
            var name = typeof(T).Name;
            return name.EndsWith(suffix, StringComparison.Ordinal)
                ? name[..^suffix.Length].ToLowerInvariant()
                : name.ToLowerInvariant();
        }
    }

    #region 事件类型定义

    //消息事件
    public class MessageEvent : EventMessageBag
    {
        public MessageEvent(JObject recvJson) : base(recvJson)
        {
        }
    }

    //通知事件（如系统通知）
    public class NoticeEvent : EventMessageBag
    {
        public NoticeEvent(JObject recvJson) : base(recvJson)
        {
        }
    }

    //更新事件（如房间列表更新）
    public class UpdateEvent : EventMessageBag
    {
        public UpdateEvent(JObject recvJson) : base(recvJson)
        {
        }
    }

    //请求事件（预留）
    public class RequestEvent : EventMessageBag
    {
        public RequestEvent(JObject recvJson) : base(recvJson)
        {
        }
    }

    //心跳事件
    public class HeartbeatEvent : EventMessageBag
    {
        public HeartbeatEvent(JObject recvJson) : base(recvJson)
        {
        }
    }
    #endregion
}

