using ChatRoom.Client.Function.ChatRoom;
using ChatRoom.Client.Messages;
using ChatRoom.Client.Models;
using ChatRoom.Client.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace ChatRoom.Client.ViewModels;

public partial class MainWindowViewModel : ObservableRecipient, IRecipient<ValueChangedMessage<AuthResultMessage>>
{
    [ObservableProperty]
    public partial ObservableCollection<MessageInfoModel> MessageInfoList { get; set; } = new ObservableCollection<MessageInfoModel>();

    [ObservableProperty]
    public partial ObservableCollection<RoomInfoModel> RoomInfoList { get; set; } = new ObservableCollection<RoomInfoModel>();

    [ObservableProperty]
    public partial MessageInfoModel SelectedMessage { get; set; } = new MessageInfoModel(0, string.Empty, string.Empty);

    [ObservableProperty]
    public partial RoomInfoModel SelectedRoom { get; set; } = new RoomInfoModel(0, string.Empty, 0);

    [ObservableProperty]
    public partial string InputMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial UserInfoModel UserInfo { get; set; } = new UserInfoModel(0, "未登录");

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
            RoomInfoList.Add(RoomInfoModel.FromRoomInfo(room));
        }
        CanRefreshRoomList = true;
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
            MessageInfoList.Add(new(0, string.Empty, "登录成功"));
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
            MessageInfoList.Add(new(0, string.Empty, "注册成功"));
            RoomInfoList.Clear();
            var rooms = await chatRoomService.GetRoomListAsync();
            foreach (var room in rooms)
            {
                RoomInfoList.Add(RoomInfoModel.FromRoomInfo(room));
            }
        }
    }
}
