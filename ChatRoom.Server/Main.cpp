#include <iostream>
#include <boost/asio.hpp>
#include<spdlog/spdlog.h>
#include <spdlog/sinks/daily_file_sink.h>
#include <spdlog/sinks/stdout_color_sinks.h>
#include <filesystem>
#include"di.hpp"

#include"ChatServerService.h"
#include"UserSession.h"
#include"UserSessionServiceClient.h"

#ifdef _WIN32
#include <windows.h>
#endif

namespace di = boost::di;

void init_spdlog();

int main()
{
#ifdef _WIN32
	// 切换控制台到 UTF-8 编码，避免中文乱码
	SetConsoleOutputCP(CP_UTF8);
	SetConsoleCP(CP_UTF8);
#endif

	init_spdlog();
	
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

void init_spdlog()
{
	wchar_t buf[MAX_PATH];
	GetModuleFileNameW(NULL, buf, MAX_PATH);
	std::filesystem::path exeDir = std::filesystem::path(buf).remove_filename();
	std::string logDir = (exeDir / "logs").string();
	std::filesystem::create_directories(logDir);

	// 每日午夜(0:0)切分，基文件名为 chatroom.log，历史文件会被重命名为 chatroom.log.YYYY-MM-DD
	auto file_sink = std::make_shared<spdlog::sinks::daily_file_sink_mt>(logDir + "/chatroom.log", 0, 0);
	auto console_sink = std::make_shared<spdlog::sinks::stdout_color_sink_mt>();

	// 设置各自的输出格式（控制台使用带颜色的等级标记）
	file_sink->set_pattern("[%Y-%m-%d %H:%M:%S] [%l] %v");
	console_sink->set_pattern("[%Y-%m-%d %H:%M:%S] [%^%l%$] %v");

	std::vector<spdlog::sink_ptr> sinks{file_sink, console_sink};
	auto logger = std::make_shared<spdlog::logger>("chat", sinks.begin(), sinks.end());
	spdlog::set_default_logger(logger);
	spdlog::flush_on(spdlog::level::info);
}