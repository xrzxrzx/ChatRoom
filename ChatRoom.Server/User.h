#pragma once

#include<string>

class User
{
public:
	User(const std::string& name, int id);
	~User();

	std::string GetName() const;
	int GetId() const;
private:
	std::string m_name;
	int m_id;
};

