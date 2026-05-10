#pragma once

#include<vector>
#include<string>
#include<map>
#include<boost/asio.hpp>
#include"ChatRoom.h"

using std::vector;
using std::map;
using boost::asio::ip::tcp;

class ChatServerService
{
public:
	ChatServerService(short port);

	void StartAccept();
	void AddChatRoom(const string& name);

private:
	tcp::acceptor acceptor;
	tcp::socket socket;
	boost::asio::io_context ioContext;

	map<int, ChatRoom> chatRoomMap;

	void do_accept();

	vector<string> GetChatRoomNames() const;
	vector<int> GetChatRoomIds() const;
	string GetChatRoomNameById(int id) const;
	bool JoinChatRoom(int roomId, std::shared_ptr<UserSession> participant);

	friend class UserSession;
};

