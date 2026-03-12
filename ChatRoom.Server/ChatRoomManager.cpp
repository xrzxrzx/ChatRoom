#include "ChatRoomManager.h"

void ChatRoomManager::AddChatRoom(const ChatRoom& chatRoom)
{
	m_chatRooms.push_back(chatRoom);
}

void ChatRoomManager::RemoveChatRoom(const ChatRoom& chatRoom)
{
	m_chatRooms.remove(chatRoom);
}
