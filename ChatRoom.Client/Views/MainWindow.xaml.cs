using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Specialized;
using Windows.System;
using ChatRoom.Client.ViewModels;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ChatRoom.Client.Views
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            ExtendsContentIntoTitleBar = true;

            // XamlRoot.RasterizationScale 需要在 UI 内容加载后才能获取，不使用 DLL 则必须依赖事件
            if (Content is FrameworkElement rootElement)
            {
                rootElement.Loaded += (s, e) =>
                {
                    SetLogicalSize(800, 650);
                    if ((Content as FrameworkElement)?.DataContext is MainWindowViewModel viewModel)
                    {
                        //新消息自动滚动到底部
                        viewModel.MessageInfoList.CollectionChanged += OnMessageCollectionChanged;
                    }
                };
            }

            //窗口关闭时释放服务
            Closed += (_, _) => App.Current.ShutdownServices();
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

        private void OnMessageCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add && ChatListView.Items.Count > 0)
            {
                ChatListView.ScrollIntoView(ChatListView.Items[^1]);
            }
        }

        //回车发送消息
        private void MessageTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter && (Content as FrameworkElement)?.DataContext is MainWindowViewModel viewModel)
            {
                e.Handled = true;
                if (viewModel.SendMessageCommand.CanExecute(null))
                {
                    viewModel.SendMessageCommand.Execute(null);
                }
            }
        }

        //创建房间：弹出命名对话框，成功后刷新列表并选中新房间
        private async void CreateRoomButton_Click(object sender, RoutedEventArgs e)
        {
            if (Content?.XamlRoot is not XamlRoot xamlRoot || (Content as FrameworkElement)?.DataContext is not MainWindowViewModel viewModel)
            {
                return;
            }

            var input = new TextBox
            {
                PlaceholderText = "请输入房间名（≤32 字符）",
                MaxLength = 32
            };

            var dialog = new ContentDialog
            {
                XamlRoot = xamlRoot,
                Title = "创建房间",
                PrimaryButtonText = "创建",
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Primary,
                Content = input
            };

            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary)
            {
                return;
            }

            var roomName = input.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(roomName))
            {
                return;
            }

            await viewModel.CreateRoomAsync(roomName);
        }
    }
}
