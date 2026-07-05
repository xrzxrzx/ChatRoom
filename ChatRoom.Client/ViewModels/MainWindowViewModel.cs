using ChatRoom.Client.Function.ChatRoom;
using ChatRoom.Client.Messages;
using ChatRoom.Client.Models;
using ChatRoom.Client.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.Collections.ObjectModel;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace ChatRoom.Client.ViewModels;

public partial class MainWindowViewModel : ObservableRecipient, IRecipient<ValueChangedMessage<AuthResultMessage>>
{
    [ObservableProperty]
    public partial ObservableCollection<MessageInfoModel> MessageInfoList { get; set; } = new ObservableCollection<MessageInfoModel>();

    [ObservableProperty]
    public partial ObservableCollection<RoomInfoModel> RoomInfoList { get; set; } = new ObservableCollection<RoomInfoModel>();

    [ObservableProperty]
    public partial MessageInfoModel SelectedMessage { get; set; } = MessageInfoModel.NewSystemMessage(string.Empty);

    [ObservableProperty]
    public partial RoomInfoModel SelectedRoom { get; set; } = new RoomInfoModel(0, string.Empty, 0);

    [ObservableProperty]
    public partial string InputMessage { get; set; } = string.Empty; 

    [ObservableProperty]
    public partial UserInfoModel UserInfo { get; set; } = new UserInfoModel(0, "未登录");

    private readonly ILogger logger;
    private readonly IChatRoomService chatRoomService;

    public MainWindowViewModel(IChatRoomService chatRoomService, ILogger logger)
    {
        this.chatRoomService = chatRoomService;
        this.logger = logger;
        this.chatRoomService.OutputMessage += OnMessageReceived;

        IsActive = true;

        chatRoomService.OnLoginStatusChanged += OnLoginStatusChanged;
    }

    public bool IsLoggedIn
    {
        get => chatRoomService.IsLoggedIn;
    }

    private void OnLoginStatusChanged()
    {
        OnPropertyChanged(nameof(IsLoggedIn));
        OnPropertyChanged(nameof(CanLogin));

        if (!IsLoggedIn)
        {
            UserInfo = new UserInfoModel(0, "未登录");
            //MessageInfoList.Clear();
            RoomInfoList.Clear();
            MessageInfoList.Add(MessageInfoModel.NewSystemMessage("已退出登录"));
        }

        LoginCommand.NotifyCanExecuteChanged();
        LogoutCommand.NotifyCanExecuteChanged();
        SendMessageCommand.NotifyCanExecuteChanged();
        RefreshRoomListCommand.NotifyCanExecuteChanged();
    }

    private bool CanLogin => !IsLoggedIn;
    [RelayCommand(CanExecute = nameof(CanLogin))]
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

    [RelayCommand(CanExecute = nameof(IsLoggedIn))]
    private async Task LogoutAsync()
    {
        await chatRoomService.LogOutAsync();
        UserInfo = new UserInfoModel(0, "未登录");
        MessageInfoList.Clear();
        RoomInfoList.Clear();
        MessageInfoList.Add(MessageInfoModel.NewSystemMessage("已退出登录"));
    }

    [RelayCommand(CanExecute = nameof(IsLoggedIn))]
    private async Task SendMessageAsync()
    {
        if (!string.IsNullOrWhiteSpace(InputMessage))
        {
            await chatRoomService.SendMessageAsync(InputMessage);
            InputMessage = string.Empty;
        }
    }

    private bool CanRefreshRoomList => IsLoggedIn && _canRefreshRoomList;
    private bool _canRefreshRoomList = true;

    [RelayCommand(CanExecute = nameof(CanRefreshRoomList))]
    private async Task RefreshRoomListAsync()
    {
        _canRefreshRoomList = false;
        RefreshRoomListCommand.NotifyCanExecuteChanged();
        RoomInfoList.Clear();
        var rooms = await chatRoomService.GetRoomListAsync();

        foreach (var room in rooms)
        {
            RoomInfoList.Add(RoomInfoModel.FromRoomInfo(room));
        }
        _canRefreshRoomList = true;
        RefreshRoomListCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedRoomChanged(RoomInfoModel value)
    {
        if (value != null)
        {
            _ = HandleRoomSelectedAsync(value);
        }
    }

    private async Task HandleRoomSelectedAsync(RoomInfoModel room)
    {
        MessageInfoList.Clear();
        _ = await chatRoomService.JoinRoomAsync(room.Id);
        logger.Information($"加入房间: {room.Name} (房间ID: {room.Id})");

        //获取聊天记录
        //var messages = await chatRoomService.GetMessageListAsync(room.Id);
        //foreach (var message in messages)
        //{
        //    MessageInfoList.Add(MessageInfoModel.FromOutputMessageInfo(message));
        //}
    }

    private void OnMessageReceived(OutputMessageInfo message)
    {
        MessageInfoList.Add(MessageInfoModel.FromOutputMessageInfo(message));
    }

    public async void Receive(ValueChangedMessage<AuthResultMessage> message)
    {
        if (message.Value.Action == AuthAction.Login && message.Value.Result == AuthResult.Success)
        {
            UserInfo = UserInfoModel.FromUserInfo(new UserInfo
            {
                Id = chatRoomService.GetUserId(),
                NickName = chatRoomService.GetNickName()
            });
            MessageInfoList.Add(MessageInfoModel.NewSystemMessage("登录成功"));
            RoomInfoList.Clear();
            var rooms = await chatRoomService.GetRoomListAsync();
            foreach (var room in rooms)
            {
                RoomInfoList.Add(RoomInfoModel.FromRoomInfo(room));
            }
        }
        else if (message.Value.Action == AuthAction.Register && message.Value.Result == AuthResult.Success)
        {
            UserInfo = UserInfoModel.FromUserInfo(new UserInfo
            {
                Id = chatRoomService.GetUserId(),
                NickName = chatRoomService.GetNickName()
            });
            MessageInfoList.Add(MessageInfoModel.NewSystemMessage("注册成功"));
            RoomInfoList.Clear();
            var rooms = await chatRoomService.GetRoomListAsync();
            foreach (var room in rooms)
            {
                RoomInfoList.Add(RoomInfoModel.FromRoomInfo(room));
            }
        }
    }
}
