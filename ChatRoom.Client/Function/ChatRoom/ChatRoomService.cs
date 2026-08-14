using ChatRoom.Client.Core.Network;
using ChatRoom.Client.Core.Network.MessageBag.APIMessageBag;
using Newtonsoft.Json.Linq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ChatRoom.Client.Function.ChatRoom;

public class ChatRoomService : IChatRoomService
{
    private IChatClientService chatClientService;
    private ILogger logger;
    private UserInfo userInfo;
    private string sessionToken = string.Empty;

    private bool isLoggedIn = false;
    public bool IsLoggedIn
    {
        get => isLoggedIn;
        private set
        {
            if (isLoggedIn != value)
            {
                isLoggedIn = value;
                OnLoginStatusChanged?.Invoke();
            }
        }
    }

    public int? JoinedRoomId { get; private set; }

    public event IChatRoomService.OutputMessageDelegate? OutputMessage;
    public event IChatRoomService.OnLoginStatusChangedDelegate? OnLoginStatusChanged;
    public event IChatRoomService.RoomListUpdatedDelegate? RoomListUpdated;

    public ChatRoomService(IChatClientService chatClientService, ILogger logger)
    {
        this.chatClientService = chatClientService;
        this.logger = logger;
        userInfo = new UserInfo();

        ChatRoomFunction.SetLogger(logger);
        ChatRoomFunction.SetOutputMessageDelegate(message => OutputMessage?.Invoke(message));

        this.chatClientService.SubscribeToEvent<MessageEvent>(ChatRoomFunction.OutputMessage);
        this.chatClientService.SubscribeToEvent<UpdateEvent>(OnUpdateEvent);
        this.chatClientService.SubscribeToEvent<NoticeEvent>(OnNoticeEvent);
        this.chatClientService.SubscribeToEvent<HeartbeatEvent>(OnHeartbeatEvent);
        this.chatClientService.AddReconnectHandler(ReconnectHandler);
    }

    public async Task<ResponseMessageBag> CallAPIAsync(string apiName, params APIParameter[] parameters)
    {
        return await chatClientService.CallAPIAsync(apiName, sessionToken, parameters);
    }

    public async Task<bool> ConnectToServer()
    {
        var result = await chatClientService.ConnectAsync();
        if (result == false)
        {
            OutputMessage?.Invoke(new(OutputMessageInfo.MessageSenderType.System, "连接服务器失败"));
            return result;
        }

        chatClientService.StartReceiving();
        return true;
    }

    public async Task DisconnectToServer()
    {
        await chatClientService.DisconnectAsync();
        sessionToken = string.Empty;
        JoinedRoomId = null;
        IsLoggedIn = false;
    }

    public async Task<bool> RegisterAsync(string password, string nickname)
    {
        if (IsLoggedIn)
        {
            OutputMessage?.Invoke(new(OutputMessageInfo.MessageSenderType.System, "已登录，无法注册新用户"));
            return false;
        }

        var response = await chatClientService.CallAPIAsync("register", string.Empty,
                                                 new("password", password),
                                                 new("nickname", nickname));
        if (response.Success == false)
        {
            OutputMessage?.Invoke(new(OutputMessageInfo.MessageSenderType.System, $"注册失败: {response.ErrorMessage}"));
            return false;
        }
        userInfo.Id = response.Data["user_id"]?.Value<int>() ?? 0;
        userInfo.NickName = nickname;
        sessionToken = response.Data["session_token"]?.Value<string>() ?? string.Empty;

        IsLoggedIn = true;

        return true;
    }

    public async Task<bool> LogInAsync(int userId, string password)
    {
        if (IsLoggedIn)
        {
            OutputMessage?.Invoke(new(OutputMessageInfo.MessageSenderType.System, "已登录，无法重复登录"));
            return false;
        }

        var response = await chatClientService.CallAPIAsync("login", string.Empty,
                                           new("user_id", userId),
                                                    new("password", password));
        if (response.Success == false)
        {
            OutputMessage?.Invoke(new(OutputMessageInfo.MessageSenderType.System, $"登陆失败: {response.ErrorMessage}"));
            return false;
        }
        userInfo.Id = response.Data["user_id"]?.Value<int>() ?? 0;
        userInfo.NickName = response.Data["nickname"]?.Value<string>() ?? string.Empty;
        sessionToken = response.Data["session_token"]?.Value<string>() ?? string.Empty;

        IsLoggedIn = true;

        return true;
    }

    public async Task LogOutAsync()
    {
        if (!IsLoggedIn)
        {
            OutputMessage?.Invoke(new(OutputMessageInfo.MessageSenderType.System, "未登录，无法登出"));
            return;
        }
        var response = await CallAPIAsync("logout");
        if (response.Success == false)
        {
            OutputMessage?.Invoke(new(OutputMessageInfo.MessageSenderType.System, $"登出失败: {response.ErrorMessage}"));
            return;
        }

        await chatClientService.DisconnectAsync();
        sessionToken = string.Empty;
        JoinedRoomId = null;
        IsLoggedIn = false;
    }

