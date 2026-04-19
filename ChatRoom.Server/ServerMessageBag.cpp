#include "ServerMessageBag.h"
using namespace ServerMessage;

ServerMessageBag::ServerMessageBag(const string& type)
{
	_recode = 0;
	_message = "";
	_type = type;
}

ServerMessageBag ServerMessageBag::AddRecode(int recode, const string& message)
{
	_recode = recode;
	_message = message;

	return *this;
}

string ServerMessageBag::ToJsonString() const
{
	json j;
	j["type"] = _type;
	j["recode"] = _recode;
	j["msg"] = _message;
	j["data"] = _data;
	return j.dump();
}

template<typename T>
ServerMessageBag ServerMessageBag::AddData(const string& key, const T& value)
{
	_data[key] = value;
	return *this;
}