package gRPCUserSession

import (
	"ChatRoom.Server.Models/Database"
	"ChatRoom.Server.Models/JWT"
	"context"
	"fmt"
)

type Server struct {
	UnimplementedUserSessionServiceServer
}

func (server *Server) Register(ctx context.Context, req *RegisterRequest) (*RegisterResponse, error) {
	var response RegisterResponse
	var user Database.User
	err := user.CreateUser(req.Password, req.Nickname)
	if err != nil {
		response.Success = false
		response.SessionToken = ""
		return &response, fmt.Errorf("创建用户失败")
	}

	tokenString, err := JWT.GenerateToken(user.Id)
	if err != nil {
		response.Success = false
		response.SessionToken = ""
		return &response, fmt.Errorf("生成JWT令牌失败")
	}

	response.Success = true
	response.SessionToken = tokenString
	return &response, nil
}

func (server *Server) Login(ctx context.Context, req *LoginRequest) (*LoginResponse, error) {

}

func (server *Server) Logout(ctx context.Context, req *LogoutRequest) (*LogoutResponse, error) {

}

func (server *Server) GetSessionInfo(ctx context.Context, req *SessionInfoRequest) (*SessionInfoResponse, error) {

}

func (server *Server) RefreshSession(ctx context.Context, req *RefreshSessionRequest) (*RefreshSessionResponse, error) {

}

func (server *Server) ValidateSession(ctx context.Context, req *SessionInfoRequest) (*SessionInfoResponse, error) {

}
