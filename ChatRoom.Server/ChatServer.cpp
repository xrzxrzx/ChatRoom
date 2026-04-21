#include "ChatServer.h"
#include "UserSession.h"
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
				std::make_shared<UserSession>(std::move(_socket))->Start();
			}
			else
			{
				cout << "错误: " << error.message() << endl;
			}
			StartAccept();
		});
}
