#pragma once

#include<memory>
#include<queue>
#include<string>
#include<boost/asio.hpp>

#include"APIMessageBag.h"
#include"EventMessageBag.h"

using std::queue;
using std::string;
using boost::asio::ip::tcp;

class ChatRoom;
class ChatServerService;
class IUserSessionServiceClient;

using APIMessageBag::ResquestBag;
using APIMessageBag::ResponseBag;

// 使用IoC
class UserSession : public std::enable_shared_from_this<UserSession>
{
public:
	UserSession(ChatServerService& server, IUserSessionServiceClient& userSessionServiceClient);

	//初始化新连接，需要等待客户端调用 login 接口，获取用户信息后才能加入聊天室，且用户仅能调用一次 login 接口，否则会被服务器断开连接
	void Init(boost::asio::ip::tcp::socket socket);
	void Deliver(const string& message); 

private:
	tcp::socket socket;
	boost::asio::streambuf readBuffer;
	queue<string> writeQueue;

	ChatRoom* currentChatRoom = nullptr;
	ChatServerService& server;
	IUserSessionServiceClient& serviceClient;
	friend class ChatServerService;

	//用户信息
	int userId;
	string nickname;

	void do_write();
	void do_read();

	void HandleLogin(int userId, const string& password, const string& echo);
	void HandleRegister(const string& password, const string& nickname, const string& echo);

	void HandleAPIRequest(const ResquestBag& resquestBag);
	void HandleSendMessage(const ResquestBag& resquestBag);
	void HandleGetRoomList(const ResquestBag& resquestBag);
	void HandleRequest(const ResquestBag& resquestBag);
};

