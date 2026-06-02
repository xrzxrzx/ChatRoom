#include "UserSession.h"

#include<spdlog/spdlog.h>
#include<nlohmann/json.hpp>
#include"APIMessageBag.h"
#include"EventMessageBag.h"
#include"ChatServerService.h"
#include"ChatRoom.h"
#include"UserSessionServiceClient.h"

namespace asio = boost::asio;
using APIMessageBag::ResquestBag;
using APIMessageBag::ResponseBag;

UserSession::UserSession(ChatServerService& server, IUserSessionServiceClient& userSessionServiceClient)
	: socket(server.ioContext), server(server), serviceClient(userSessionServiceClient)
{
	userId = 0;
	nickname = "";
}

void UserSession::Init(boost::asio::ip::tcp::socket socket)
{
	this->socket = std::move(socket);

	asio::async_read_until(socket, readBuffer, '\n',
		[this](boost::system::error_code ec, std::size_t bytes_transferred)
		{
			if (!ec)
			{
				std::istream is(&readBuffer);
				string line;
				std::getline(is, line);
				ResquestBag requestBag(line);
				if (requestBag.GetAction() == "login")
				{
					spdlog::info("用户请求登录");
					HandleLogin(requestBag.GetData()["user_id"], requestBag.GetData()["password"], requestBag.GetEcho());
				}
				else if (requestBag.GetAction() == "register")
				{
					spdlog::info("用户请求注册");
					HandleRegister(requestBag.GetData()["password"], requestBag.GetData()["nickname"], requestBag.GetEcho());
				}
				else
				{
					// TODO : 处理其他接口调用，用户仅能调用一次 login 或 register 接口，否则会被服务器断开连接
				}
			}
			else
			{
				spdlog::error("接收消息出错: {}", ec.message());
			}
		});
}

void UserSession::Deliver(const string& message)
{
	bool write_in_progress = !writeQueue.empty();
	writeQueue.push(message);
	if (!write_in_progress) {
		do_write();
	}
}

void UserSession::do_write()
{
	asio::async_write(socket, asio::buffer(writeQueue.front()),
		[this](boost::system::error_code ec, std::size_t /*length*/)
		{
			if (!ec)
			{
				writeQueue.pop();
				if (!writeQueue.empty()) {
					do_write();
				}
			}
			else
			{
				spdlog::error("发送消息出错: {}", ec.message());
			}
		});
}

void UserSession::do_read()
{
	asio::async_read_until(socket, readBuffer, '\n',
		[this](boost::system::error_code ec, std::size_t bytes_transferred)
		{
			if (!ec)
			{
				std::istream is(&readBuffer);
				string line;
				std::getline(is, line);
				ResquestBag requestBag(line);
				// TODO : 处理用户发送的消息
			}
			else
			{
				spdlog::error("接收消息出错: {}", ec.message());
			}
		});
}

void UserSession::HandleLogin(int userId, const string& password, const string& echo)
{
	ResponseBag responseBag(echo);

	auto rpcResponse = serviceClient.Login(userId, password);

	if (rpcResponse.success() == false)
	{
		spdlog::error("用户登录失败，userId: {}", userId);
		responseBag.SetError(1, "登录失败");
		Deliver(responseBag.ToJsonString());

		boost::system::error_code ec;
		socket.shutdown(boost::asio::ip::tcp::socket::shutdown_both, ec);
		socket.close(ec);
		return;
	}

	this->nickname = rpcResponse.nickname();

	responseBag.AddData("session_token", rpcResponse.session_token());
	responseBag.AddData("nickname", this->nickname);
	Deliver(responseBag.ToJsonString());
}

void UserSession::HandleRegister(const string& password, const string& nickname, const string& echo)
{
	ResponseBag responseBag(echo);

	auto rpcResponse = serviceClient.Register(nickname, password);

	if (rpcResponse.success() == false)
	{
		spdlog::error("用户注册失败，nickname: {}", nickname);
		responseBag.SetError(1, "注册失败");
		Deliver(responseBag.ToJsonString());
		boost::system::error_code ec;
		socket.shutdown(boost::asio::ip::tcp::socket::shutdown_both, ec);
		socket.close(ec);
		return;
	}

	this->userId = rpcResponse.user_id();
	this->nickname = nickname;

	responseBag.AddData("user_id", rpcResponse.user_id());
	responseBag.AddData("session_token", rpcResponse.session_token());
	responseBag.AddData("nickname", this->nickname);
	Deliver(responseBag.ToJsonString());
}
