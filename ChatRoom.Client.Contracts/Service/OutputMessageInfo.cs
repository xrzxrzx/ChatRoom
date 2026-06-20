using System;
using System.Collections.Generic;
using System.Text;

namespace ChatRoom.Client.Contracts.Service;

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
