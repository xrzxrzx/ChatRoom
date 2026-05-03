using System;
using System.Collections.Generic;
using System.Text;

namespace ChatRoom.Client.Models
{
    public record MessageInfo
    {
        public int SenderId { get; init; }
        public string SenderNickName { get; init; }
        public string Message { get; init; }

        public MessageInfo(int senderId, string senderNickName, string message)
        {
            SenderId = senderId;
            SenderNickName = senderNickName;
            Message = message;
        }
    }
}
