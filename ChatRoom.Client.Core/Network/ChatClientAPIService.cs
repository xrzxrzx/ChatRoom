using ChatRoom.Client.Core.Network.MessageBag.APIMessageBag;
using Newtonsoft.Json.Linq;
using Serilog;
using Serilog.Core;
using System.Collections.Concurrent;

namespace ChatRoom.Client.Core.Network
{
    public class ChatClientAPIService : IChatClientAPIService
    {
        private ILogger logger;

        ConcurrentDictionary<string ,TaskCompletionSource<ResponseMessageBag>> _responseWaiters;

        public event IChatClientAPIService.SendMessageAsyncDelegate? SendMessageAsync;

        public ChatClientAPIService(ILogger logger)
        {
            _responseWaiters = new ConcurrentDictionary<string, TaskCompletionSource<ResponseMessageBag>>();
            this.logger = logger;
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

            //设置十秒超时
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            try
            {
                return await tcs.Task.WaitAsync(cts.Token);
            }
            catch (TimeoutException)
            {
                logger.Warning($"API '{apiName}' 调用超时 (10秒), Echo: {echo}");
                _responseWaiters.TryRemove(echo, out _); // 清理超时的任务
                return new ResponseMessageBag(false, "API 调用超时");
            }
        }

        public void OnResponseReceived(ResponseMessageBag messageBag)
        {
            string echo = messageBag.Echo;
            TaskCompletionSource<ResponseMessageBag>? tcs;

            _responseWaiters.TryGetValue(echo, out tcs);
            if(tcs == null)
            {
                logger.Warning($"未找到对应的 API 响应等待者，Echo: {echo}");
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
