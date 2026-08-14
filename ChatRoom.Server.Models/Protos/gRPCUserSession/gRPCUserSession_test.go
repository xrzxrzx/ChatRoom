package gRPCUserSession

import (
	"ChatRoom.Server.Models/JWT"
	"context"
	"testing"
	"time"
)

func TestValidateSession_ValidToken(t *testing.T) {
	tokenString, err := JWT.GenerateToken(7)
	if err != nil {
		t.Fatalf("GenerateToken 失败: %v", err)
	}

	server := &Server{}
	response, err := server.ValidateSession(context.Background(), &ValidateSessionRequest{
		SessionToken: tokenString,
	})
	if err != nil {
		t.Fatalf("ValidateSession 出错: %v", err)
	}
	if !response.IsValid {
		t.Error("有效 token 应返回 is_valid=true")
	}
}

func TestValidateSession_ExpiredToken(t *testing.T) {
	tokenString, err := JWT.GenerateTokenWithExpiry(7, time.Now().Add(-1*time.Hour))
	if err != nil {
		t.Fatalf("GenerateTokenWithExpiry 失败: %v", err)
	}

	server := &Server{}
	response, err := server.ValidateSession(context.Background(), &ValidateSessionRequest{
		SessionToken: tokenString,
	})
	if err != nil {
		t.Fatalf("ValidateSession 出错: %v", err)
	}
	if response.IsValid {
		t.Error("过期 token 应返回 is_valid=false")
	}
}

func TestValidateSession_ForgedToken(t *testing.T) {
	server := &Server{}
	response, err := server.ValidateSession(context.Background(), &ValidateSessionRequest{
		SessionToken: "forged.token.string",
	})
	if err != nil {
		t.Fatalf("ValidateSession 出错: %v", err)
	}
	if response.IsValid {
		t.Error("伪造 token 应返回 is_valid=false")
	}
}

