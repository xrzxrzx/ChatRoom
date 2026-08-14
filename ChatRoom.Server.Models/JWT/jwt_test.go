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
