#pragma once

#include<vector>
#include<string>
#include<map>
#include<memory>
#include<functional>
#include<boost/asio.hpp>
#include"ChatRoom.h"

using std::vector;
using std::map;
using std::string;
using boost::asio::ip::tcp;

class ChatRoom;

// 使用IoC
class ChatServerService : public std::enable_shared_from_this<ChatServerService>
{
public:
	ChatServerService(short port);

	void StartAccept();
	void AddChatRoom(const string& name);
	void SetSessionFactory(std::function<std::shared_ptr<UserSession>()> factory);

private:
	tcp::acceptor acceptor;
	tcp::socket socket;
	boost::asio::io_context ioContext;
	std::function<std::shared_ptr<UserSession>()> sessionFactory;

	map<int, ChatRoom> chatRoomMap;

	void do_accept();

	vector<string> GetChatRoomNames() const;
	vector<int> GetChatRoomIds() const;
	string GetChatRoomNameById(int id) const;
	bool JoinChatRoom(int roomId, std::shared_ptr<UserSession> participant);

	friend class UserSession;
};

