#include "UserSession.h"
#include"StringTool.hpp"

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
using json = nlohmann::json;

UserSession::UserSession(ChatServerService& server, IUserSessionServiceClient& userSessionServiceClient)
	: socket(server.ioContext), server(server), gRPCServiceClient(userSessionServiceClient)
{
	userId = 0;
	nickname = "";
	lastActivity = std::chrono::steady_clock::now();

	apiHandlers = {
		{"send_message", [this](const RequestBag& requestBag) { HandleSendMessage(requestBag); }},
		{"get_room_list", [this](const RequestBag& requestBag) { HandleGetRoomList(requestBag); }},
		{"join_room",     [this](const RequestBag& requestBag) { HandleJoinRoom(requestBag); }},
		{"create_room",   [this](const RequestBag& requestBag) { HandleCreateRoom(requestBag); }},
		{"logout",        [this](const RequestBag& requestBag) { HandleLogout(requestBag); }},
		{"request",       [this](const RequestBag& requestBag) { HandleRequest(requestBag); }},
	};
}

void UserSession::Init(boost::asio::ip::tcp::socket socket)
{
	this->socket = std::move(socket);

	asio::async_read_until(this->socket, readBuffer, '\n',
		[this](boost::system::error_code ec, std::size_t /*bytes_transferred*/)
		{
			if (ec)
			{
				spdlog::error("接收消息出错: {}", ToUtf8(ec.message()));
				Close();
				return;
			}

			UpdateActivity();
			spdlog::info("接收到新用户连接 {}", this->socket.remote_endpoint().address().to_string());

			std::istream is(&readBuffer);
			string line;
			std::getline(is, line);
			if (line.size() > server.GetConfig().maxMessageLength)
			{
				spdlog::error("首条消息超长，断开连接");
				ResponseBag responseBag("");
				responseBag.SetError(400, "消息过长");
				DeliverResponse(responseBag);
				CloseAfterFlush();
				return;
			}

			try
			{
				RequestBag requestBag(line);
				if (requestBag.GetAction() == "login")
				{
					spdlog::info("用户请求登录");
					HandleLogin(requestBag.GetParams()["user_id"], requestBag.GetParams()["password"], requestBag.GetEcho());
				}
				else if (requestBag.GetAction() == "register")
				{
					spdlog::info("用户请求注册");
					HandleRegister(requestBag.GetParams()["password"], requestBag.GetParams()["nickname"], requestBag.GetEcho());
				}
				else
				{
					spdlog::error("用户未登录 {}", this->socket.remote_endpoint().address().to_string());
					HandleUnauthorized(requestBag.GetEcho());
				}
			}
			catch (const json::exception& e)
			{
				spdlog::error("JSON 解析失败: {}", e.what());
				ResponseBag responseBag("");
				responseBag.SetError(400, "请求格式错误");
				DeliverResponse(responseBag);
				CloseAfterFlush();
			}
		});
}

void UserSession::Deliver(const string& message)
{
	if (closed)
	{
		return;
	}

	bool write_in_progress = !writeQueue.empty();
	writeQueue.push(message + "\n");
	if (!write_in_progress) {
		do_write();
	}
}

void UserSession::do_write()
{
	asio::async_write(socket, asio::buffer(writeQueue.front()),
		[this](boost::system::error_code ec, std::size_t /*length*/)
		{
			if (ec)
			{
				spdlog::error("发送消息出错: {}", ToUtf8(ec.message()));
				Close();
				return;
			}

			writeQueue.pop();
			if (!writeQueue.empty()) {
				do_write();
			}
			else if (closeAfterFlush)
			{
				Close();
			}
		});
}

void UserSession::do_read()
{
	asio::async_read_until(socket, readBuffer, '\n',
		[this](boost::system::error_code ec, std::size_t /*bytes_transferred*/)
		{
			if (ec)
			{
				spdlog::error("接收消息出错: {}", ToUtf8(ec.message()));
				Close();
				return;
			}

			UpdateActivity();

			std::istream is(&readBuffer);
			string line;
			std::getline(is, line);
			if (line.size() > server.GetConfig().maxMessageLength)
			{
				spdlog::error("消息超长，断开连接");
				ResponseBag responseBag("");
				responseBag.SetError(400, "消息过长");
				DeliverResponse(responseBag);
				CloseAfterFlush();
				return;
			}

			try
			{
				RequestBag requestBag(line);
				HandleAPIRequest(requestBag);
			}
			catch (const json::exception& e)
			{
				spdlog::error("JSON 解析失败: {}", e.what());
				ResponseBag responseBag("");
				responseBag.SetError(400, "请求格式错误");
				DeliverResponse(responseBag);
				CloseAfterFlush();
				return;
			}

			if (!closed)
			{
				//继续监听客户端请求
				do_read();
			}
		});
}

