package gRPCUserSession

import (
	"ChatRoom.Server.Models/Database"
	"ChatRoom.Server.Models/JWT"
	"context"
	"errors"
	"fmt"
	"time"
)

type Server struct {
	UnimplementedUserSessionServiceServer
}

func (server *Server) Register(ctx context.Context, req *RegisterRequest) (*RegisterResponse, error) {
	var response RegisterResponse
	var user Database.User
	var err error

	response.Success = false
	response.UserId = 0
	response.SessionToken = ""

	err = user.CreateUser(req.Password, req.Nickname)
	if err != nil {
		if errors.Is(err, Database.ErrNicknameTaken) {
			return &response, fmt.Errorf("昵称已被占用")
		}
		return &response, fmt.Errorf("创建用户失败")
	}

	tokenString, err := JWT.GenerateToken(user.Id)
	if err != nil {
		return &response, fmt.Errorf("生成JWT令牌失败")
	}

	response.Success = true
	response.UserId = user.Id
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

	if !user.VerifyPassword(req.Password) {
		return &response, fmt.Errorf("用户密码错误")
	}

	tokenString, err := JWT.GenerateToken(user.Id)
	if err != nil {
		return &response, fmt.Errorf("生成JWT令牌失败")
	}

	response.Success = true
	response.Nickname = user.Nickname
	response.SessionToken = tokenString
	response.UserId = user.Id
	return &response, nil
}

func (server *Server) Logout(ctx context.Context, req *LogoutRequest) (*LogoutResponse, error) {
	var response LogoutResponse
	response.Success = false

	if JWT.IsTokenExpired(req.SessionToken) {
		return &response, fmt.Errorf("token已过期")
	}

	if err := JWT.RevokeToken(req.SessionToken); err != nil {
		return &response, err
	}

	response.Success = true
	return &response, nil
}

func (server *Server) GetSessionInfo(ctx context.Context, req *SessionInfoRequest) (*SessionInfoResponse, error) {
	var err error
	var response SessionInfoResponse
	response.UserId = 0
	response.Nickname = ""

	claims, err := JWT.ParseToken(req.SessionToken)
	if err != nil {
		return &response, err
	}

	// 不能直接使用IsTokenExpired判断，因为IsTokenExpired的实现依赖于ParseToken，会造成性能浪费
	if claims.ExpiresAt.Before(time.Now()) || JWT.IsTokenRevoked(req.SessionToken) {
		return &response, fmt.Errorf("token已过期")
	}

	var user Database.User
	err = user.GetUserById(claims.UserId)
	if err != nil {
		return &response, err
	}

	response.UserId = user.Id
	response.Nickname = user.Nickname
	return &response, nil
}

func (server *Server) RefreshSession(ctx context.Context, req *RefreshSessionRequest) (*RefreshSessionResponse, error) {
	var response RefreshSessionResponse
	response.NewSessionToken = ""

	newToken, err := JWT.RefreshToken(req.SessionToken)
	if err != nil {
		return &response, err
	}

	response.NewSessionToken = newToken
	return &response, nil
}

func (server *Server) ValidateSession(ctx context.Context, req *ValidateSessionRequest) (*ValidateSessionResponse, error) {
	var response ValidateSessionResponse
	response.IsValid = !JWT.IsTokenExpired(req.SessionToken) && !JWT.IsTokenRevoked(req.SessionToken)
	return &response, nil
}
