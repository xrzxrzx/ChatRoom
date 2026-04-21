#include "ClientMessageBag.h"

using namespace ClientMessage;

RequestBag::RequestBag(const string& raw_message)
{
	json j = json::parse(raw_message);
	_command = j["command"].get<string>();
	_echo = j["echo"].get<string>();
	_parameters = j["params"];
}

CommandType RequestBag::GetCommand()
{
	CommandType command;
	if (_command == "message")
	{
		command = CommandType::Message;
	}
	else if(_command == "request")
	{
		command = CommandType::Request;
	}

	return command;
}

string& RequestBag::GetEcho()
{
	return _echo;
}

json& RequestBag::GetParameters()
{
	return _parameters;
}

ResponseBag::ResponseBag(const string& echo)
{
	_recode = 0;
	_message = "";
	_echo = echo;
}

ResponseBag ResponseBag::AddRecode(int recode, const string& message)
{
	_recode = recode;
	_message = message;
	return *this;
}

string ResponseBag::ToJsonString() const
{
	json j;
	j["echo"] = _echo;
	j["recode"] = _recode;
	j["msg"] = _message;
	j["data"] = _data;
	return j.dump();
}

template<typename T>
ResponseBag ResponseBag::AddData(const string& key, const T& value)
{
	_data[key] = value;
	return *this;
}