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
		DeliverResponse(responseBag);

		boost::system::error_code ec;
		socket.shutdown(boost::asio::ip::tcp::socket::shutdown_both, ec);
		socket.close(ec);
		return;
	}

	this->nickname = rpcResponse.nickname();

	responseBag.AddData("session_token", rpcResponse.session_token());
	responseBag.AddData("nickname", this->nickname);
	DeliverResponse(responseBag);
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
		DeliverResponse(responseBag);
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
	DeliverResponse(responseBag);
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
	responseBag.AddData("message", message);
	DeliverResponse(responseBag);

	EventMessageBag eventMessageBag("message");
	eventMessageBag.AddData("message", message);
	eventMessageBag.AddData("sender", userId);
	eventMessageBag.AddData("nickname", nickname);
	currentChatRoom->Broadcast(eventMessageBag);
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
	DeliverResponse(responseBag);
}

void UserSession::HandleJoinRoom(const RequestBag& requestBag)
{
	ResponseBag responseBag(requestBag.GetEcho());

	if (ValidateToken(requestBag.GetToken(), responseBag))
		return;

	auto& roomId = requestBag.GetData()["room_id"];
	bool success = server.JoinChatRoom(roomId, shared_from_this());
	if (!success)
	{
		responseBag.SetError(404, "聊天室不存在");
		responseBag.AddData("success", false);
		DeliverResponse(responseBag);
		return;
	}

	responseBag.AddData("success", true);
	DeliverResponse(responseBag);

	//广播用户加入聊天室事件
	EventMessageBag eventMessageBag("notice");
	eventMessageBag.AddData("notice_type", "join_room");
	eventMessageBag.AddData("user_id", userId);
	currentChatRoom->Broadcast(eventMessageBag);
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
		DeliverResponse(responseBag);
		return false;
	}
	return true;
}
