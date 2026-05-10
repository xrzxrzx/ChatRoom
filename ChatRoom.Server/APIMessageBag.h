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
		ResponseBag();

	private:
	};
}
