using System;
using System.Collections.Generic;
using System.Text;

namespace ChatRoom.Client.Messages
{
    public enum AuthAction
    {
        Login,
        Register
    }

    public enum AuthResult
    {
        Success,
        Failed,
    }

    public record AuthResultMessage
    {
        public AuthAction Action { get; init; }
        public AuthResult Result { get; init; }
    }
}
