using ChatRoom.Client.Function.ChatRoom;

namespace ChatRoom.Client.Models;

public class UserInfoModel
{
    public int Id { get; set; }
    public string NickName { get; set; } = string.Empty;

    public UserInfoModel(int id, string nickName)
    {
        Id = id;
        NickName = nickName;
    }
    
    public static UserInfoModel FromUserInfo(UserInfo userInfo)
    {
        return new UserInfoModel(userInfo.Id, userInfo.NickName);
    }
}
