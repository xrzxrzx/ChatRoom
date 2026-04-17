#pragma once

#include <boost/asio.hpp>
#include <queue>

namespace asio = boost::asio;
using tcp = asio::ip::tcp;

class UserSession : public std::enable_shared_from_this<UserSession>
{
public:
	UserSession(tcp::socket socket) : _socket(std::move(socket)) {}
	tcp::socket& Socket() { return _socket; }
	void Start();
	void SendMessage(const std::string& message);

private:
	tcp::socket _socket;
	asio::streambuf _buffer;
	std::queue<std::string> _messageQueue;

	void do_recvive();
	void do_send(const std::string& message);

	void OnMessageReceived(const std::string& message);
};