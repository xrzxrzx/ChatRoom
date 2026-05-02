using ChatRoom.Client.Core.Network.MessageBag.ClientMessageBag;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;

namespace ChatRoom.Client.Core.Network
{
    internal interface IChatClientAPIService : IDisposable
    {
        Task<ResponseMessageBag> CallAPIAsync(string apiName, IEnumerable<APIParameter> parameters);
        void OnResponseReceived(ResponseMessageBag messageBag);

        delegate Task SendMessageAsyncDelegate(string message);
        event SendMessageAsyncDelegate SendMessageAsync;
    }

    internal class ChatClientAPIService : IChatClientAPIService
    {
        ConcurrentDictionary<string ,TaskCompletionSource<ResponseMessageBag>> _responseWaiters;

        public event IChatClientAPIService.SendMessageAsyncDelegate? SendMessageAsync;

        public ChatClientAPIService()
        {
            _responseWaiters = new ConcurrentDictionary<string, TaskCompletionSource<ResponseMessageBag>>();
        }

        public async Task<ResponseMessageBag> CallAPIAsync(string apiName, IEnumerable<APIParameter> parameters)
        {
            string echo = Guid.NewGuid().ToString();
            RequestMessageBag messageBag = new RequestMessageBag(apiName).SetEcho(echo);

            foreach(var param in parameters)
            {
                messageBag.AddParameter(param.Key, (dynamic)param.Value);
            }

            var tcs = new TaskCompletionSource<ResponseMessageBag>();
            _responseWaiters.TryAdd(echo, tcs);
            
            SendMessageAsync?.Invoke(messageBag.ToJsonString());

            return await tcs.Task;
        }

        public void OnResponseReceived(ResponseMessageBag messageBag)
        {
            string echo = messageBag.Echo;
            TaskCompletionSource<ResponseMessageBag>? tcs;

            _responseWaiters.TryGetValue(echo, out tcs);
            if(tcs == null)
            {
                //TODO 日志记录未找到对应的等待者
            }

            tcs?.SetResult(messageBag);
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
