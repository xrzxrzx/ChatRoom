using ChatRoom.Client.Core.Network;
using ChatRoom.Client.Core.Network.MessageBag.APIMessageBag;
using Newtonsoft.Json.Linq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Formats.Asn1;
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

    public event IChatRoomService.OutputMessageDelegate? OutputMessage;
    public event IChatRoomService.OnLoginStatusChangedDelegate? OnLoginStatusChanged;

    public ChatRoomService(IChatClientService chatClientService, ILogger logger)
    {
        this.chatClientService = chatClientService;
        this.logger = logger;
        userInfo = new UserInfo();

        ChatRoomFunction.SetLogger(logger);
        ChatRoomFunction.SetOutputMessageDelegate(message => OutputMessage?.Invoke(message));

        this.chatClientService.SubscribeToEvent<MessageEvent>(ChatRoomFunction.OutputMessage);
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
        IsLoggedIn = false;
    }

    private void ReconnectHandler()
    {
        sessionToken = string.Empty;
        IsLoggedIn = false;
    }

    public async Task<bool> JoinRoomAsync(int roomId)
    {
        var response = await chatClientService.CallAPIAsync("join_room", string.Empty, new APIParameter("room_id", roomId));
        if (response.Success == false)
        {
            OutputMessage?.Invoke(new(OutputMessageInfo.MessageSenderType.System, $"加入房间失败: {response.ErrorMessage}"));
            return false;
        }
        return true;
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

    public void Dispose()
    {
        throw new NotImplementedException();
    }

    public int GetUserId()
    {
        return userInfo.Id;
    }

    public string GetNickName()
    {
        return userInfo.NickName;
    }

    public async Task<List<RoomInfo>> GetRoomListAsync()
    {
        var response = await chatClientService.CallAPIAsync("get_room_list", string.Empty);

        if (!response.Success)
        {
            OutputMessage?.Invoke(new(OutputMessageInfo.MessageSenderType.System, $"获取房间列表失败: {response.ErrorMessage}"));
            return new List<RoomInfo>();
        }

        List<RoomInfo> roomList = new List<RoomInfo>();
        var roomListJson = response.Data["room_info_list"];

        if (roomListJson != null)
        {
            foreach (var room in roomListJson)
            {
                RoomInfo roomInfo = new RoomInfo
                {
                    RoomId = room["room_id"]?.Value<int>() ?? 0,
                    RoomName = room["room_name"]?.Value<string>() ?? string.Empty,
                    UserCount = room["user_count"]?.Value<int>() ?? 0
                };
                roomList.Add(roomInfo);
            }
        }

        return roomList;
    }
}
