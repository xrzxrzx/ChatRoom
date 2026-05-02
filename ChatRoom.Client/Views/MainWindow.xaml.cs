using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using ChatRoom.Client.Core.Network;
using Newtonsoft.Json.Linq;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ChatRoom.Client
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private IChatClientService chatService;

        public MainWindow(IChatClientService chatClientService)
        {
            chatService = chatClientService;

            InitializeComponent();

            ExtendsContentIntoTitleBar = true;
            AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Standard;

            // XamlRoot.RasterizationScale 需要在 UI 内容加载后才能获取，不使用 DLL 则必须依赖事件
            if (Content is FrameworkElement rootElement)
            {
                rootElement.Loaded += (s, e) => SetLogicalSize(530, 500);
            }
            
        }

        /// <summary>
        /// 通过 WinUI 3 原生 XamlRoot 获取显示器缩放系数并设置窗口的逻辑大小
        /// </summary>
        private void SetLogicalSize(double logicalWidth, double logicalHeight)
        {
            if (Content?.XamlRoot is XamlRoot xamlRoot)
            {
                // 获取当前显示器的缩放比例（例如150%时该值为1.5，完全等同于以 dpi/96.0 换算）
                double scale = xamlRoot.RasterizationScale;

                int pixelWidth = (int)Math.Round(logicalWidth * scale);
                int pixelHeight = (int)Math.Round(logicalHeight * scale);

                AppWindow.Resize(new Windows.Graphics.SizeInt32(pixelWidth, pixelHeight));
            }
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            if (MessageTextBox.Text.Trim() == string.Empty)
                return;

            await chatService.CallAPIAsync("message", new("sender", 123),
                                                    new("message", "测试"));
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            await chatClient.ConnectAsync();
            chatClient.StartReceiving();
        }

        private void UpdateChatMessage(string message)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                ChatTextBox.Text += message + Environment.NewLine;
            });
        }
    }
}
