#pragma once

#include <boost/asio.hpp>
#include <queue>
#include<nlohmann/json.hpp>
#include<functional>
#include"ServerMessageBag.h"
#include"ClientMessageBag.h"

namespace asio = boost::asio;
using tcp = asio::ip::tcp;
using json = nlohmann::json;

using ClientMessage::RequestBag;
using ClientMessage::ResponseBag;
using ServerMessage::ServerMessageBag;

class UserSession : public std::enable_shared_from_this<UserSession>
{
public:
	UserSession(tcp::socket socket) : _socket(std::move(socket)) {}
	tcp::socket& Socket() { return _socket; }
	void Start();
	void SendMessage(const std::string& message);

	void SetMessageCommandHandle(std::function<json&(const std::string& echo, const json& params)> handle);

	void SetRequestCommandHandle(std::function<json&(const std::string& echo, const json& params)> handle);

private:
	tcp::socket _socket;
	asio::streambuf _buffer;
	std::queue<std::string> _messageQueue;

	std::function<json&(const std::string& echo, const json& params)> _messageCommandHandle;
	std::function<json&(const std::string& echo, const json& params)> _requestCommandHandle;

	void do_recvive();
	void do_send(const std::string& message);

	void OnMessageReceived(const std::string& message);
	json& SwitchCommand(const ClientMessage::RequestBag& requestBag);
};