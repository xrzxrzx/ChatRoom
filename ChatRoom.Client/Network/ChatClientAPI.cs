using ChatRoom.Client.Network.MessageBag.ClientMessageBag;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatRoom.Client.Network
{
    internal class ChatClientAPI
    {
        public ResponseMessageBag SendAPI(RequestMessageBag requestMessageBag)
        {
            
        }
    }

    public static class ChatRoomAPIType
    {
        public static readonly ChatRoomAPIName Message = new ChatRoomAPIName("message");
        public static readonly ChatRoomAPIName Request = new ChatRoomAPIName("request");
    }

    public record ChatRoomAPIName
    {
        public string API { get; set; }

        public ChatRoomAPIName(string api)
        {
            API = api;
        }
    }
}
