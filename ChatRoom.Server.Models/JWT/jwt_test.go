package JWT

import (
	"testing"
	"time"
)

func TestGenerateTokenAndParse(t *testing.T) {
	tokenString, err := GenerateToken(42)
	if err != nil {
		t.Fatalf("GenerateToken 失败: %v", err)
	}
	if tokenString == "" {
		t.Fatal("生成的 token 为空")
	}

	claims, err := ParseToken(tokenString)
	if err != nil {
		t.Fatalf("ParseToken 失败: %v", err)
	}
	if claims.UserId != 42 {
		t.Errorf("期望 UserId=42，实际 %d", claims.UserId)
	}
}

func TestIsTokenExpired_ValidToken(t *testing.T) {
	tokenString, err := GenerateToken(1)
	if err != nil {
		t.Fatalf("GenerateToken 失败: %v", err)
	}
	if IsTokenExpired(tokenString) {
		t.Error("有效 token 不应被判为过期")
	}
}

func TestIsTokenExpired_ExpiredToken(t *testing.T) {
	tokenString, err := GenerateTokenWithExpiry(1, time.Now().Add(-1*time.Hour))
	if err != nil {
		t.Fatalf("GenerateTokenWithExpiry 失败: %v", err)
	}
	if !IsTokenExpired(tokenString) {
		t.Error("过期 token 应被判为过期")
	}
}

func TestIsTokenExpired_ForgedToken(t *testing.T) {
	if !IsTokenExpired("forged.token.string") {
		t.Error("伪造 token 应被判为过期（无效）")
	}
}

func TestRevokeToken(t *testing.T) {
	tokenString, err := GenerateToken(1)
	if err != nil {
		t.Fatalf("GenerateToken 失败: %v", err)
	}

	if IsTokenRevoked(tokenString) {
		t.Error("新 token 不应在注销黑名单中")
	}

	if err := RevokeToken(tokenString); err != nil {
		t.Fatalf("RevokeToken 失败: %v", err)
	}
	if !IsTokenRevoked(tokenString) {
		t.Error("注销后 token 应在黑名单中")
	}
	if IsTokenExpired(tokenString) {
		t.Error("注销不应改变 token 的过期状态")
	}
}

func TestSetSecret(t *testing.T) {
	original := jwtSecret
	defer func() { jwtSecret = original }()

	SetSecret([]byte("new-secret-for-test"))

	tokenString, err := GenerateToken(2)
	if err != nil {
		t.Fatalf("GenerateToken 失败: %v", err)
	}

	//旧密钥解析新 token 应失败
	oldSecret := jwtSecret
	jwtSecret = original
	if _, err := ParseToken(tokenString); err == nil {
		t.Error("旧密钥不应能解析新密钥签发的 token")
	}
	jwtSecret = oldSecret
}
