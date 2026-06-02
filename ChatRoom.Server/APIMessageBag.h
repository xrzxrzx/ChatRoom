#pragma once

#include<string>
#include<nlohmann/json.hpp>

namespace APIMessageBag
{
	using std::string;
	using json = nlohmann::json;

	class ResquestBag
	{
	public:
		ResquestBag(string rawMessage);

		string GetAction() const { return action; }
		json GetData() const { return data; }
		string GetEcho() const { return echo; }

	private:
		string action;
		json data;
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
