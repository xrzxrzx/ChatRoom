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

        public MainWindowViewModel(IServiceProvider serviceProvider)
        {
            chatRoomService = serviceProvider.GetRequiredService<IChatRoomService>();
        }

        [RelayCommand]
        private async Task ConnectAsync()
        {
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
    }
}
