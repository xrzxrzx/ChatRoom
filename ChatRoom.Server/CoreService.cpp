#include "CoreService.h"

CoreService::CoreService(const tcp::endpoint& endpoint)
	: _chatServer(ioContext, endpoint)
{
}

void CoreService::StartCoreService()
{
	_chatServer.SetMessageCommandHandle(HandleMessageCommand);
	_chatServer.SetRequestCommandHandle(HandleRequestCommand);

	_chatServer.StartAccept();
	ioContext.run();
}

json& CoreService::HandleMessageCommand(const string& echo, const json& params)
{
	static json responseData;
	responseData.clear();

	responseData["echo"] = echo;
	responseData["params"] = params;
	responseData["recode"] = 0;
	responseData["msg"] = "消息处理成功";

	return responseData;
}

json& CoreService::HandleRequestCommand(const string & echo, const json & params)
{
	static json responseData;
	responseData.clear();

	int sender = params.value("sender", 0);

	json senderInfo;
	senderInfo["sender"] = sender;

	responseData["data"] = senderInfo;
	responseData["recode"] = 0;
	responseData["msg"] = "请求成功";

	return responseData;
}
