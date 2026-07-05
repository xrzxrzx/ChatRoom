using ChatRoom.Client.Core.Common;
using ChatRoom.Client.Core.Network.MessageBag;
using ChatRoom.Client.Core.Network.MessageBag.APIMessageBag;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Serilog;
using System;
using System.Threading.Tasks;

namespace ChatRoom.Client.Core.Network
{
    public class ChatClientService : IChatClientService
    {
        private readonly IChatClientCoreService _coreService;
        private readonly IChatClientAPIService _apiService;
        private readonly IChatClientEventService _eventService;

        private readonly IChatClientConfigService _configService;
        private readonly ILogger _logger;

        public ChatClientService(IChatClientCoreService chatClientCoreService, IChatClientAPIService chatClientAPIService, IChatClientEventService chatClientEventService, IChatClientConfigService chatClientConfigService, ILogger logger)
        {
            //核心服务
            _coreService = chatClientCoreService;
            _apiService = chatClientAPIService;
            _eventService = chatClientEventService;

            //配置服务
            _configService = chatClientConfigService;

            //日志服务
            _logger = logger;

            _coreService.OnEventReceived += _eventService.OnEventReceived;
            _coreService.OnResponseReceived += _apiService.OnResponseReceived;
            _apiService.SendMessageAsync += _coreService.SendMessageAsync;
        }

        public async Task<bool> ConnectAsync()
        {
            var config = _configService.GetConfig();
            int maxAttempts = config.ConnectionRetryCount;
            var timeout = TimeSpan.FromMilliseconds(config.ConnectionTimeout);

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    var connectTask = _coreService.ConnectAsync(config);
                    var completed = await Task.WhenAny(connectTask, Task.Delay(timeout));

                    if (completed == connectTask)
                    {
                        await connectTask;
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warning($"连接服务器失败 (尝试 {attempt}/{maxAttempts}): {ex.Message}");
                }
            }
            _logger.Error($"连接服务器失败，已尝试 {maxAttempts} 次。");
            return false;
        }

        public async Task DisconnectAsync()
        {
            await _coreService.DisconnectAsync();
        }

        public void StartReceiving()
        {
            _coreService.StartReceive();
        }

        public async Task<ResponseMessageBag> CallAPIAsync(string apiName, string token, params APIParameter[] parameters)
        {
            return await _apiService.CallAPIAsync(apiName, token, parameters);
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void SubscribeToEvent<T>(Action<T> handler) where T : EventMessageBag
        {
            _eventService.Subscribe(handler);
        }

        public void AddReconnectHandler(IChatClientCoreService.ReconnectHandler reconnectHandler)
        {
            _coreService.Reconnect += reconnectHandler;
        }
    }
}
