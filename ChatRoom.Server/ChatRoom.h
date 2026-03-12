#pragma once

#include<list>
#include "User.h"

class ChatRoom
{
public:
	ChatRoom();
	~ChatRoom();
	void Join(const User& user);
	void Leave(const User& user);
	void DeliverMessage(const User& sender, const std::string& message);

private:
	std::list<User> m_users;
};