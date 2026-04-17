#include <iostream>
#include <boost/asio.hpp>
#include "ChatServer.h"

int main()
{
	asio::io_context ioContext;
	ChatServer server(ioContext, tcp::endpoint(tcp::v4(), 12345));
	ioContext.run();
}
