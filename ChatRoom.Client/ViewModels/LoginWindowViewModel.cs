using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Text;

namespace ChatRoom.Client.ViewModels
{
    public partial class LoginWindowViewModel : ObservableRecipient, IRecipient<string>
    {
        [RelayCommand]
        private void Login()
        {
            
        }

        [RelayCommand]
        private void Register()
        {
        }

        public void Receive(string message)
        {
            throw new NotImplementedException();
        }
    }
}
