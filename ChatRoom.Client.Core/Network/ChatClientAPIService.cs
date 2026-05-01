using ChatRoom.Client.Core.Network.MessageBag.ClientMessageBag;
using Newtonsoft.Json.Linq;

namespace ChatRoom.Client.Core.Network
{
    internal interface IChatClientAPIService : IDisposable
    {
        Task<ResponseMessageBag> SendAPIAsync(string apiName, IEnumerable<APIParameter> parameters);
    }

    internal class ChatClientAPIService : IChatClientAPIService
    {
        public async Task<ResponseMessageBag> SendAPIAsync(string apiName, IEnumerable<APIParameter> parameters)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }

    public record APIParameter
    {
        public string Key { get; init; }
        public object Value { get; init; }

        // 私有化底层构造方法，防止外部传入不受支持的 object 类型
        private APIParameter(string key, object value)
        {
            Key = key;
            Value = value;
        }

        // 提供强类型的重载构造方法，限定只能传入指定的类型
        public APIParameter(string key, int value) : this(key, (object)value) { }
        public APIParameter(string key, string value) : this(key, (object)value) { }
        public APIParameter(string key, float value) : this(key, (object)value) { }
        public APIParameter(string key, bool value) : this(key, (object)value) { }
    }
}
