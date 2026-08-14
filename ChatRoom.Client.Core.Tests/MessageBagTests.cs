using ChatRoom.Client.Core.Network.MessageBag;
using ChatRoom.Client.Core.Network.MessageBag.APIMessageBag;
using Newtonsoft.Json.Linq;
using Xunit;

namespace ChatRoom.Client.Core.Tests;

public class MessageBagTests
{
    [Fact]
    public void RequestMessageBag_SerializesTokenAndParams()
    {
        var request = new RequestMessageBag("get_room_list")
            .SetEcho("echo-1")
            .SetToken("token-abc")
            .AddParameter("room_id", 3);

        var json = JObject.Parse(request.ToJsonString());

        Assert.Equal("get_room_list", json.Value<string>("action"));
        Assert.Equal("echo-1", json.Value<string>("echo"));
        Assert.Equal("token-abc", json.Value<string>("token"));
        Assert.Equal(3, json["params"]?.Value<int>("room_id"));
    }

    [Fact]
    public void ResponseMessageBag_ParsesSuccessResponse()
    {
        var json = JObject.Parse(
            """{"recode":0,"message":"","echo":"e1","data":{"user_id":7,"session_token":"tok"}}""");
        var response = new ResponseMessageBag(json);

        Assert.True(response.Success);
        Assert.Equal("e1", response.Echo);
        Assert.Equal(7, response.Data.Value<int>("user_id"));
        Assert.Equal("tok", response.Data.Value<string>("session_token"));
    }

    [Fact]
    public void ResponseMessageBag_ParsesErrorResponse()
    {
        var json = JObject.Parse(
            """{"recode":502,"message":"令牌不合法","echo":"e2","data":{}}""");
        var response = new ResponseMessageBag(json);

        Assert.False(response.Success);
        Assert.Equal(502, response.Recode);
        Assert.Equal("令牌不合法", response.ErrorMessage);
    }

    [Fact]
    public void MessageBagAnalysis_DetectsEventAndResponse()
    {
        var eventAnalysis = new MessageBagAnalysis(
            """{"post_type":"message","data":{"sender":1,"nickname":"a","message":"hi"}}""");
        Assert.True(eventAnalysis.IsEvent);
        Assert.False(eventAnalysis.IsResponse);

        var responseAnalysis = new MessageBagAnalysis("""{"recode":0,"echo":"e"}""");
        Assert.True(responseAnalysis.IsResponse);
        Assert.False(responseAnalysis.IsEvent);
    }
}

