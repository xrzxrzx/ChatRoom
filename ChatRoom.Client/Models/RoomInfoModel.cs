namespace ChatRoom.Client.Models;

public class RoomInfoModel
{
    public int Id { get; set; } = -1;
    public string Name { get; set; } = string.Empty;
    public int UserCount { get; set; } = 0;

    public RoomInfoModel(int id, string name, int userCount)
    {
        Id = id;
        Name = name;
        UserCount = userCount;
    }

    public static RoomInfoModel FromRoomInfo(Function.ChatRoom.RoomInfo roomInfo)
    {
        return new RoomInfoModel(
            roomInfo.Id,
            roomInfo.Name,
            roomInfo.UserCount
        );
    }
}
