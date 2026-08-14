using ChatRoom.Client.Core.Network;
using Serilog;
using static ChatRoom.Client.Function.ChatRoom.IChatRoomService;

namespace ChatRoom.Client.Function.ChatRoom
{
    internal static class ChatRoomFunction
    {
        private static OutputMessageDelegate? output;
        private static ILogger? logger;

        public static void SetLogger(ILogger logger)
        {
            ChatRoomFunction.logger = logger;
        }

        public static void SetOutputMessageDelegate(OutputMessageDelegate outputMessage)
        {
            output = outputMessage;
        }

        public static void OutputMessage(MessageEvent @event)
        {
            var senderName = @event.Data.Value<string>("nickname");
            if (senderName == null) 
            {
                logger?.Warning($"消息事件缺少发送者信息: {@event}");
                return;
            }

            var id = @event.Data.Value<int?>("sender") ?? 0;
            var messageContent = @event.Data.Value<string>("message") ?? string.Empty;


            output?.Invoke(new(id, senderName, messageContent));
        }
    }
}
