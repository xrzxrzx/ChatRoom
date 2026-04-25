#include "ChatServer.h"
#include <iostream>

using std::cout;
using std::endl;

ChatServer::ChatServer(asio::io_context& ioContext, const tcp::endpoint& endpoint)
	: _acceptor(ioContext, endpoint), _socket(ioContext)
{
	StartAccept();
}

void ChatServer::StartAccept()
{
	_acceptor.async_accept(_socket,
		[this](const boost::system::error_code& error)
		{
			if (!error)
			{
				auto session = std::make_shared<UserSession>(std::move(_socket));
				_sessions.push_back(session);
				session->SetMessageCommandHandle(_messageCommandHandle);
				session->SetRequestCommandHandle(_requestCommandHandle);
				session->Start();
			}
			else
			{
				cout << "错误: " << error.message() << endl;
			}
			StartAccept();
		});
}

void ChatServer::BroadcastMessage(const std::string& message)
{
	for (auto& session : _sessions)
	{
		session->SendMessage(message);
	}
}

void ChatServer::SetMessageCommandHandle(std::function<json& (const std::string& echo, const json& params)> handle)
{
	_messageCommandHandle = handle;
}

void ChatServer::SetRequestCommandHandle(std::function<json& (const std::string& echo, const json& params)> handle)
{
	_requestCommandHandle = handle;
}
