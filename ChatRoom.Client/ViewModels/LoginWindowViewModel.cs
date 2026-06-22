using ChatRoom.Client.Function.ChatRoom;
using ChatRoom.Client.Messages;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace ChatRoom.Client.ViewModels
{
    public partial class LoginWindowViewModel : ObservableValidator
    {
        private string userId = string.Empty;

        [Required(ErrorMessage = "请输入用户ID")]
        [RegularExpression(@"^[1-9]\d*$", ErrorMessage = "用户ID必须为正整数")]
        public string UserId
        {
            get => userId;
            set
            {
                if (SetProperty(ref userId, value, true))
                {
                    UserIdValidationMessage = GetFirstErrorMessage(nameof(UserId));
                }
            }
        }

        private string nickName = string.Empty;

        [Required(ErrorMessage = "请输入昵称")]
        public string NickName
        {
            get => nickName;
            set
            {
                if (SetProperty(ref nickName, value, true))
                {
                    NickNameValidationMessage = GetFirstErrorMessage(nameof(NickName));
                }
            }
        }

        private string password = string.Empty;

        [Required(ErrorMessage = "请输入密码")]
        [MinLength(6, ErrorMessage = "密码长度至少为6位")]
        public string Password
        {
            get => password;
            set
            {
                if (SetProperty(ref password, value, true))
                {
                    PasswordValidationMessage = GetFirstErrorMessage(nameof(Password));
                    PasswordAgainValidationMessage = GetFirstErrorMessage(nameof(PasswordAgain));
                }
            }
        }

        private string passwordAgain = string.Empty;

        [Required(ErrorMessage = "请再次输入密码")]
        [MinLength(6, ErrorMessage = "密码长度至少为6位")]
        [Compare(nameof(Password), ErrorMessage = "两次输入的密码不一致")]
        public string PasswordAgain
        {
            get => passwordAgain;
            set
            {
                if (SetProperty(ref passwordAgain, value, true))
                {
                    PasswordAgainValidationMessage = GetFirstErrorMessage(nameof(PasswordAgain));
                }
            }
        }

        private string userIdValidationMessage = string.Empty;

        public string UserIdValidationMessage
        {
            get => userIdValidationMessage;
            set => SetProperty(ref userIdValidationMessage, value);
        }

        private string nickNameValidationMessage = string.Empty;

        public string NickNameValidationMessage
        {
            get => nickNameValidationMessage;
            set => SetProperty(ref nickNameValidationMessage, value);
        }

        private string passwordValidationMessage = string.Empty;

        public string PasswordValidationMessage
        {
            get => passwordValidationMessage;
            set => SetProperty(ref passwordValidationMessage, value);
        }

        private string passwordAgainValidationMessage = string.Empty;

        public string PasswordAgainValidationMessage
        {
            get => passwordAgainValidationMessage;
            set => SetProperty(ref passwordAgainValidationMessage, value);
        }

        IChatRoomService chatRoomService;

        public LoginWindowViewModel(IChatRoomService chatRoomService)
        {
            this.chatRoomService = chatRoomService;
        }

        [RelayCommand]
        private async Task LoginAsync()
        {
            ValidateProperty(UserId, nameof(UserId));
            ValidateProperty(Password, nameof(Password));
            UserIdValidationMessage = GetFirstErrorMessage(nameof(UserId));
            PasswordValidationMessage = GetFirstErrorMessage(nameof(Password));

            if (HasErrors)
            {
                return;
            }

            if (!int.TryParse(UserId, out var userId))
            {
                UserIdValidationMessage = "用户ID必须为正整数";
                return;
            }

            var result = await chatRoomService.LogInAsync(userId, Password);
            if (result)
            {
                WeakReferenceMessenger.Default.Send(new ValueChangedMessage<AuthResultMessage>(
                    new AuthResultMessage { 
                        Action = AuthAction.Login,
                        Result = AuthResult.Success
                    }));
            }
        }


        [RelayCommand]
        private async Task RegisterAsync()
        {
            ValidateAllProperties();
            UserIdValidationMessage = GetFirstErrorMessage(nameof(UserId));
            NickNameValidationMessage = GetFirstErrorMessage(nameof(NickName));
            PasswordValidationMessage = GetFirstErrorMessage(nameof(Password));
            PasswordAgainValidationMessage = GetFirstErrorMessage(nameof(PasswordAgain));

            if (HasErrors)
            {
                return;
            }

            if (!int.TryParse(UserId, out var userId))
            {
                UserIdValidationMessage = "用户ID必须为正整数";
                return;
            }

            var result = await chatRoomService.RegisterAsync(userId, Password, NickName);
            if(result)
            {
                WeakReferenceMessenger.Default.Send(new ValueChangedMessage<AuthResultMessage>(
                    new AuthResultMessage { 
                        Action = AuthAction.Register,
                        Result = AuthResult.Success
                    }));
            }
        }

        private string GetFirstErrorMessage(string propertyName)
        {
            return GetErrors(propertyName)
                .FirstOrDefault()?.ErrorMessage ?? string.Empty;
        }
    }
}
