#pragma once

#include <boost/asio.hpp>
#include<vector>

#include "UserSession.h"

namespace asio = boost::asio;
using tcp = asio::ip::tcp;
using json = nlohmann::json;

using std::vector;

class ChatServer
{
	private:
		tcp::acceptor _acceptor;
		tcp::socket _socket;

		std::function<json& (const std::string& echo, const json& params)> _messageCommandHandle;
		std::function<json& (const std::string& echo, const json& params)> _requestCommandHandle;

		vector<std::shared_ptr<UserSession>> _sessions;

	public:
		ChatServer(asio::io_context& ioContext, const tcp::endpoint& endpoint);
		void StartAccept();//开始接受连接
		void BroadcastMessage(const std::string& message);//广播消息给所有用户

		void SetMessageCommandHandle(std::function<json& (const std::string& echo, const json& params)> handle);
		void SetRequestCommandHandle(std::function<json& (const std::string& echo, const json& params)> handle);
};

