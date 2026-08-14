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
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
        this.chatRoomService.RoomListUpdated += OnRoomListUpdated;

        IsActive = true;

        chatRoomService.OnLoginStatusChanged += OnLoginStatusChanged;
    }

    public bool IsLoggedIn
    {
        get => chatRoomService.IsLoggedIn;
    }

    //事件回调可能来自线程池，集合更新统一切换到 UI 线程
    private void RunOnUiThread(Action action)
    {
        var dispatcherQueue = App.Current.MainWindow.DispatcherQueue;
        if (dispatcherQueue.HasThreadAccess)
        {
            action();
        }
        else
        {
            dispatcherQueue.TryEnqueue(() => action());
        }
    }

    private void OnLoginStatusChanged()
    {
        RunOnUiThread(() =>
        {
            OnPropertyChanged(nameof(IsLoggedIn));
            OnPropertyChanged(nameof(CanLogin));

            if (!IsLoggedIn)
            {
                UserInfo = new UserInfoModel(0, "未登录");
                RoomInfoList.Clear();
                MessageInfoList.Clear();
                MessageInfoList.Add(MessageInfoModel.NewSystemMessage("已退出登录"));
            }

            LoginCommand.NotifyCanExecuteChanged();
            LogoutCommand.NotifyCanExecuteChanged();
            SendMessageCommand.NotifyCanExecuteChanged();
            RefreshRoomListCommand.NotifyCanExecuteChanged();
        });
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
        //已在目标房间时不再重复 join（避免切房通知刷屏）
        if (value != null && value.Id >= 0 && chatRoomService.JoinedRoomId != value.Id)
        {
            _ = HandleRoomSelectedAsync(value);
        }
    }

    private async Task HandleRoomSelectedAsync(RoomInfoModel room)
    {
        MessageInfoList.Clear();
        _ = await chatRoomService.JoinRoomAsync(room.Id);
        logger.Information($"加入房间: {room.Name} (房间ID: {room.Id})");
    }

    //服务端推送的房间列表全量更新（含新增/移除/人数变化）
    private void OnRoomListUpdated(List<RoomInfo> roomList)
    {
        RunOnUiThread(() =>
        {
            int previousSelectedId = SelectedRoom?.Id ?? -1;

            RoomInfoList.Clear();
            foreach (var room in roomList)
            {
                RoomInfoList.Add(RoomInfoModel.FromRoomInfo(room));
            }

            var stillExists = RoomInfoList.FirstOrDefault(r => r.Id == previousSelectedId);
            if (stillExists != null)
            {
                //保留选中；OnSelectedRoomChanged 会依据 JoinedRoomId 跳过重复 join
                SelectedRoom = stillExists;
            }
            else if (previousSelectedId >= 0)
            {
                SelectedRoom = new RoomInfoModel(0, string.Empty, 0);
                MessageInfoList.Clear();
                MessageInfoList.Add(MessageInfoModel.NewSystemMessage("房间已关闭"));
            }
        });
    }

    //创建房间（由 UI 输入房间名后调用）
    public async Task CreateRoomAsync(string roomName)
    {
        int? roomId = await chatRoomService.CreateRoomAsync(roomName);
        if (roomId is not int id)
        {
            return;
        }

        var rooms = await chatRoomService.GetRoomListAsync();
        RunOnUiThread(() =>
        {
            RoomInfoList.Clear();
            foreach (var room in rooms)
            {
                RoomInfoList.Add(RoomInfoModel.FromRoomInfo(room));
            }

            var created = RoomInfoList.FirstOrDefault(r => r.Id == id);
            if (created != null)
            {
                SelectedRoom = created;
            }
        });
    }

    private void OnMessageReceived(OutputMessageInfo message)
    {
        RunOnUiThread(() => MessageInfoList.Add(MessageInfoModel.FromOutputMessageInfo(message)));
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
