#pragma once

#include<string>
#include<nlohmann/json.hpp>

using std::string;
using json = nlohmann::json;

namespace ClientMessage
{
	enum CommandType
	{
		Message,
		Request
	};

	class RequestBag
	{
	public:
		RequestBag(const string& raw_message);
		CommandType GetCommand();
		string& GetEcho();
		json& GetParameters();

	private:
		string _command;
		string _echo;
		json _parameters;
	};

	class ResponseBag
	{
	public:
		ResponseBag(const string& echo);
		ResponseBag AddRecode(int recode, const string& message);
		string ToJsonString() const;

		template<typename T>
		ResponseBag AddData(const string& key, const T& value);

	private:
		int _recode;
		string _message;
		string _echo;
		json _data;
	};
};