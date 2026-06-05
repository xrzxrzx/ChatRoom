#pragma once

#include<string>
#include<nlohmann/json.hpp>

namespace APIMessageBag
{
	using std::string;
	using json = nlohmann::json;

	class RequestBag
	{
	public:
		RequestBag(string rawMessage);

		string GetAction() const { return action; }
		json GetData() const { return data; }
		string GetEcho() const { return echo; }
		string GetToken() const { return token; }

	private:
		string action;
		json data;
		string token;
		string echo;
	};

	class ResponseBag
	{
	public:
		ResponseBag(const string& echo);

		void SetError(int recode, const string& message) { this->recode = recode; this->message = message; }
		void AddData(const string& key, const json& value) { data[key] = value; }
		string ToJsonString() const;

	private:
		ResponseBag() = default;

		int recode;
		string message;
		string echo;
		json data;
	};
}
