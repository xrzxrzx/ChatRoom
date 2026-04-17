using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ChatRoom.Client.Network;
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
        private ChatClient chatClient;

        public MainWindow()
        {
            InitializeComponent();

            ExtendsContentIntoTitleBar = true;
            AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Standard;

            // XamlRoot.RasterizationScale 需要在 UI 内容加载后才能获取，不使用 DLL 则必须依赖事件
            if (Content is FrameworkElement rootElement)
            {
                rootElement.Loaded += (s, e) => SetLogicalSize(530, 500);
            }

            chatClient = new ChatClient("127.0.0.1", 12345);
            chatClient.OnMessageEventReceived += MessageEventHandler;
            chatClient.OnNoticeEventReceived += NoticeEventHandler;
            chatClient.OnRequestEventReceived += RequestEventHandler;
            chatClient.OnErrorEventReceived += ErrorEventHandler;
            chatClient.OnHeartbeatEventReceived += HeartbeatEventHandler;
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

            ClientMessageBag messageBag = new ClientMessageBag("message")
                                            .AddParameter("data", MessageTextBox.Text.Trim());

            await chatClient.SendMessageAsync(messageBag);
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            await chatClient.ConnectAsync();
            chatClient.StartReceive();
        }

        private void MessageEventHandler(JObject data)
        {
            string sender = data["sender"]?.ToString() ?? "Unknown";
            string message = data["message"]?.ToString() ?? string.Empty;
            UpdateChatMessage($"{sender}: {message}");
        }

        private void NoticeEventHandler(JObject data)
        {
            string notice = data["notice"]?.ToString() ?? string.Empty;
            UpdateChatMessage($"[Notice] {notice}");
        }

        private void RequestEventHandler(JObject data)
        {
            string request = data["request"]?.ToString() ?? string.Empty;
            UpdateChatMessage($"[Request] {request}");
        }

        private void ErrorEventHandler(int recode, string message)
        {
            UpdateChatMessage($"[Error {recode}] {message}");
        }

        private void HeartbeatEventHandler(JObject data)
        {
            // 心跳事件通常不包含具体数据，这里仅记录接收时间
            UpdateChatMessage($"[Heartbeat] {DateTime.Now}");
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
