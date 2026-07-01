using ChatRoom.Client.Views.LoginWindowPages;
using ChatRoom.Client.Messages;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using Windows.UI.ApplicationSettings;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ChatRoom.Client.Views
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();

            ExtendsContentIntoTitleBar = true;

            // XamlRoot.RasterizationScale 需要在 UI 内容加载后才能获取，不使用 DLL 则必须依赖事件
            if (Content is FrameworkElement rootElement)
            {
                rootElement.Loaded += (s, e) => 
                {
                    SetLogicalSize(350, 580);
                };
            }

            WeakReferenceMessenger.Default.Register<ValueChangedMessage<AuthResultMessage>>(this, (_, message) =>
            {
                if (message.Value.Result == AuthResult.Success)
                {
                    Close();
                }
            });

            Closed += (_, _) => WeakReferenceMessenger.Default.UnregisterAll(this);
        }

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

        private void RootNavigation_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.InvokedItemContainer is NavigationViewItem item && item.Tag != null)
            {
                Type pageType = item.Tag switch
                {
                    "LoginPage" => typeof(LoginPage),
                    "RegisterPage" => typeof(RegisterPage),
                    _ => throw new ArgumentException("未定义的页面类型：" + item.Tag)
                };
                contentFrame.Navigate(pageType);
            }
        }

        internal void SetLogType(string logType)
        {
            if(logType == "Login")
            {
                contentFrame.Navigate(typeof(LoginPage));
            }
            else if(logType == "Register")
            {
                contentFrame.Navigate(typeof(RegisterPage));
            }
        }
    }
}
