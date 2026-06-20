using System;
using System.Collections.Generic;
using System.Text;

namespace ChatRoom.Client.Function.ChatRoom;

public record UserInfo
{
    public int Id { get; set; }
    public string NickName { get; set; } = string.Empty;
}
