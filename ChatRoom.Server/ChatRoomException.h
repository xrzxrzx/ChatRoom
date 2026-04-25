#pragma once

#include<string>

using std::string;

namespace ChatRoomException
{
	class APITimeOutException
	{
	public:
		APITimeOutException(const string& message, int errorCode) : message(message), errorCode(errorCode) {}
		const char* what() const noexcept { return message.c_str(); }
		int code() const noexcept { return errorCode; }

	private:
		string message;
		int errorCode;
	};

	class InvalidParameterException
	{
	public:
		InvalidParameterException(const string& message, int errorCode) : message(message), errorCode(errorCode) {}
		const char* what() const noexcept { return message.c_str(); }
		int code() const noexcept { return errorCode; }

	private:
		string message;
		int errorCode;
	};
}

