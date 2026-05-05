using System;
using System.Collections.Generic;
using System.Text;

namespace ChatRoom.Client.Models
{
    public class MessageInfo
    {
        public enum MessageInfoSenderType
        {
            Self,
            OtherUser,
            System
        }

        public int SenderId { get; init; }
        public string SenderNickName { get; init; }
        public string Message { get; init; }
        public MessageInfoSenderType SenderType { get; init; }

        public MessageInfo(int senderId, string senderNickName, string message, bool isSelf = false)
        {

            SenderId = senderId;
            SenderNickName = senderNickName;
            Message = message;
            SenderType = isSelf ? MessageInfoSenderType.Self : MessageInfoSenderType.OtherUser;
        }

        public MessageInfo(string message)
        {
            SenderId = -1;
            SenderNickName = "System";
            Message = message;
            SenderType = MessageInfoSenderType.System;
        }
    }
}
