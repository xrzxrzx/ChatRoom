using ChatRoom.Client.Core.Network;
using ChatRoom.Client.Core.Network.MessageBag.APIMessageBag;
using Newtonsoft.Json.Linq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Windows.System.UserProfile;

namespace ChatRoom.Client.Function.ChatRoom
{
    public class ChatRoomService : IChatRoomService
    {
        private IChatClientService chatClientService;
        private UserInfo userInfo;

        public event IChatRoomService.OutputMessageDelegate? OutputMessage;

        public ChatRoomService(IChatClientService chatClientService, ILogger logger)
        {
            this.chatClientService = chatClientService;
            userInfo = new UserInfo();

            ChatRoomFunction.SetLogger(logger);
            ChatRoomFunction.SetOutputMessageDelegate(message => OutputMessage?.Invoke(message));

            this.chatClientService.SubscribeToEvent<MessageEvent>(ChatRoomFunction.OutputMessage);
        }

        public async Task<ResponseMessageBag> CallAPIAsync(string apiName, params APIParameter[] parameters)
        {
            return await chatClientService.CallAPIAsync(apiName, parameters);
        }

        public async void ConnectToServer()
        {

            var result = await chatClientService.ConnectAsync();
            if (result == false)
            {
                OutputMessage?.Invoke(new(OutputMessageInfo.MessageSenderType.System, "连接服务器失败"));
                return;
            }

            chatClientService.StartReceiving();
        }

        public void DisconnectToServer()
        {
            throw new NotImplementedException();
        }

        public async Task<bool> RegisterAsync(int user_id, string password, string nickname)
        {
            var response = await chatClientService.CallAPIAsync("register", new("user_id", user_id),
                                                     new("password", password),
                                                     new("nickname", nickname));
            if (response.Success == false)
            {
                OutputMessage?.Invoke(new(OutputMessageInfo.MessageSenderType.System, $"注册失败: {response.ErrorMessage}"));
                return false;
            }
            userInfo.Id = response.Data["user_id"]?.Value<int>() ?? 0;
            userInfo.NickName = nickname;
            return true;
        }

        public async Task<bool> LogInAsync(int userId, string password)
        {
            var response = await chatClientService.CallAPIAsync("login",
                                               new("user_id", userId),
                                                        new("password", password));
            if (response.Success == false)
            {
                OutputMessage?.Invoke(new(OutputMessageInfo.MessageSenderType.System, $"登陆失败: {response.ErrorMessage}"));
                return false;
            }
            userInfo.Id = response.Data["user_id"]?.Value<int>() ?? 0;
            userInfo.NickName = response.Data["nickname"]?.Value<string>() ?? string.Empty;
            return true;
        }

        public async Task<bool> JoinRoomAsync(int roomId)
        {
            var response = await chatClientService.CallAPIAsync("join_room", new APIParameter("room_id", roomId));
            if (response.Success == false)
            {
                OutputMessage?.Invoke(new(OutputMessageInfo.MessageSenderType.System, $"加入房间失败: {response.ErrorMessage}"));
                return false;
            }
            return true;
        }

        public async Task SendMessageAsync(string message)
        {
            var response = await chatClientService.CallAPIAsync("send_message", new APIParameter("sender", userInfo.Id),
                                                     new APIParameter("message", message));

            if (response.Success == false)
            {
                Log.Warning($"消息发送失败: {response.ErrorMessage}");
                OutputMessage?.Invoke(new(OutputMessageInfo.MessageSenderType.System, $"消息发送失败: {response.ErrorMessage}"));
                return;
            }

            OutputMessage?.Invoke(new(OutputMessageInfo.MessageSenderType.Self, message));
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public int GetUserId()
        {
            return userInfo.Id;
        }

        public string GetNickName()
        {
            return userInfo.NickName;
        }

        public async Task<List<RoomInfo>> GetRoomListAsync()
        {
            var response = await chatClientService.CallAPIAsync("get_room_list");

            if (!response.Success)
            {
                OutputMessage?.Invoke(new(OutputMessageInfo.MessageSenderType.System, $"获取房间列表失败: {response.ErrorMessage}"));
                return new List<RoomInfo>();
            }

            var roomList = response.Data["room_info_list"]?.ToObject<List<RoomInfo>>() ?? new List<RoomInfo>();
            return roomList;
        }
    }

    public class OutputMessageInfo
    {
        public enum MessageSenderType
        {
            System,
            OtherUser,
            Self
        }

        public record SenderInfomation
        {
            public int Id { get; set; }
            public string NickName { get; set; } = string.Empty;
        }

        public MessageSenderType SenderType { get; init; }
        public SenderInfomation SenderInfo { get; init; } = new SenderInfomation();
        public string Content { get; init; } = string.Empty;

        public OutputMessageInfo(MessageSenderType senderType, string content)
        {
            SenderType = senderType;
            Content = content;
        }

        public OutputMessageInfo(int id, string nickname, string content)
        {
            SenderType = MessageSenderType.OtherUser;
            Content = content;
            SenderInfo.Id = id;
            SenderInfo.NickName = nickname;
        }
    }
}
