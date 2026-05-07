using ChatRoom.Client.Core.Network;
using ChatRoom.Client.Core.Network.MessageBag.APIMessageBag;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ChatRoom.Client.Function.ChatRoom
{
    public interface IChatRoomService : IDisposable
    {
        int GetUserId();
        string GetNickName();
        void ConnectToServer();
        void DisconnectToServer();
        Task SendMessageAsync(string message);
        Task LogInAsync(string user_id, string password);
        Task<ResponseMessageBag> CallAPIAsync(string apiName, params APIParameter[] parameters);

        public delegate void OutputMessageDelegate(OutputMessageInfo outputMessage);
        public event OutputMessageDelegate? OutputMessage;
    }
}
