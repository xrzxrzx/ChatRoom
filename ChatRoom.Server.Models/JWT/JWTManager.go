package JWT

import (
	"fmt"
	"github.com/golang-jwt/jwt/v5"
	"time"
)

type CustomClaims struct {
	UserId int32
	jwt.RegisteredClaims
}

// JWT秘钥
var jwtSecret = []byte("your-secret-key-change-this")

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
