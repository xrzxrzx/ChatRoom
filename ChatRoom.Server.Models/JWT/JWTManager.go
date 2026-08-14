package JWT

import (
	"fmt"
	"github.com/golang-jwt/jwt/v5"
	"sync"
	"time"
)

type CustomClaims struct {
	UserId int32
	jwt.RegisteredClaims
}

// JWT秘钥（默认值仅用于本地开发，生产环境必须通过 SetSecret 覆盖）
var jwtSecret = []byte("your-secret-key-change-this")

// 已注销 token 黑名单（内存实现，服务重启后失效；生产可替换为 Redis）
var revokedTokens sync.Map

// SetSecret 设置 JWT 签名密钥（从配置读取）
func SetSecret(secret []byte) {
	if len(secret) > 0 {
		jwtSecret = secret
	}
}

// RevokeToken 将 token 加入注销黑名单
func RevokeToken(tokenString string) error {
	claims, err := ParseToken(tokenString)
	if err != nil {
		return err
	}

	revokedTokens.Store(tokenString, claims.ExpiresAt.Time)
	cleanupExpiredTokens()
	return nil
}

// IsTokenRevoked 判断 token 是否已被注销
func IsTokenRevoked(tokenString string) bool {
	_, ok := revokedTokens.Load(tokenString)
	return ok
}

// cleanupExpiredTokens 清理已过期的黑名单条目（尽力而为）
func cleanupExpiredTokens() {
	now := time.Now()
	revokedTokens.Range(func(key, value interface{}) bool {
		if expiry, ok := value.(time.Time); ok && expiry.Before(now) {
			revokedTokens.Delete(key)
		}
		return true
	})
}

func GenerateToken(id int32) (string, error) {
	return generateToken(id, time.Now().Add(1*time.Hour))
}

// GenerateTokenWithExpiry 生成指定过期时间的 token（主要供测试与刷新场景使用）
func GenerateTokenWithExpiry(id int32, expiresAt time.Time) (string, error) {
	return generateToken(id, expiresAt)
}

func generateToken(id int32, expiresAt time.Time) (string, error) {
	claims := CustomClaims{
		UserId: id,
		RegisteredClaims: jwt.RegisteredClaims{
			ExpiresAt: jwt.NewNumericDate(expiresAt),  // 过期时间
			IssuedAt:  jwt.NewNumericDate(time.Now()), // 签发时间
			NotBefore: jwt.NewNumericDate(time.Now()), // 生效时间
			Issuer:    "ChatRoom.Server.Models",       // 签发人
			Subject:   fmt.Sprintf("%d", id),
		},
	}

	token := jwt.NewWithClaims(jwt.SigningMethodHS256, claims)

	tokenString, err := token.SignedString(jwtSecret)
	if err != nil {
		return "", err
	}

	return tokenString, nil
}

func ParseToken(tokenString string) (*CustomClaims, error) {
	token, err := jwt.ParseWithClaims(tokenString, &CustomClaims{}, func(token *jwt.Token) (interface{}, error) {
		if _, ok := token.Method.(*jwt.SigningMethodHMAC); !ok {
			return nil, fmt.Errorf("unexpected signing method: %v", token.Header["alg"])
		}
		return jwtSecret, nil
	})

	if err != nil {
		return nil, err
	}

	if claims, ok := token.Claims.(*CustomClaims); ok && token.Valid {
		return claims, nil
	}

	return nil, fmt.Errorf("解析令牌出错")
}

func IsTokenExpired(tokenString string) bool {
	claims, err := ParseToken(tokenString)
	if err != nil {
		return true
	}

	return claims.ExpiresAt.Time.Before(time.Now())
}

func RefreshToken(oldTokenString string) (string, error) {
	claims, err := ParseToken(oldTokenString)
	if err != nil {
		return "", err
	}

	newToken, err := GenerateToken(claims.UserId)
	if err != nil {
		return "", err
	}

	return newToken, nil
}
