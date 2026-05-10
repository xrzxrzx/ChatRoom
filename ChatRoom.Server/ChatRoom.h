#pragma once

#include<vector>
#include<string>
#include"UserSession.h"

using std::vector;
using std::string;

class ChatRoom
{
public:
	ChatRoom(string name, int id) : name(std::move(name)), id(id) {}

	void AddParticipant(std::shared_ptr<UserSession> participant);
	void RemoveParticipant(std::shared_ptr<UserSession> participant);

	string GetName() const { return name; }
	int GetId() const { return id; }

private:
	string name;
	int id;
	vector<std::shared_ptr<UserSession>> participants;
};

