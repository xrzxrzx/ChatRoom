#include "UserSession.h"
#include"ChatRoomException.h"
#include <iostream>

using std::cout;
using std::endl;

void UserSession::Start()
{
	do_recvive();
}

void UserSession::do_recvive()
{
	auto self(shared_from_this());

	asio::async_read_until(_socket, _buffer, "\n",
		[this, self](const boost::system::error_code& error, std::size_t length)
		{
			if (!error)
			{
				std::istream is(&_buffer);
				std::string message;
				std::getline(is, message);
				
				//接收消息处理
				OnMessageReceived(message);

				do_recvive();
			}
			else
			{
				cout << "错误: " << error.message() << endl;
			}
		});
}

void UserSession::do_send(const std::string& message)
{
	auto self(shared_from_this());

	asio::async_write(_socket, asio::buffer(message + "\n"),
		[this, self](const boost::system::error_code& error, std::size_t length)
		{
			if (!error)
			{
				_messageQueue.pop();
				if (!_messageQueue.empty())
				{
					do_send(_messageQueue.front());
				}
			}
			else
			{
				cout << "错误: " << error.message() << endl;
			}
		});
}

void UserSession::OnMessageReceived(const std::string& message)
{
	json responseData;
	RequestBag requestBag(message);
	ResponseBag responseBag(requestBag.GetEcho());

	try
	{
		responseData = SwitchCommand(requestBag);
	}
	catch (const ChatRoomException::APITimeOutException& e)
	{
		responseBag.AddRecode(e.code(), e.what());
	}
	catch (const ChatRoomException::InvalidParameterException& e)
	{
		responseBag.AddRecode(e.code(), e.what());
	}
	catch (const std::exception& e)
	{
		cout << "未知错误: " << e.what() << endl;
	}

	responseBag.AddData("data", responseData["data"]);
	SendMessage(responseBag.ToJsonString());
}

//消息分拣
json& UserSession::SwitchCommand(const ClientMessage::RequestBag& requestBag)
{
	using ClientMessage::CommandType;

	json responseData;

	switch (requestBag.GetCommand())
	{
	case CommandType::Message:
		return _messageCommandHandle(requestBag.GetEcho(), requestBag.GetParameters());
	case CommandType::Request:
		return _requestCommandHandle(requestBag.GetEcho(), requestBag.GetParameters());
	default:
		static json default_json;
		return default_json;
	}
}

void UserSession::SendMessage(const std::string& message)
{
	bool write_in_progress = !_messageQueue.empty();
	_messageQueue.push(message);
	if(!write_in_progress)
	{
		do_send(_messageQueue.front());
	}
}

void UserSession::SetMessageCommandHandle(std::function<json&(const std::string& echo, const json& params)> handle)
{
	_messageCommandHandle = handle;
}

void UserSession::SetRequestCommandHandle(std::function<json&(const std::string& echo, const json& params)> handle)
{
	_requestCommandHandle = handle;
}