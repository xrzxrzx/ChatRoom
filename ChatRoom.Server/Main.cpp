#include <iostream>
#include <boost/asio.hpp>
#include<spdlog/spdlog.h>
#include <spdlog/sinks/daily_file_sink.h>
#include <spdlog/sinks/stdout_color_sinks.h>
#include <filesystem>
#include"di.hpp"

#include"ChatServerService.h"
#include"ServerConfig.h"
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
	
	auto config = ServerConfig::Load();
	spdlog::info("开启服务器，端口: {}", config.port);

	auto injector = di::make_injector(
		di::bind<short>.to(config.port),
		di::bind<std::string>.to(config.grpcAddress),
		di::bind<ServerConfig>.to(config),
		di::bind<ChatServerService>.in(di::singleton),
		di::bind<IUserSessionServiceClient>.to<UserSessionServiceClient>().in(di::singleton)
	);

	auto chatServerService = injector.create<std::shared_ptr<ChatServerService>>();
	chatServerService->SetSessionFactory([&injector]() {
		//注意：不能使用 injector.create<std::shared_ptr<UserSession>>()，
		//boost.di 对 shared_ptr 的 deduced 作用域会将其缓存为注入器级单例，导致所有连接复用同一会话对象。
		//这里显式解析依赖并用 make_shared 创建全新会话。
		auto& server = injector.create<ChatServerService&>();
		auto& serviceClient = injector.create<IUserSessionServiceClient&>();
		return std::make_shared<UserSession>(server, serviceClient);
		});

	chatServerService->StartAccept();
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
