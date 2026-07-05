#include "ChatServerService.h"
#include"StringTool.hpp"

#include<spdlog/spdlog.h>
#include"UserSession.h"

ChatServerService::ChatServerService(short port) : acceptor(ioContext, tcp::endpoint(tcp::v4(), port)), socket(ioContext)
{
	AddChatRoom("通用");
	AddChatRoom("测试");
	AddChatRoom("语言房间");
	AddChatRoom("文字聊天");
	AddChatRoom("超长文字房间");
}

void ChatServerService::StartAccept()
{
	spdlog::info("开始接受连接...");
	do_accept();
	ioContext.run();
}

void ChatServerService::AddChatRoom(const string& name)
{
	static int id = 0;
	chatRoomMap.emplace(id, ChatRoom(name, id));
	++id;
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
					return;
				}

				auto newSession = sessionFactory();
				newSession->Init(std::move(socket));
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
	if (participant->currentChatRoom)
	{
		participant->currentChatRoom->RemoveParticipant(participant);
		participant->currentChatRoom = nullptr;
	}

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
