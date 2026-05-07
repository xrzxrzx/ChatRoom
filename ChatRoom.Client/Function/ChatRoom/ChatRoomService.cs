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
            await chatClientService.ConnectAsync();
            chatClientService.StartReceiving();
        }

        public void DisconnectToServer()
        {
            throw new NotImplementedException();
        }

        public async Task LogUpAsync(string user_id, string password, string nickname)
        {
            var response = await chatClientService.CallAPIAsync("register", new APIParameter("user_id", user_id),
                                                     new APIParameter("password", password),
                                                     new APIParameter("nickname", nickname));
            if (response.Success == false)
            {
                OutputMessage?.Invoke(new(OutputMessageInfo.MessageSenderType.System, $"注册失败: {response.ErrorMessage}"));
            }
            userInfo.Id = response.Data["user_id"]?.Value<int>() ?? 0;
        }

        public async Task LogInAsync(string user_id, string password)
        {
            var response = await chatClientService.CallAPIAsync("login", new APIParameter("user_id", user_id),
                                                     new APIParameter("password", password));
            if (response.Success == false)
            {
                OutputMessage?.Invoke(new(OutputMessageInfo.MessageSenderType.System, $"登陆失败: {response.ErrorMessage}"));
            }
            userInfo.Id = response.Data["user_id"]?.Value<int>() ?? 0;
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
