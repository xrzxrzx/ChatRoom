using ChatRoom.Client.Core.Common;
using ChatRoom.Client.Core.Network.MessageBag;
using ChatRoom.Client.Core.Network.MessageBag.APIMessageBag;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
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

        public ChatClientService(IChatClientCoreService chatClientCoreService, IChatClientAPIService chatClientAPIService, IChatClientEventService chatClientEventService, IChatClientConfigService chatClientConfigService)
        {
            //核心服务
            _coreService = chatClientCoreService;
            _apiService = chatClientAPIService;
            _eventService = chatClientEventService;

            //配置服务
            _configService = chatClientConfigService;

            _coreService.OnEventReceived += _eventService.OnEventReceived;
            _coreService.OnResponseReceived += _apiService.OnResponseReceived;
            _apiService.SendMessageAsync += _coreService.SendMessageAsync;
        }

        public async Task ConnectAsync()
        {
            ChatClientConfig config = _configService.GetConfig();
            await _coreService.ConnectAsync(config);
        }

        public void StartReceiving()
        {
            _coreService.StartReceive();
        }

        public async Task<ResponseMessageBag> CallAPIAsync(string apiName, params APIParameter[] parameters)
        {
            return await _apiService.CallAPIAsync(apiName, parameters);
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void SubscribeToEvent<T>(Action<T> handler) where T : EventMessageBag
        {
            _eventService.Subscribe(handler);
        }
    }
}
