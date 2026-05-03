using ChatRoom.Client.Core.Network;
using ChatRoom.Client.Core.Network.MessageBag.ClientMessageBag;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Windows.System.UserProfile;

namespace ChatRoom.Client.Function.ChatRoom
{
    internal interface IChatRoomService : IDisposable
    {
        void ConnectToServer();
        void DisconnectToServer();
        Task SendMessageAsync(string message);
        Task LogInAsync(string user_id, string password);
        Task<ResponseMessageBag> CallAPIAsync(string apiName, params APIParameter[] parameters);
    }

    internal class ChatRoomService : IChatRoomService
    {
        private IChatClientService _chatClientService;
        private UserInfo _userInfo;

        public delegate void OutputMessageDelegate(OutputMessageInfo outputMessage);
        public event OutputMessageDelegate? OutputMessage;

        public ChatRoomService(IChatClientService chatClientService)
        {
            _chatClientService = chatClientService;
            _userInfo = new UserInfo();
        }

        public async Task<ResponseMessageBag> CallAPIAsync(string apiName, params APIParameter[] parameters)
        {
            return await _chatClientService.CallAPIAsync(apiName, parameters);
        }

        public async void ConnectToServer()
        {
            await _chatClientService.ConnectAsync();
            _chatClientService.StartReceiving();
        }

        public void DisconnectToServer()
        {
            throw new NotImplementedException();
        }

        public async Task LogInAsync(string user_id, string password)
        {
            var response = await _chatClientService.CallAPIAsync("login", new APIParameter("user_id", user_id),
                                                     new APIParameter("password", password));
            if(response.Success == false)
            {
                OutputMessage?.Invoke(new(OutputMessageInfo.MessageSender.System, $"登陆失败: {response.ErrorMessage }"));
            }
            _userInfo.Id = response.Data["user_id"]?.Value<int>() ?? 0;
        }

        public async Task SendMessageAsync(string message)
        {
            await _chatClientService.CallAPIAsync("send_message", new APIParameter("sender", _userInfo.Id),
                                                     new APIParameter("message", message));
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }

    internal record OutputMessageInfo
    {
        public enum MessageSender
        {
            System,
            OtherUser,
            Self
        }

        MessageSender Sender { get; init; }
        string Content { get; init; } = string.Empty;

        public OutputMessageInfo(MessageSender sender, string content)
        {
            Sender = sender;
            Content = content;
        }
    }
}
