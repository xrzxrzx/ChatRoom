using System;
using System.Collections.Generic;
using System.Text;
using YamlDotNet.Serialization;

namespace ChatRoom.Client.Core.Common
{
    internal interface IChatClientConfigService : IDisposable
    {
        public ChatClientConfig GetConfig();
        public void SetConfig(ChatClientConfig config);
    }

    internal class ChatClientConfigService : IChatClientConfigService
    {
        private readonly string _configFilePath = "ChatClient.yaml";

        public ChatClientConfigService()
        {
            
        }

        public ChatClientConfig GetConfig()
        {
            var deserializer = new DeserializerBuilder().Build();
            var configContent = File.ReadAllText(_configFilePath);
            var config = deserializer.Deserialize<ChatClientConfig>(configContent);
            return config;
        }

        public void SetConfig(ChatClientConfig config)
        {
            var serializer = new SerializerBuilder().Build();
            var yamlContent = serializer.Serialize(config);
            File.WriteAllText(_configFilePath, yamlContent);
        }

        public void Dispose()
        {
            
        }
    }

    public record ChatClientConfig
    {
        public string ServerIp { get; init; }
        public int ServerPort { get; init; }
        public ChatClientConfig(string serverIp, int serverPort)
        {
            ServerIp = serverIp;
            ServerPort = serverPort;
        }
    }
}
