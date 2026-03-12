#include "User.h"

User::User(const std::string& name, int id)
{
    m_name = name;
	m_id = id;
}

std::string User::GetName() const
{
    return m_name;
}

int User::GetId() const
{
    return m_id;
}
