using ChatRoom.Client.Core.Common;
using ChatRoom.Client.Core.Network.MessageBag.ClientMessageBag;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;

namespace ChatRoom.Client.Core.Network
{
    public interface IChatClientService : IDisposable
    {
        public Task ConnectAsync();
        public void StartReceiving();
        public Task<ResponseMessageBag> SendAPIAsync(string apiName, params APIParameter[] parameters);
    }

    public class ChatClientService : IChatClientService
    {
        private readonly IServiceProvider _serviceProvider;

        private readonly IChatClientCoreService _coreService;
        private readonly IChatClientAPIService _apiService;
        private readonly IChatClientEventService _eventService;

        private readonly IChatClientConfigService _configService;

        public ChatClientService()
        {
            _serviceProvider = ConfigureServices();

            //核心服务
            _coreService = _serviceProvider.GetRequiredService<IChatClientCoreService>();
            _apiService = _serviceProvider.GetRequiredService<IChatClientAPIService>();
            _eventService = _serviceProvider.GetRequiredService<IChatClientEventService>();

            //配置服务
            _configService = _serviceProvider.GetRequiredService<IChatClientConfigService>();

            _coreService.OnEventReceived += _eventService.OnEventReceived;
        }

        private IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            // 注册服务
            services.AddSingleton<IChatClientCoreService, ChatClientCoreService>();
            services.AddSingleton<IChatClientAPIService, ChatClientAPIService>();
            services.AddSingleton<IChatClientEventService, ChatClientEventService>();

            services.AddSingleton<IChatClientConfigService, ChatClientConfigService>();

            return services.BuildServiceProvider();
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
        public async Task<ResponseMessageBag> SendAPIAsync(string apiName, params APIParameter[] parameters)
        {
            return await _apiService.SendAPIAsync(apiName, parameters);
        }
        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
