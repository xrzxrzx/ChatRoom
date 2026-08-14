using ChatRoom.Client.Core.Network;
using ChatRoom.Client.Core.Network.MessageBag;
using Newtonsoft.Json.Linq;
using Xunit;

namespace ChatRoom.Client.Core.Tests;

public class EventBusTests
{
    private static EventMessageBag CreateEvent(string postType) =>
        new(JObject.Parse("{\"post_type\":\"" + postType + "\",\"data\":{\"sender\":1,\"nickname\":\"a\",\"message\":\"hi\"}}"));

    [Fact]
    public void Event_DispatchesByPostType()
    {
        using var service = new ChatClientEventService();
        service.StartHandleEvents();

        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        service.Subscribe<MessageEvent>(_ => tcs.TrySetResult(true));

        service.OnEventReceived(CreateEvent("message"));

        Assert.True(tcs.Task.Wait(TimeSpan.FromSeconds(3)), "message 事件应派发到 MessageEvent 订阅者");
    }

    [Fact]
    public void Event_UnknownPostTypeIsIgnored()
    {
        using var service = new ChatClientEventService();
        service.StartHandleEvents();

        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        service.Subscribe<MessageEvent>(_ => tcs.TrySetResult(true));

        service.OnEventReceived(CreateEvent("unknown_type"));

        Assert.False(tcs.Task.Wait(TimeSpan.FromMilliseconds(500)), "未知事件类型不应派发");
    }

    [Fact]
    public void Event_UnsubscribeStopsDelivery()
    {
        using var service = new ChatClientEventService();
        service.StartHandleEvents();

        int count = 0;
        Action<MessageEvent> handler = _ => Interlocked.Increment(ref count);
        service.Subscribe(handler);
        service.Unsubscribe(handler);

        service.OnEventReceived(CreateEvent("message"));
        Thread.Sleep(500);

        Assert.Equal(0, count);
    }
}
