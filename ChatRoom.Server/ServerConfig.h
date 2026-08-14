#pragma once

#include<string>
#include<vector>

//服务端配置：全部字段带默认值，可通过 server.json 覆盖
struct ServerConfig
{
	short port = 12345;
	//使用 127.0.0.1 而非 localhost，避免 gRPC 首次连接时的 IPv4/IPv6 解析回退导致 10s+ 延迟
	std::string grpcAddress = "127.0.0.1:50051";
	int heartbeatIntervalSec = 30;
	int idleTimeoutSec = 90;
	int maxMessageLength = 4096;
	std::vector<std::string> rooms = { "通用", "游戏开黑", "技术交流" };

	//依次尝试当前工作目录与可执行文件目录下的 configFile
	static ServerConfig Load(const std::string& configFile = "server.json");
};
