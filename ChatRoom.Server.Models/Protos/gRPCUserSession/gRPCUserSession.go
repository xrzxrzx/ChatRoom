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

	response.Success = false
	response.SessionToken = ""

	err := user.CreateUser(req.Password, req.Nickname)
	if err != nil {
		return &response, fmt.Errorf("创建用户失败")
	}

	tokenString, err := JWT.GenerateToken(user.Id)
	if err != nil {
		return &response, fmt.Errorf("生成JWT令牌失败")
	}

	response.Success = true
	response.SessionToken = tokenString
	return &response, nil
}

func (server *Server) Login(ctx context.Context, req *LoginRequest) (*LoginResponse, error) {
	var response LoginResponse
	var user Database.User
	var err error

	response.Success = false
	response.SessionToken = ""
	response.Nickname = ""

	err = user.GetUserById(req.UserId)

	if err != nil {
		return &response, fmt.Errorf("用户不存在")
	}

	if user.Password != req.Password {
		return &response, fmt.Errorf("用户密码错误")
	}

	tokenString, err := JWT.GenerateToken(user.Id)
	if err != nil {
		return &response, fmt.Errorf("生成JWT令牌失败")
	}

	response.Success = true
	response.Nickname = user.Nickname
	response.SessionToken = tokenString
	return &response, nil
}

func (server *Server) Logout(ctx context.Context, req *LogoutRequest) (*LogoutResponse, error) {

}

func (server *Server) GetSessionInfo(ctx context.Context, req *SessionInfoRequest) (*SessionInfoResponse, error) {

}

func (server *Server) RefreshSession(ctx context.Context, req *RefreshSessionRequest) (*RefreshSessionResponse, error) {

}

func (server *Server) ValidateSession(ctx context.Context, req *SessionInfoRequest) (*SessionInfoResponse, error) {

}
