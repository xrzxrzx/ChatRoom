using ChatRoom.Client.Core.Network;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ChatRoom.Client
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private Window? _window;

        private readonly IServiceProvider _serviceProvider;
        public new static App Current => (App)Application.Current;

        private IChatClientService _chatClientService;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            _serviceProvider = ConfigureServices();

            _chatClientService = _serviceProvider.GetRequiredService<IChatClientService>();

            InitializeComponent();
        }

        private static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            // 注册服务
            services.AddSingleton<IChatClientService, ChatClientService>();

            return services.BuildServiceProvider();
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            _window = new MainWindow(_chatClientService);
            _window.Activate();
        }
    }
}
