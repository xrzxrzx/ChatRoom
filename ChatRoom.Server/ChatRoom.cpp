#include "ChatRoom.h"

ChatRoom::ChatRoom()
{
}

void ChatRoom::Join(const User& user)
{
	m_users.push_back(user);
}

void ChatRoom::Leave(const User& user)
{
	m_users.remove(user);
}