//仅在用户登录时调用，且用户仅能调用一次 login 接口，否则会被服务器断开连接
void UserSession::HandleLogin(int userId, const string& password, const string& echo)
{
	ResponseBag responseBag(echo);

	auto rpcStart = std::chrono::steady_clock::now();
	auto rpcResponse = gRPCServiceClient.Login(userId, password);
	spdlog::info("Login RPC 耗时: {} ms", std::chrono::duration_cast<std::chrono::milliseconds>(std::chrono::steady_clock::now() - rpcStart).count());

	if (rpcResponse.success() == false)
	{
		spdlog::error("用户登录失败，userId: {}", userId);
		responseBag.SetError(1, "登录失败，用户ID或密码错误");
		DeliverResponse(responseBag);
		CloseAfterFlush();
		return;
	}

	this->userId = rpcResponse.user_id();
	this->nickname = rpcResponse.nickname();
	UpdateActivity();

	//继续监听客户端请求
	do_read();

	responseBag.AddData("user_id", this->userId);
	responseBag.AddData("session_token", rpcResponse.session_token());
	responseBag.AddData("nickname", this->nickname);
	DeliverResponse(responseBag);
}

//仅在用户注册时调用，且用户仅能调用一次 register 接口，否则会被服务器断开连接
void UserSession::HandleRegister(const string& password, const string& nickname, const string& echo)
{
	ResponseBag responseBag(echo);

	auto rpcStart = std::chrono::steady_clock::now();
	auto rpcResponse = gRPCServiceClient.Register(nickname, password);
	spdlog::info("Register RPC 耗时: {} ms", std::chrono::duration_cast<std::chrono::milliseconds>(std::chrono::steady_clock::now() - rpcStart).count());

	if (rpcResponse.success() == false)
	{
		spdlog::error("用户注册失败，nickname: {}", nickname);
		responseBag.SetError(1, "注册失败，昵称已被占用");
		DeliverResponse(responseBag);
		CloseAfterFlush();
		return;
	}

	this->userId = rpcResponse.user_id();
	this->nickname = nickname;
	UpdateActivity();

	//继续监听客户端请求
	do_read();

	responseBag.AddData("user_id", rpcResponse.user_id());
	responseBag.AddData("session_token", rpcResponse.session_token());
	responseBag.AddData("nickname", this->nickname);
	DeliverResponse(responseBag);
}

void UserSession::HandleUnauthorized(const string& echo)
{
	ResponseBag responseBag(echo);
	responseBag.SetError(401, "用户未登录");
	DeliverResponse(responseBag);
	CloseAfterFlush();
}

//不允许用户在调用 login 或 register 接口前调用
void UserSession::HandleAPIRequest(const RequestBag& requestBag)
{
	ResponseBag responseBag(requestBag.GetEcho());

	if (!ValidateToken(requestBag.GetToken(), responseBag))
		return;

	auto it = apiHandlers.find(requestBag.GetAction());
	if (it != apiHandlers.end()) {
		spdlog::info("处理 API 操作: {}", requestBag.GetAction());
		it->second(requestBag);
	}
	else {
		// 处理未知 API 操作
		spdlog::error("未知的 API 操作: {}", requestBag.GetAction());
		responseBag.SetError(404, "未知的 API 操作");
		Deliver(responseBag.ToJsonString());
	}
}

void UserSession::HandleSendMessage(const RequestBag& requestBag)
{
	ResponseBag responseBag(requestBag.GetEcho());

	if (currentChatRoom == nullptr)
	{
		responseBag.SetError(403, "用户未加入聊天室");
		Deliver(responseBag.ToJsonString());
		return;
	}

	string message = requestBag.GetParams().value("message", string());
	if (message.empty() || message.size() > server.GetConfig().maxMessageLength)
	{
		responseBag.SetError(400, "消息内容非法或超长");
		DeliverResponse(responseBag);
		return;
	}

	responseBag.AddData("message", message);
	DeliverResponse(responseBag);

	//广播消息事件给房间内其他成员（不含发送者本人，本人消息由 API 响应回显）
	EventMessageBag eventMessageBag("message");
	eventMessageBag.AddData("message", message);
	eventMessageBag.AddData("sender", userId);
	eventMessageBag.AddData("nickname", nickname);
	currentChatRoom->Broadcast(eventMessageBag, shared_from_this());
}

