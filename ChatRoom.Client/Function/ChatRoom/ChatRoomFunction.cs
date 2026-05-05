using ChatRoom.Client.Core.Network;
using Serilog;
using static ChatRoom.Client.Function.ChatRoom.IChatRoomService;

namespace ChatRoom.Client.Function.ChatRoom
{
    internal static class ChatRoomFunction
    {
        private static OutputMessageDelegate? _output;

        public static void SetOutputMessageDelegate(OutputMessageDelegate outputMessage)
        {
            _output = outputMessage;
        }

        public static void OutputMessage(MessageEvent @event)
        {
            var senderName = @event.Data.Value<string>("nickname");
            if (senderName == null) 
            {
                Log.Warning($"消息事件缺少发送者信息: {@event}");
                return;
            }

            var id = @event.Data["id"]?.Value<int>("sender") ?? 0;
            var messageContent = @event.Data.Value<string>("message") ?? string.Empty;


            _output?.Invoke(new(id, senderName, messageContent));
        }
    }
}
