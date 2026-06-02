#include <iostream>
#include <boost/asio.hpp>
#include<spdlog/spdlog.h>
#include"di.hpp"

#include"ChatServerService.h"
#include"UserSession.h"
#include"UserSessionServiceClient.h"

namespace di = boost::di;

int main()
{
	short port = 12345;
	spdlog::info("开启服务器...");

	auto injector = di::make_injector(
		di::bind<short>.to(port),
		di::bind<ChatServerService>.in(di::singleton),
		di::bind<IUserSessionServiceClient>.to<UserSessionServiceClient>().in(di::singleton)
	);

	auto chatServerService = injector.create<std::shared_ptr<ChatServerService>>();
	chatServerService->SetSessionFactory([&injector]() {
		return injector.create<std::shared_ptr<UserSession>>();
		});

	chatServerService->StartAccept();
	chatServerService->AddChatRoom("游戏开黑");
	chatServerService->AddChatRoom("技术交流");
	chatServerService->AddChatRoom("日常吹水");
}
