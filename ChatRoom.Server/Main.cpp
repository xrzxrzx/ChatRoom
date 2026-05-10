#include <iostream>
#include <boost/asio.hpp>
#include<spdlog/spdlog.h>
#include"di.hpp"

#include"ChatServerService.h"

namespace di = boost::di;

int main()
{
	short port = 12345;
	spdlog::info("Starting ChatRoom Server...");

	auto injector = di::make_injector(
		di::bind<ChatServerService>.to([port]() {
			return std::make_shared<ChatServerService>(port);
			})
	);
}
