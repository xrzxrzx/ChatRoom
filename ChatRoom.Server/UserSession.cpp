#include "UserSession.h"

#include<spdlog/spdlog.h>
#include<nlohmann/json.hpp>
#include"APIMessageBag.h"
#include"EventMessageBag.h"
#include"ChatServerService.h"
#include"ChatRoom.h"
#include"UserSessionServiceClient.h"

namespace asio = boost::asio;
using APIMessageBag::RequestBag;
using APIMessageBag::ResponseBag;

UserSession::UserSession(ChatServerService& server, IUserSessionServiceClient& userSessionServiceClient)
	: socket(server.ioContext), server(server), gRPCServiceClient(userSessionServiceClient)
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
				RequestBag requestBag(line);
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
				RequestBag requestBag(line);
				HandleAPIRequest(requestBag);
			}
			else
			{
				spdlog::error("接收消息出错: {}", ec.message());
			}
		});
}

//仅在用户登录时调用，且用户仅能调用一次 login 接口，否则会被服务器断开连接
void UserSession::HandleLogin(int userId, const string& password, const string& echo)
{
	ResponseBag responseBag(echo);

	auto rpcResponse = gRPCServiceClient.Login(userId, password);

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

//仅在用户注册时调用，且用户仅能调用一次 register 接口，否则会被服务器断开连接
void UserSession::HandleRegister(const string& password, const string& nickname, const string& echo)
{
	ResponseBag responseBag(echo);

	auto rpcResponse = gRPCServiceClient.Register(nickname, password);

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

//不允许用户在调用 login 或 register 接口前调用
void UserSession::HandleAPIRequest(const RequestBag& requestBag)
{
	ResponseBag responseBag(requestBag.GetEcho());

	if (ValidateToken(requestBag.GetToken(), responseBag))
		return;

	auto action = requestBag.GetAction();
	if (action == "send_message")
	{
		HandleSendMessage(requestBag);
	}
	else if (action == "get_room_list")
	{
		HandleGetRoomList(requestBag);
	}
	else if (action == "join_room")
	{
		HandleJoinRoom(requestBag);
	}
	else if(action == "request")
	{
		HandleRequest(requestBag);
	}
}

void UserSession::HandleSendMessage(const RequestBag& requestBag)
{
	ResponseBag responseBag(requestBag.GetEcho());

	if (ValidateToken(requestBag.GetToken(), responseBag))
		return;

	if (currentChatRoom == nullptr)
	{
		responseBag.SetError(403, "用户未加入聊天室");
		Deliver(responseBag.ToJsonString());
		return;
	}

	auto& message = requestBag.GetData()["message"];
	auto& sender = requestBag.GetData()["sender"];
	responseBag.AddData("message", message);
	responseBag.AddData("sender", sender);

	currentChatRoom->Broadcast(responseBag.ToJsonString());
}

void UserSession::HandleGetRoomList(const RequestBag & requestBag)
{
	ResponseBag responseBag(requestBag.GetEcho());

	if (ValidateToken(requestBag.GetToken(), responseBag))
		return;

	json roomList = json::array();
	for (auto& [roomId, room] : server.chatRoomMap)
	{
		roomList.push_back({ {"room_id", roomId}, {"room_name", room.GetName()} });
	}

	responseBag.AddData("room_info_list", roomList);
	Deliver(responseBag.ToJsonString());
}

void UserSession::HandleJoinRoom(const RequestBag& requestBag)
{
	ResponseBag responseBag(requestBag.GetEcho());

	if (ValidateToken(requestBag.GetToken(), responseBag))
		return;

	//TODO : 处理用户加入聊天室的请求
}

void UserSession::HandleRequest(const RequestBag & requestBag)
{
	// TODO 处理请求API
}

bool UserSession::ValidateToken(const string& token, ResponseBag& responseBag)
{
	auto rpcResponse = gRPCServiceClient.ValidateSession(userId, token);
	if (!rpcResponse.is_valid())
	{
		responseBag.SetError(502, "令牌不合法");
		Deliver(responseBag.ToJsonString());
		return false;
	}
	return true;
}