void UserSession::HandleGetRoomList(const RequestBag & requestBag)
{
	ResponseBag responseBag(requestBag.GetEcho());

	json roomList = json::array();
	for (auto& [roomId, room] : server.chatRoomMap)
	{
		roomList.push_back({ {"room_id", roomId}, {"room_name", room.GetName()}, {"user_count", room.GetUserCount()} });
	}

	responseBag.AddData("room_info_list", roomList);
	DeliverResponse(responseBag);
}

void UserSession::HandleJoinRoom(const RequestBag& requestBag)
{
	ResponseBag responseBag(requestBag.GetEcho());

	int roomId = requestBag.GetParams()["room_id"];
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
	eventMessageBag.AddData("nickname", nickname);
	currentChatRoom->Broadcast(eventMessageBag);
	server.BroadcastRoomListUpdate();
}

void UserSession::HandleCreateRoom(const RequestBag& requestBag)
{
	ResponseBag responseBag(requestBag.GetEcho());

	string roomName = requestBag.GetParams().value("room_name", string());
	if (roomName.empty() || roomName.size() > 32)
	{
		responseBag.SetError(400, "房间名不合法（非空且不超过32字符）");
		DeliverResponse(responseBag);
		return;
	}

	int roomId = server.CreateChatRoom(roomName);
	if (roomId < 0)
	{
		responseBag.SetError(409, "房间名已存在");
		DeliverResponse(responseBag);
		return;
	}

	//创建者自动加入
	server.JoinChatRoom(roomId, shared_from_this());

	responseBag.AddData("room_id", roomId);
	responseBag.AddData("room_name", roomName);
	DeliverResponse(responseBag);
	server.BroadcastRoomListUpdate();
}

void UserSession::HandleLogout(const RequestBag& requestBag)
{
	ResponseBag responseBag(requestBag.GetEcho());

	auto rpcResponse = gRPCServiceClient.Logout(requestBag.GetToken());
	if (!rpcResponse.success())
	{
		spdlog::error("注销失败，userId: {}", userId);
		responseBag.SetError(1, "注销失败");
		DeliverResponse(responseBag);
		CloseAfterFlush();
		return;
	}

	spdlog::info("用户注销: userId={}", userId);
	DeliverResponse(responseBag);
	CloseAfterFlush();
}

void UserSession::HandleRequest(const RequestBag & requestBag)
{
	// TODO 处理请求API
}

void UserSession::LeaveCurrentRoom()
{
	if (currentChatRoom == nullptr)
	{
		return;
	}

	ChatRoom* room = currentChatRoom;
	currentChatRoom = nullptr;
	room->RemoveParticipant(shared_from_this());

	if (room->GetUserCount() > 0)
	{
		EventMessageBag notice("notice");
		notice.AddData("notice_type", "leave_room");
		notice.AddData("user_id", userId);
		notice.AddData("nickname", nickname);
		room->Broadcast(notice);
		server.BroadcastRoomListUpdate();
	}
	else if (!room->IsSystem())
	{
		//用户创建的房间空置后自动关闭
		server.RemoveChatRoom(room->GetId());
	}
	else
	{
		server.BroadcastRoomListUpdate();
	}
}

void UserSession::Close()
{
	if (closed)
	{
		return;
	}
	closed = true;

	spdlog::info("关闭连接: userId={}", userId);
	LeaveCurrentRoom();

	boost::system::error_code ec;
	socket.shutdown(boost::asio::ip::tcp::socket::shutdown_both, ec);
	socket.close(ec);

	//延后移除会话，避免在异步处理器栈内销毁对象
	server.RequestRemoveSession(shared_from_this());
}

void UserSession::CloseAfterFlush()
{
	if (closed)
	{
		return;
	}
	closeAfterFlush = true;
	if (writeQueue.empty())
	{
		Close();
	}
}

bool UserSession::ValidateToken(const string& token, ResponseBag& responseBag)
{
	if (token.empty())
	{
		responseBag.SetError(502, "令牌不合法");
		DeliverResponse(responseBag);
		return false;
	}

	//以 token 声明中的用户身份为准进行校验，不依赖会话内保存的 userId
	auto rpcStart = std::chrono::steady_clock::now();
	auto infoResponse = gRPCServiceClient.GetSessionInfo(token);
	spdlog::debug("GetSessionInfo RPC 耗时: {} ms", std::chrono::duration_cast<std::chrono::milliseconds>(std::chrono::steady_clock::now() - rpcStart).count());
	if (infoResponse.user_id() <= 0 || infoResponse.user_id() != userId)
	{
		responseBag.SetError(502, "令牌不合法");
		DeliverResponse(responseBag);
		return false;
	}
	return true;
}
