#include "APIMessageBag.h"

APIMessageBag::ResquestBag::ResquestBag(std::string rawMessage)
{
	json jsonMessage = json::parse(rawMessage);
	action = jsonMessage["action"].get<string>();
	data = jsonMessage["data"];
	echo = jsonMessage["echo"].get<string>();
}
