using ChatRoom.Client.Core.Common;
using ChatRoom.Client.Core.Network;
using ChatRoom.Client.Function.ChatRoom;
using ChatRoom.Client.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Serilog;
using Serilog.Core;
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

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            _serviceProvider = ConfigureServices();

            InitializeComponent();
        }

        private static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            //日志服务
            services.AddSingleton<ILogger>(_ => 
            {
                return new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File("logs/chatclient.log", rollingInterval: RollingInterval.Day)
                .CreateLogger();
            });

            //UI层服务
            services.AddSingleton<IChatClientService, ChatClientService>();
            services.AddSingleton<IChatRoomService, ChatRoomService>();
            services.AddTransient<MainWindowViewModel>();
            services.AddTransient<MainWindow>(sp => 
            {
                var window = new MainWindow();
                if (window.Content is FrameworkElement frameworkElement)
                {
                    frameworkElement.DataContext = sp.GetRequiredService<MainWindowViewModel>();
                }
                return window;
            });

            //核心网络服务
            services.AddSingleton<IChatClientCoreService, ChatClientCoreService>();
            services.AddSingleton<IChatClientAPIService, ChatClientAPIService>();
            services.AddSingleton<IChatClientEventService, ChatClientEventService>();
            services.AddSingleton<IChatClientConfigService, ChatClientConfigService>();

            return services.BuildServiceProvider();
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            _window = _serviceProvider.GetService<MainWindow>();
            if (_window == null)
            {
                throw new InvalidOperationException("Failed to create the main window.");
            }
            _window.Activate();
        }
    }
}
