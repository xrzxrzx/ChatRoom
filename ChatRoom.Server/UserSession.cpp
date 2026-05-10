#include "UserSession.h"

#include<spdlog/spdlog.h>
#include<nlohmann/json.hpp>
#include"APIMessageBag.h"
#include"EventMessageBag.h"

namespace asio = boost::asio;
using APIMessageBag::ResquestBag;
using APIMessageBag::ResponseBag;

void UserSession::Init()
{
	asio::async_read_until(socket, readBuffer, '\n',
		[this](boost::system::error_code ec, std::size_t bytes_transferred)
		{
			if (!ec)
			{
				std::istream is(&readBuffer);
				string line;
				std::getline(is, line);
				ResquestBag requestBag(line);
				if (requestBag.GetAction() == "login")
				{
					spdlog::info("用户请求登录");
					// TODO : 处理登录逻辑，获取用户信息后加入聊天室
				}
				else
				{
					// TODO : 处理其他接口调用，用户仅能调用一次 login 接口，否则会被服务器断开连接
				}
			}
			else
			{
				spdlog::error("接收消息出错: {}", ec.message());
			}
		});
}

void UserSession::Deliver(const string& message)
{
	bool write_in_progress = !writeQueue.empty();
	writeQueue.push(message);
	if (!write_in_progress) {
		do_write();
	}
}

void UserSession::do_write()
{
	asio::async_write(socket, asio::buffer(writeQueue.front()),
		[this](boost::system::error_code ec, std::size_t /*length*/)
		{
			if (!ec)
			{
				writeQueue.pop();
				if (!writeQueue.empty()) {
					do_write();
				}
			}
			else
			{
				spdlog::error("发送消息出错: {}", ec.message());
			}
		});
}

void UserSession::do_read()
{
	asio::async_read_until(socket, readBuffer, '\n',
		[this](boost::system::error_code ec, std::size_t bytes_transferred)
		{
			if (!ec)
			{
				std::istream is(&readBuffer);
				string line;
				std::getline(is, line);
				ResquestBag requestBag(line);
				// TODO : 处理用户发送的消息
			}
			else
			{
				spdlog::error("接收消息出错: {}", ec.message());
			}
		});
}
