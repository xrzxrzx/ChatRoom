#pragma once

#include<vector>
#include<string>
#include<map>
#include<memory>
#include<functional>
#include<boost/asio.hpp>
#include<boost/asio/steady_timer.hpp>
#include"ChatRoom.h"
#include"ServerConfig.h"

using std::vector;
using std::map;
using std::string;
using boost::asio::ip::tcp;

class ChatRoom;
class EventMessageBag;
class UserSession;

// 使用IoC
class ChatServerService : public std::enable_shared_from_this<ChatServerService>
{
public:
	ChatServerService(short port, ServerConfig config);

	void StartAccept();
	int AddChatRoom(const string& name, bool isSystem = true);
	int CreateChatRoom(const string& name);
	void SetSessionFactory(std::function<std::shared_ptr<UserSession>()> factory);

	const ServerConfig& GetConfig() const { return config; }

	void RemoveChatRoom(int roomId);
	void BroadcastRoomListUpdate();
	void RequestRemoveSession(const std::shared_ptr<UserSession>& session);

private:
	boost::asio::io_context ioContext;
	tcp::acceptor acceptor;
	tcp::socket socket;
	std::function<std::shared_ptr<UserSession>()> sessionFactory;

	map<int, ChatRoom> chatRoomMap;
	std::vector<std::shared_ptr<UserSession>> sessions;
	boost::asio::steady_timer heartbeatTimer;
	int nextRoomId = 0;
	ServerConfig config;

	void do_accept();
	void StartHeartbeat();
	void BroadcastHeartbeat();
	void CheckIdleSessions();
	void RemoveSession(const std::shared_ptr<UserSession>& session);

	vector<string> GetChatRoomNames() const;
	vector<int> GetChatRoomIds() const;
	string GetChatRoomNameById(int id) const;
	bool JoinChatRoom(int roomId, std::shared_ptr<UserSession> participant);

	friend class UserSession;
};

