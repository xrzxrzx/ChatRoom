using ChatRoom.Client.Core.Network;
using ChatRoom.Client.Core.Network.MessageBag.APIMessageBag;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.VoiceCommands;

namespace ChatRoom.Client.Function.ChatRoom
{
    public interface IChatRoomService : IDisposable
    {
        bool IsLoggedIn { get; }
        int GetUserId();
        string GetNickName();
        Task<bool> ConnectToServer();
        void DisconnectToServer();
        Task SendMessageAsync(string message);
        Task<bool> LogInAsync(int userId, string password);
        Task<bool> RegisterAsync(int userId, string password, string nickname);
        Task<bool> JoinRoomAsync(int roomId);
        Task<List<RoomInfo>> GetRoomListAsync();
        Task<ResponseMessageBag> CallAPIAsync(string apiName, params APIParameter[] parameters);

        public delegate void OutputMessageDelegate(OutputMessageInfo outputMessage);
        public event OutputMessageDelegate? OutputMessage;

        public delegate void OnLoginStatusChangedDelegate();
        public event OnLoginStatusChangedDelegate? OnLoginStatusChanged;
    }
}
