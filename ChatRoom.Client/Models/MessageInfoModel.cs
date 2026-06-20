using ChatRoom.Client.Function.ChatRoom;
using System;
using System.Collections.Generic;
using System.Text;

namespace ChatRoom.Client.Models;

public class MessageInfoModel
{
    public enum MessageInfoSenderType
    {
        Self,
        OtherUser,
        System
    }

    public int SenderId { get; set; }
    public string SenderNickName { get; set; }
    public string Message { get; set; }
    public MessageInfoSenderType SenderType { get; set; }

    public MessageInfoModel(int senderId, string senderNickName, string message, bool isSelf = false)
    {

        SenderId = senderId;
        SenderNickName = senderNickName;
        Message = message;
        SenderType = isSelf ? MessageInfoSenderType.Self : MessageInfoSenderType.OtherUser;
    }

    public MessageInfoModel(string message)
    {
        SenderId = -1;
        SenderNickName = "System";
        Message = message;
        SenderType = MessageInfoSenderType.System;
    }

    public static MessageInfoModel FromOutputMessageInfo(OutputMessageInfo outputMessageInfo)
    {
        switch (outputMessageInfo.SenderType)
        {
            case OutputMessageInfo.MessageSenderType.System:
                return new(outputMessageInfo.Content);
            case OutputMessageInfo.MessageSenderType.OtherUser:
                return new(outputMessageInfo.SenderInfo.Id, outputMessageInfo.SenderInfo.NickName, outputMessageInfo.Content);
            case OutputMessageInfo.MessageSenderType.Self:
                return new(outputMessageInfo.SenderInfo.Id, outputMessageInfo.SenderInfo.NickName, outputMessageInfo.Content, true);
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
