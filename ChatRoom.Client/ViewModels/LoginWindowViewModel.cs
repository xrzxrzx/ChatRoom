using ChatRoom.Client.Function.ChatRoom;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ChatRoom.Client.ViewModels
{
    public partial class LoginWindowViewModel : ObservableObject
    {
        [ObservableProperty]
        public partial int UserId { get; set; } = 0;
        [ObservableProperty]
        public partial string NickName { get; set; } = string.Empty;
        [ObservableProperty]
        public partial string Password { get; set; } = string.Empty;
        [ObservableProperty]
        public partial string PasswordAgain { get; set; } = string.Empty;

        IChatRoomService chatRoomService;

        public LoginWindowViewModel(IChatRoomService chatRoomService)
        {
            this.chatRoomService = chatRoomService;
        }

        [RelayCommand]
        private async Task LoginAsync()
        {
            //var result = await chatRoomService.LogInAsync(UserId, Password);
            var result = true;
            if (result)
            {
                WeakReferenceMessenger.Default.Send(new ValueChangedMessage<string>("登录成功"));
            }
        }

        [RelayCommand]
        private async Task RegisterAsync()
        {
            //var result = await chatRoomService.RegisterAsync(UserId, Password);
            var result = true;
            if(result)
            {
                WeakReferenceMessenger.Default.Send(new ValueChangedMessage<string>("注册成功"));
            }
        }
    }
}
