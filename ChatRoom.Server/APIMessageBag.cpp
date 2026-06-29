#include "APIMessageBag.h"
#include <string>

using std::string;

APIMessageBag::RequestBag::RequestBag(string rawMessage)
{
	json jsonMessage = json::parse(rawMessage);
	action = jsonMessage["action"].get<string>();
	params = jsonMessage["params"];
	echo = jsonMessage["echo"].get<string>();
}

APIMessageBag::ResponseBag::ResponseBag(const string& echo)
{
	this->recode = 0;
	this->message = "";
	this->data = json::object();
	this->echo = echo;
}

string APIMessageBag::ResponseBag::ToJsonString() const
{
	json jsonMessage;
	jsonMessage["recode"] = recode;
	jsonMessage["message"] = message;
	jsonMessage["echo"] = echo;
	jsonMessage["data"] = data;
	return jsonMessage.dump();
}
