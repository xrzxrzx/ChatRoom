#include "UserSession.h"
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