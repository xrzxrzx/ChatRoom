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

        public ObservableCollection<RoomInfo> RoomInfoList { get; set; } = new ObservableCollection<RoomInfo>();

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

        private bool CanConnect { get; set; } = true;
        [RelayCommand(CanExecute = nameof(CanConnect))]
        private async Task ConnectAsync()
        {
            CanConnect = false;
            chatRoomService.ConnectToServer();
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

        private bool CanRefreshRoomList { get; set; } = true;
        [RelayCommand(CanExecute = nameof(CanRefreshRoomList))]
        private async Task RefreshRoomListAsync()
        {
            CanRefreshRoomList = false;
            RoomInfoList.Clear();
            var rooms = await chatRoomService.GetRoomListAsync();

            foreach (var room in rooms)
            {
                RoomInfoList.Add(room);
            }
            CanRefreshRoomList = true;
        }

        private void OnMessageReceived(OutputMessageInfo message)
        {
            string senderName = message.SenderType switch
            {
                OutputMessageInfo.MessageSenderType.System => "系统",
                OutputMessageInfo.MessageSenderType.OtherUser => "其他用户",
                OutputMessageInfo.MessageSenderType.Self => "我",
                _ => "未知"
            };

            MessageInfoList.Add(new MessageInfo(message.SenderInfo.Id, senderName, message.Content));
        }

        public async void Receive(ValueChangedMessage<string> message)
        {
            if (message.Value == "登录成功")
            {
                UserId = chatRoomService.GetUserId();
                NickName = chatRoomService.GetNickName();
                MessageInfoList.Add(new MessageInfo(message.Value));
                RoomInfoList.Clear();
                var rooms = await chatRoomService.GetRoomListAsync();
                foreach (var room in rooms)
                {
                    RoomInfoList.Add(room);
                }
            }
            else if (message.Value == "注册成功")
            {
                UserId = chatRoomService.GetUserId();
                NickName = chatRoomService.GetNickName();
                MessageInfoList.Add(new MessageInfo(message.Value));
                RoomInfoList.Clear();
                var rooms = await chatRoomService.GetRoomListAsync();
                foreach (var room in rooms)
                {
                    RoomInfoList.Add(room);
                }
            }
        }
    }
}
