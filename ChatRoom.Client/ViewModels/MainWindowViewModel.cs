using ChatRoom.Client.Core.Network;
using ChatRoom.Client.Function.ChatRoom;
using ChatRoom.Client.Models;
using ChatRoom.Client.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;

namespace ChatRoom.Client.ViewModels
{
    public partial class MainWindowViewModel : ObservableRecipient, IRecipient<ValueChangedMessage<string>>
    {
        [ObservableProperty]
        public partial ObservableCollection<MessageInfo> MessageInfoList { get; set; } = new ObservableCollection<MessageInfo>();

        [ObservableProperty]
        public partial MessageInfo SelectedMessage { get; set; } = new MessageInfo(0, string.Empty, string.Empty);

        [ObservableProperty]
        public partial string InputMessage { get; set; } = string.Empty;

        [ObservableProperty]
        public partial int UserId { get; set; } = 0;

        [ObservableProperty]
        public partial string NickName { get; set; } = "未登录";

        private readonly IChatRoomService chatRoomService;

        public MainWindowViewModel(IChatRoomService chatRoomService)
        {
            this.chatRoomService = chatRoomService;
            this.chatRoomService.OutputMessage += OnMessageReceived;

            IsActive = true;
        }

        [RelayCommand]
        private async Task LoginAsync(string logType)
        {
            var loginWindow = App.Current.ServiceProvider.GetRequiredService<LoginWindow>();
            loginWindow.SetLogType(logType);

            // 监听 LoginWindow 的 Closed 事件来恢复 MainWindow 可用性
            loginWindow.Closed += (s, e) =>
            {
                if (App.Current.MainWindow?.Content is Microsoft.UI.Xaml.Controls.Control control)
                {
                    control.IsEnabled = true;
                }
                else if (App.Current.MainWindow?.Content is Microsoft.UI.Xaml.Controls.Panel panel)
                {
                    panel.IsHitTestVisible = true;
                    panel.Opacity = 1.0;
                }
            };

            // 禁用 MainWindow 的内容
            if (App.Current.MainWindow?.Content is Microsoft.UI.Xaml.Controls.Control contentControl)
            {
                contentControl.IsEnabled = false;
            }
            else if (App.Current.MainWindow?.Content is Microsoft.UI.Xaml.Controls.Panel contentPanel)
            {
                contentPanel.IsHitTestVisible = false;
                contentPanel.Opacity = 0.5; // 半透明化以提示不可用
            }

            loginWindow.Activate();
        }

        [RelayCommand]
        private async Task ConnectAsync()
        {
            //chatRoomService.ConnectToServer();

            MessageInfoList.Add(new MessageInfo("Connected to the server."));
            MessageInfoList.Add(new MessageInfo(1, "我", "你好。。。。。。。。。。。。。。阿达电视............测试阿萨是的啊实打实的气温气温", true));
            MessageInfoList.Add(new MessageInfo(1, "用户啊啊啊", "阿达阿达是的阿达阿达是的千问"));
        }

        [RelayCommand]
        private async Task SendMessageAsync()
        {
            if (!string.IsNullOrWhiteSpace(InputMessage))
            {
                await chatRoomService.SendMessageAsync(InputMessage);
                InputMessage = string.Empty;
            }
        }

        private void OnMessageReceived(OutputMessageInfo message)
        {
            string senderName = message.SenderType switch
            {
                OutputMessageInfo.MessageSenderType.System => "System",
                OutputMessageInfo.MessageSenderType.OtherUser => "Other User",
                OutputMessageInfo.MessageSenderType.Self => "You",
                _ => "Unknown"
            };

            MessageInfoList.Add(new MessageInfo(message.SenderInfo.Id, senderName, message.Content));
        }

        public void Receive(ValueChangedMessage<string> message)
        {
            if (message.Value == "登录成功")
            {
                UserId = chatRoomService.GetUserId();
                NickName = chatRoomService.GetNickName();
                MessageInfoList.Add(new MessageInfo(message.Value));
                
            }
            else if (message.Value == "注册成功")
            {
                UserId = chatRoomService.GetUserId();
                NickName = chatRoomService.GetNickName();
                MessageInfoList.Add(new MessageInfo(message.Value));
            }
        }
    }
}
