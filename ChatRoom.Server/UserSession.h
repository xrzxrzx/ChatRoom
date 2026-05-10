#pragma once

#include<memory>
#include<queue>
#include<boost/asio.hpp>
#include"ChatServerService.h"
#include"ChatRoom.h"

using std::queue;
using boost::asio::ip::tcp;

class UserSession : public std::enable_shared_from_this<UserSession>
{
public:
	UserSession(boost::asio::ip::tcp::socket socket, ChatServerService& server) : socket(std::move(socket)), server(server) {}

	/**
	* 初始化新连接，需要等待客户端调用 login 接口，获取用户信息后才能加入聊天室，且用户仅能调用一次 login 接口，否则会被服务器断开连接
	*/
	void Init();
	void Deliver(const string& message);

private:
	tcp::socket socket;
	boost::asio::streambuf readBuffer;
	queue<string> writeQueue;

	ChatRoom* currentChatRoom = nullptr;
	ChatServerService& server;
	friend class ChatServerService;

	void do_write();
	void do_read();
};

