using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using ChatRoom.Client.Core.Network;
using Newtonsoft.Json.Linq;
using ChatRoom.Client.ViewModels;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ChatRoom.Client
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private MainWindowViewModel viewModel;

        public MainWindow(IServiceProvider serviceProvider)
        {
            viewModel = new MainWindowViewModel(serviceProvider);

            InitializeComponent();

            ExtendsContentIntoTitleBar = true;
            AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Standard;

            // XamlRoot.RasterizationScale 需要在 UI 内容加载后才能获取，不使用 DLL 则必须依赖事件
            if (Content is FrameworkElement rootElement)
            {
                rootElement.Loaded += (s, e) => SetLogicalSize(530, 500);
            }

            // 监听 MessageInfoList 的集合变化，一旦有新数据添加就滚动到底部
            viewModel.MessageInfoList.CollectionChanged += (s, e) =>
            {
                if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
                {
                    // 确保在UI线程执行，将 ListView 滚动到最后一个元素
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        var items = viewModel.MessageInfoList;
                        if (items.Count > 0)
                        {
                            ChatListView.ScrollIntoView(items[items.Count - 1]);
                        }
                    });
                }
            };
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
    }
}
