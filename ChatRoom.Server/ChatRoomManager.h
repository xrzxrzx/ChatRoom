#pragma once

#include "ChatRoom.h"
#include <list>

class ChatRoomManager
{
private:
	std::list<ChatRoom> m_chatRooms;
public:
	void AddChatRoom(const ChatRoom& chatRoom);
	void RemoveChatRoom(const ChatRoom& chatRoom);
};

