#pragma once

#include <boost/asio.hpp>
#include <queue>
#include<nlohmann/json.hpp>
#include<functional>

namespace asio = boost::asio;
using tcp = asio::ip::tcp;
using json = nlohmann::json;

class UserSession : public std::enable_shared_from_this<UserSession>
{
public:
	UserSession(tcp::socket socket) : _socket(std::move(socket)) {}
	tcp::socket& Socket() { return _socket; }
	void Start();
	void SendMessage(const std::string& message);

	void SetMessageCommandHandle(std::function<void(const std::string& echo, const json& params)> handle);

	void SetRequestCommandHandle(std::function<void(const std::string& echo, const json& params)> handle);

private:
	tcp::socket _socket;
	asio::streambuf _buffer;
	std::queue<std::string> _messageQueue;

	std::function<void(const std::string& echo, const json& params)> _messageCommandHandle;
	std::function<void(const std::string& echo, const json& params)> _requestCommandHandle;

	void do_recvive();
	void do_send(const std::string& message);

	void OnMessageReceived(const std::string& message);
};