    private async void ReconnectHandler()
    {
        try
        {
            sessionToken = string.Empty;
            JoinedRoomId = null;
            IsLoggedIn = false;
            OutputMessage?.Invoke(new(OutputMessageInfo.MessageSenderType.System, "连接已断开，正在自动重连..."));

            bool connected = await chatClientService.ConnectAsync();
            if (connected)
            {
                chatClientService.StartReceiving();
                OutputMessage?.Invoke(new(OutputMessageInfo.MessageSenderType.System, "已重新连接，请重新登录"));
            }
            else
            {
                OutputMessage?.Invoke(new(OutputMessageInfo.MessageSenderType.System, "自动重连失败，请检查网络后重试"));
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "自动重连失败");
        }
    }

    public async Task<bool> JoinRoomAsync(int roomId)
    {
        var response = await CallAPIAsync("join_room", new APIParameter("room_id", roomId));
        if (response.Success == false)
        {
            OutputMessage?.Invoke(new(OutputMessageInfo.MessageSenderType.System, $"加入房间失败: {response.ErrorMessage}"));
            return false;
        }
        JoinedRoomId = roomId;
        return true;
    }

    public async Task<int?> CreateRoomAsync(string roomName)
    {
        var response = await CallAPIAsync("create_room", new APIParameter("room_name", roomName));
        if (response.Success == false)
        {
            OutputMessage?.Invoke(new(OutputMessageInfo.MessageSenderType.System, $"创建房间失败: {response.ErrorMessage}"));
            return null;
        }

        int roomId = response.Data.Value<int>("room_id");
        JoinedRoomId = roomId; //服务端已自动将创建者加入房间
        OutputMessage?.Invoke(new(OutputMessageInfo.MessageSenderType.System, $"房间「{roomName}」创建成功"));
        return roomId;
    }

    public async Task SendMessageAsync(string message)
    {
        var response = await CallAPIAsync("send_message", new APIParameter("message", message));

        if (response.Success == false)
        {
            logger.Warning($"消息发送失败: {response.ErrorMessage}");
            OutputMessage?.Invoke(new(OutputMessageInfo.MessageSenderType.System, $"消息发送失败: {response.ErrorMessage}"));
            return;
        }

        OutputMessage?.Invoke(new(OutputMessageInfo.MessageSenderType.Self, message));
    }

    public async Task<List<RoomInfo>> GetRoomListAsync()
    {
        var response = await CallAPIAsync("get_room_list");

        if (!response.Success)
        {
            OutputMessage?.Invoke(new(OutputMessageInfo.MessageSenderType.System, $"获取房间列表失败: {response.ErrorMessage}"));
            return new List<RoomInfo>();
        }

        return ParseRoomList(response.Data["room_info_list"]);
    }

    private void OnUpdateEvent(UpdateEvent @event)
    {
        if (!string.Equals(@event.Data.Value<string>("update_type"), "room_list", StringComparison.Ordinal))
        {
            return;
        }

        var roomList = ParseRoomList(@event.Data["update_data"]);
        RoomListUpdated?.Invoke(roomList);
    }

    private void OnNoticeEvent(NoticeEvent @event)
    {
        var noticeType = @event.Data.Value<string>("notice_type");
        var nickname = @event.Data.Value<string>("nickname") ?? string.Empty;

        switch (noticeType)
        {
            case "join_room":
                OutputMessage?.Invoke(new(OutputMessageInfo.MessageSenderType.System, $"{nickname} 加入了房间"));
                break;
            case "leave_room":
                OutputMessage?.Invoke(new(OutputMessageInfo.MessageSenderType.System, $"{nickname} 离开了房间"));
                break;
        }
    }

    private async void OnHeartbeatEvent(HeartbeatEvent @event)
    {
        //仅登录状态下回发心跳，未登录连接由服务端按空闲超时清理
        if (!IsLoggedIn)
        {
            return;
        }

        try
        {
            await CallAPIAsync("heartbeat");
        }
        catch (Exception ex)
        {
            logger.Warning(ex, "心跳回应失败");
        }
    }

    private List<RoomInfo> ParseRoomList(JToken? roomListJson)
    {
        var roomList = new List<RoomInfo>();
        if (roomListJson is not JArray array)
        {
            return roomList;
        }

        foreach (var room in array)
        {
            roomList.Add(new RoomInfo
            {
                RoomId = room["room_id"]?.Value<int>() ?? 0,
                RoomName = room["room_name"]?.Value<string>() ?? string.Empty,
                UserCount = room["user_count"]?.Value<int>() ?? 0
            });
        }

        return roomList;
    }

    public void Dispose()
    {
        chatClientService.UnsubscribeToEvent<MessageEvent>(ChatRoomFunction.OutputMessage);
        chatClientService.UnsubscribeToEvent<UpdateEvent>(OnUpdateEvent);
        chatClientService.UnsubscribeToEvent<NoticeEvent>(OnNoticeEvent);
        chatClientService.UnsubscribeToEvent<HeartbeatEvent>(OnHeartbeatEvent);
        chatClientService.Dispose();
    }

    public int GetUserId()
    {
        return userInfo.Id;
    }

    public string GetNickName()
    {
        return userInfo.NickName;
    }
}

