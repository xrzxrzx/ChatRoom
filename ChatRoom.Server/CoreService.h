#pragma once
#include <boost/asio.hpp>
#include<nlohmann/json.hpp>
#include<string>

#include"ChatServer.h"

using std::string;
using json = nlohmann::json;

class CoreService
{
private:
	asio::io_context ioContext;
	ChatServer _chatServer;

public:
	CoreService(const tcp::endpoint& endpoint);
	void StartCoreService();

	static json& HandleMessageCommand(const string& echo, const json& params);
	static json& HandleRequestCommand(const string& echo, const json& params);
};

