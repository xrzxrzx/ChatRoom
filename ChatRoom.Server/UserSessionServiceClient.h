#pragma once

#include<string>

#include<grpcpp/grpcpp.h>
#include <gRPCUserSession.grpc.pb.h>
#include <gRPCUserSession.pb.h>

using std::string;

using grpc::Channel;
using grpc::ClientContext;
using grpc::Status;
using grpc::ClientReader;
using grpc::ClientReaderWriter;

using gRPCUserSession::UserSessionService;
using gRPCUserSession::RegisterRequest;
using gRPCUserSession::RegisterResponse;
using gRPCUserSession::LoginRequest;
using gRPCUserSession::LoginResponse;
using gRPCUserSession::LogoutRequest;
using gRPCUserSession::LogoutResponse;
using gRPCUserSession::ValidateSessionRequest;
using gRPCUserSession::ValidateSessionResponse;
using gRPCUserSession::RefreshSessionRequest;
using gRPCUserSession::RefreshSessionResponse;
using gRPCUserSession::SessionInfoRequest;
using gRPCUserSession::SessionInfoResponse;

class IUserSessionServiceClient 
{
public:
	virtual RegisterResponse Register(const string& nickname, const string& password) = 0;
	virtual LoginResponse Login(int userId, const string& password) = 0;
	virtual LogoutResponse Logout(const string& sessionToken) = 0;
	virtual ValidateSessionResponse ValidateSession(int userId, const string& sessionToken) = 0;
	virtual RefreshSessionResponse RefreshSession(int userId, const string& sessionToken) = 0;
	virtual SessionInfoResponse GetSessionInfo(const string& sessionToken) = 0;
};

class UserSessionServiceClient : public IUserSessionServiceClient
{
public:
	UserSessionServiceClient(const string& address);

	RegisterResponse Register(const string& nickname, const string& password);
	LoginResponse Login(int userId, const string& password);
	LogoutResponse Logout(const string& sessionToken);
	ValidateSessionResponse ValidateSession(int userId, const string& sessionToken);
	RefreshSessionResponse RefreshSession(int userId, const string& sessionToken);
	SessionInfoResponse GetSessionInfo(const string& sessionToken);


private:
	std::unique_ptr<UserSessionService::Stub> stub_;
};
