using ChatRoom.Client.Core.Network;
using ChatRoom.Client.Function.ChatRoom;
using ChatRoom.Client.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;

namespace ChatRoom.Client.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        [ObservableProperty]
        public partial ObservableCollection<MessageInfo> MessageInfoList { get; set; } = new ObservableCollection<MessageInfo>();

        [ObservableProperty]
        public partial MessageInfo SelectedMessage { get; set; } = new MessageInfo(0, string.Empty, string.Empty);

        [ObservableProperty]
        public partial string InputMessage { get; set; } = string.Empty;

        private readonly IChatRoomService chatRoomService;

        public MainWindowViewModel(IChatRoomService chatRoomService)
        {
            this.chatRoomService = chatRoomService;
            this.chatRoomService.OutputMessage += OnMessageReceived;
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
    }
}
