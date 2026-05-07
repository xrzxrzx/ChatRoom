using ChatRoom.Client.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ChatRoom.Client.Views.LoginWindowPages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class LoginPage : Page
    {
        public LoginPage()
        {
            InitializeComponent();

            if (Content is FrameworkElement frameworkElement)
            {
                frameworkElement.DataContext = App.Current.ServiceProvider.GetRequiredService<LoginWindowViewModel>();
            }
        }
    }
}