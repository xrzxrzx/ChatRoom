#pragma once

#include<vector>
#include<string>
#include<memory>

using std::vector;
using std::string;

class UserSession;
class EventMessageBag;

class ChatRoom
{
public:
	ChatRoom(string name, int id) : name(std::move(name)), id(id) {}

	void AddParticipant(std::shared_ptr<UserSession> participant);
	void RemoveParticipant(std::shared_ptr<UserSession> participant);

	void Broadcast(const EventMessageBag& eventMessageBag);

	string GetName() const { return name; }
	int GetId() const { return id; }
	int GetUserCount() const { return participants.size(); }

private:
	string name;
	int id;
	vector<std::shared_ptr<UserSession>> participants;
};

