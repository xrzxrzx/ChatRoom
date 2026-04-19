#pragma once
#include<string>
#include<nlohmann/json.hpp>

using std::string;
using json = nlohmann::json;

namespace ServerMessage {
	class ServerMessageBag
	{
	private:
		int _recode;
		string _message;
		string _type;
		json _data;

	public:
		ServerMessageBag(const string& type);
		ServerMessageBag AddRecode(int recode, const string& message);

		template<typename T>
		ServerMessageBag AddData(const string& key, const T& value);

		string ToJsonString() const;
	};
}