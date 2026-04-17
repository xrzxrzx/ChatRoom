#pragma once

#include "UserSession.h"

class ChatServer
{
	private:
		tcp::acceptor _acceptor;
		tcp::socket _socket;

	public:
		ChatServer(asio::io_context& ioContext, const tcp::endpoint& endpoint);
		void StartAccept();
};

