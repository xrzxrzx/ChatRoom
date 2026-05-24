#include "UserSessionServiceClient.h"

#include<spdlog/spdlog.h>

RegisterResponse UserSessionServiceClient::Register(const string& nickname, const string& password)
{
	RegisterRequest request;
	request.set_nickname(nickname);
	request.set_password(password);

	RegisterResponse response;
	ClientContext context;
	Status status = stub_->Register(&context, request, &response);
	if (!status.ok()) {
		spdlog::error("注册 RPC 出错: {}", status.error_message());
		response.set_success(false);
	}
	return response;
}

LoginResponse UserSessionServiceClient::Login(int userId, const string& password)
{
	LoginRequest request;
	request.set_user_id(userId);
	request.set_password(password);

	LoginResponse response;
	ClientContext context;
	Status status = stub_->Login(&context, request, &response);
	if (!status.ok()) {
		spdlog::error("登录 RPC 出错: {}", status.error_message());
		response.set_success(false);
	}
	return response;
}

LogoutResponse UserSessionServiceClient::Logout(const string& sessionToken)
{
	LogoutRequest request;
	request.set_session_token(sessionToken);

	LogoutResponse response;
	ClientContext context;
	Status status = stub_->Logout(&context, request, &response);
	if (!status.ok()) {
		spdlog::error("注销 RPC 出错: {}", status.error_message());
		response.set_success(false);
	}
	return response;
}

ValidateSessionResponse UserSessionServiceClient::ValidateSession(int userId, const string& sessionToken)
{
	ValidateSessionRequest request;
	request.set_session_token(sessionToken);

	ValidateSessionResponse response;
	ClientContext context;
	Status status = stub_->ValidateSession(&context, request, &response);
	if (!status.ok()) {
		spdlog::error("验证会话 RPC 出错: {}", status.error_message());
		response.set_is_valid(false);
	}
	return response;
}

RefreshSessionResponse UserSessionServiceClient::RefreshSession(int userId, const string& sessionToken)
{
	RefreshSessionRequest request;
	request.set_session_token(sessionToken);

	RefreshSessionResponse response;
	ClientContext context;
	Status status = stub_->RefreshSession(&context, request, &response);
	if (!status.ok()) {
		spdlog::error("刷新会话 RPC 出错: {}", status.error_message());
		response.set_new_session_token("");
	}
	return response;
}

SessionInfoResponse UserSessionServiceClient::GetSessionInfo(const string& sessionToken)
{
	SessionInfoRequest request;
	request.set_session_token(sessionToken);

	SessionInfoResponse response;
	ClientContext context;
	Status status = stub_->GetSessionInfo(&context, request, &response);
	if (!status.ok()) {
		spdlog::error("获取会话信息 RPC 出错: {}", status.error_message());
		response.set_user_id(0);
		response.set_nickname("");
	}
	return response;
}