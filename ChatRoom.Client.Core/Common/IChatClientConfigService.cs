using System;
using System.Collections.Generic;
using System.Text;

namespace ChatRoom.Client.Core.Common
{
    public interface IChatClientConfigService : IDisposable
    {
        public ChatClientConfig GetConfig();
        public void SetConfig(ChatClientConfig config);
    }
}
