#include "ChatServerService.h"
#include"StringTool.hpp"
#include"EventMessageBag.h"

#include<spdlog/spdlog.h>
#include"UserSession.h"
#include<nlohmann/json.hpp>

#include<algorithm>
#include<chrono>
#include<ctime>

using json = nlohmann::json;

ChatServerService::ChatServerService(short port, ServerConfig config)
	: acceptor(ioContext, tcp::endpoint(tcp::v4(), port)), socket(ioContext),
	  heartbeatTimer(ioContext), config(std::move(config))
{
	for (const auto& name : this->config.rooms)
	{
		AddChatRoom(name, true);
	}
}

void ChatServerService::StartAccept()
{
	spdlog::info("开始接受连接...");
	do_accept();
	StartHeartbeat();
	ioContext.run();
}

int ChatServerService::AddChatRoom(const string& name, bool isSystem)
{
	int id = nextRoomId++;
	chatRoomMap.emplace(id, ChatRoom(name, id, isSystem));
	spdlog::info("添加房间: id={}, name={}, isSystem={}", id, name, isSystem);
	return id;
}

int ChatServerService::CreateChatRoom(const string& name)
{
	for (const auto& [_, room] : chatRoomMap)
	{
		if (room.GetName() == name)
		{
			spdlog::warn("创建房间失败，房间名已存在: {}", name);
			return -1;
		}
	}
	return AddChatRoom(name, false);
}

void ChatServerService::SetSessionFactory(std::function<std::shared_ptr<UserSession>()> factory)
{
	sessionFactory = std::move(factory);
}

void ChatServerService::do_accept()
{
	acceptor.async_accept(socket,
		[this](boost::system::error_code ec)
		{
			if (!ec)
			{
				if (!sessionFactory)
				{
					spdlog::error("UserSession factory 未初始化");
				}
				else
				{
					auto newSession = sessionFactory();
					sessions.push_back(newSession);
					newSession->Init(std::move(socket));
					spdlog::info("新连接已建立，当前在线会话数: {}", sessions.size());
				}
			}
			else
			{
				spdlog::error("Accept error: {}", ToUtf8(ec.message()));
			}
			do_accept();
		});
}

vector<string> ChatServerService::GetChatRoomNames() const
{
	vector<string> names;
	for (auto& [_, chatRoom] : chatRoomMap)
	{
		names.push_back(chatRoom.GetName());
	}
	return names;
}

vector<int> ChatServerService::GetChatRoomIds() const
{
	vector<int> ids;
	for (auto& [id, _] : chatRoomMap)
	{
		ids.push_back(id);
	}
	return ids;
}

string ChatServerService::GetChatRoomNameById(int id) const
{
	auto it = chatRoomMap.find(id);
	if (it != chatRoomMap.end())
	{
		return it->second.GetName();
	}
	return {};
}

bool ChatServerService::JoinChatRoom(int roomId, std::shared_ptr<UserSession> participant)
{
	participant->LeaveCurrentRoom();

	auto it = chatRoomMap.find(roomId);
	if (it != chatRoomMap.end())
	{
		it->second.AddParticipant(participant);
		participant->currentChatRoom = &it->second;
		return true;
	}
	spdlog::error("未找到房间号: {}", roomId);
	return false;
}

void ChatServerService::RemoveChatRoom(int roomId)
{
	auto it = chatRoomMap.find(roomId);
	if (it == chatRoomMap.end())
	{
		return;
	}
	chatRoomMap.erase(it);
	spdlog::info("用户创建的房间已关闭: roomId={}", roomId);
	BroadcastRoomListUpdate();
}

void ChatServerService::BroadcastRoomListUpdate()
{
	json roomList = json::array();
	for (const auto& [roomId, room] : chatRoomMap)
	{
		roomList.push_back({ {"room_id", roomId}, {"room_name", room.GetName()}, {"user_count", room.GetUserCount()} });
	}

	EventMessageBag event("update");
	event.AddData("update_type", "room_list");
	event.AddData("update_data", roomList);

	for (const auto& session : sessions)
	{
		session->DeliverEvent(event);
	}
}

void ChatServerService::RequestRemoveSession(const std::shared_ptr<UserSession>& session)
{
	//延后移除：等待所有已排队的异步处理器执行完毕，避免悬垂 this
	boost::asio::post(ioContext, [this, session] { RemoveSession(session); });
}

void ChatServerService::RemoveSession(const std::shared_ptr<UserSession>& session)
{
	auto it = std::find(sessions.begin(), sessions.end(), session);
	if (it != sessions.end())
	{
		sessions.erase(it);
		spdlog::info("会话已移除，当前在线会话数: {}", sessions.size());
	}
}

void ChatServerService::StartHeartbeat()
{
	heartbeatTimer.expires_after(std::chrono::seconds(config.heartbeatIntervalSec));
	heartbeatTimer.async_wait([this](boost::system::error_code ec)
		{
			if (ec)
			{
				return;
			}
			BroadcastHeartbeat();
			CheckIdleSessions();
			StartHeartbeat();
		});
}

void ChatServerService::BroadcastHeartbeat()
{
	EventMessageBag event("heartbeat");
	event.AddData("time", static_cast<int64_t>(std::time(nullptr)));
	for (const auto& session : sessions)
	{
		session->DeliverEvent(event);
	}
}

void ChatServerService::CheckIdleSessions()
{
	auto now = std::chrono::steady_clock::now();
	for (const auto& session : sessions)
	{
		if (now - session->GetLastActivity() > std::chrono::seconds(config.idleTimeoutSec))
		{
			spdlog::warn("会话空闲超时，断开连接: userId={}", session->GetUserId());
			session->Close();
		}
	}
}
