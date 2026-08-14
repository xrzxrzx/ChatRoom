#include "ServerConfig.h"

#include<nlohmann/json.hpp>
#include<spdlog/spdlog.h>

#include<filesystem>
#include<fstream>

#ifdef _WIN32
#include<windows.h>
#endif

ServerConfig ServerConfig::Load(const std::string& configFile)
{
	ServerConfig config;

	std::vector<std::filesystem::path> candidates;
	candidates.push_back(std::filesystem::current_path() / configFile);

#ifdef _WIN32
	wchar_t buf[MAX_PATH];
	GetModuleFileNameW(NULL, buf, MAX_PATH);
	candidates.push_back(std::filesystem::path(buf).remove_filename() / configFile);
#endif

	for (const auto& path : candidates)
	{
		std::ifstream in(path);
		if (!in.is_open())
		{
			continue;
		}

		try
		{
			nlohmann::json j;
			in >> j;

			if (j.contains("port")) config.port = j.value("port", config.port);
			if (j.contains("grpc_address")) config.grpcAddress = j.value("grpc_address", config.grpcAddress);
			if (j.contains("heartbeat_interval_sec")) config.heartbeatIntervalSec = j.value("heartbeat_interval_sec", config.heartbeatIntervalSec);
			if (j.contains("idle_timeout_sec")) config.idleTimeoutSec = j.value("idle_timeout_sec", config.idleTimeoutSec);
			if (j.contains("max_message_length")) config.maxMessageLength = j.value("max_message_length", config.maxMessageLength);
			if (j.contains("rooms")) config.rooms = j.value("rooms", config.rooms);

			spdlog::info("已加载配置文件: {}", path.string());
			return config;
		}
		catch (const std::exception& e)
		{
			spdlog::warn("解析配置文件失败({}), 使用默认配置: {}", path.string(), e.what());
			return config;
		}
	}

	spdlog::warn("未找到配置文件 {}, 使用默认配置", configFile);
	return config;
}

