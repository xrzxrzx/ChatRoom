#pragma once

#include<memory>
#include<queue>
#include<string>
#include<unordered_map>
#include<chrono>
#include<functional>
#include<boost/asio.hpp>

#include"APIMessageBag.h"
#include"EventMessageBag.h"

using std::queue;
using std::string;
using boost::asio::ip::tcp;
using json = nlohmann::json;

class ChatRoom;
class ChatServerService;
class IUserSessionServiceClient;

using APIMessageBag::RequestBag;
using APIMessageBag::ResponseBag;

// 使用IoC
class UserSession : public std::enable_shared_from_this<UserSession>
{
public:
	UserSession(ChatServerService& server, IUserSessionServiceClient& userSessionServiceClient);

	//初始化新连接，需要等待客户端调用 login 接口，获取用户信息后才能加入聊天室，且用户仅能调用一次 login 接口，否则会被服务器断开连接
	void Init(boost::asio::ip::tcp::socket socket);
	void DeliverResponse(const ResponseBag& responseBag) { Deliver(responseBag.ToJsonString()); }
	void DeliverEvent(const EventMessageBag& eventMessageBag) { Deliver(eventMessageBag.ToJsonString()); }

	//关闭连接（清理房间、广播通知、从会话表移除）
	void Close();
	//等待待发送数据全部写出后再关闭连接
	void CloseAfterFlush();

	std::chrono::steady_clock::time_point GetLastActivity() const { return lastActivity; }
	int GetUserId() const { return userId; }

private:
	tcp::socket socket;
	boost::asio::streambuf readBuffer;
	queue<string> writeQueue;

	ChatRoom* currentChatRoom = nullptr;
	ChatServerService& server;
	IUserSessionServiceClient& gRPCServiceClient;
	friend class ChatServerService;

	//API请求处理
	using APIHandler = std::function<void(const RequestBag&)>;
	std::unordered_map<string, APIHandler> apiHandlers;

	//用户信息
	int userId;
	string nickname;

	bool closed = false;
	bool closeAfterFlush = false;
	std::chrono::steady_clock::time_point lastActivity;

	void do_write();
	void do_read();

	//不要调用这个函数直接发送消息，应该调用 DeliverResponse 或 DeliverEvent 来发送响应或事件消息
	void Deliver(const string& message); 
	void UpdateActivity() { lastActivity = std::chrono::steady_clock::now(); }

	void LeaveCurrentRoom();

	void HandleLogin(int userId, const string& password, const string& echo);
	void HandleRegister(const string& password, const string& nickname, const string& echo);
	void HandleUnauthorized(const string& echo);

	void HandleAPIRequest(const RequestBag& requestBag);
	void HandleSendMessage(const RequestBag& requestBag);
	void HandleGetRoomList(const RequestBag& requestBag);
	void HandleJoinRoom(const RequestBag& requestBag);
	void HandleCreateRoom(const RequestBag& requestBag);
	void HandleLogout(const RequestBag& requestBag);
	void HandleRequest(const RequestBag& requestBag);

	bool ValidateToken(const string& token, ResponseBag& responseBag);
};

