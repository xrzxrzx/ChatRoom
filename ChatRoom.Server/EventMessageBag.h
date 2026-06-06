#pragma once

#include<string>
#include<nlohmann/json.hpp>

using std::string;
using json = nlohmann::json;

class EventMessageBag
{
public:
	EventMessageBag(const string& postType) : postType(postType){}
	void AddData(const string& key, const json& value) { data[key] = value; }
	string ToJsonString() const;

private:
	string postType;
	json data;
};

