#include "ChatRoom.h"
#include "UserSession.h"

void ChatRoom::AddParticipant(std::shared_ptr<UserSession> participant)
{
	participants.push_back(participant);
}

void ChatRoom::RemoveParticipant(std::shared_ptr<UserSession> participant)
{
	participants.erase(std::remove(participants.begin(), participants.end(), participant), participants.end());
}

void ChatRoom::Broadcast(const string& message)
{
	for (auto& participant : participants)
	{
		participant->Deliver(message);
	}
}
