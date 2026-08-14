package Database

import (
	"errors"

	"github.com/go-sql-driver/mysql"
	"golang.org/x/crypto/bcrypt"
)

var ErrNicknameTaken = errors.New("昵称已被占用")

type User struct {
	Id       int32
	Password string // 密码哈希
	Nickname string
}

func (user *User) GetUserById(id int32) error {
	query := "SELECT id, `password`, nickname\nFROM `user`\nWHERE id=?"
	err := db.QueryRow(query, id).
		Scan(&user.Id, &user.Password, &user.Nickname)
	if err != nil {
		return err
	}

	return nil
}

func (user *User) CreateUser(password string, nickname string) error {
	hash, err := bcrypt.GenerateFromPassword([]byte(password), bcrypt.DefaultCost)
	if err != nil {
		return err
	}

	query := "INSERT INTO user (password, nickname) VALUES (?, ?)"
	result, err := db.Exec(query, string(hash), nickname)
	if err != nil {
		var mysqlErr *mysql.MySQLError
		if errors.As(err, &mysqlErr) && mysqlErr.Number == 1062 {
			return ErrNicknameTaken
		}
		return err
	}

	id, _ := result.LastInsertId()
	user.Id = int32(id)
	user.Password = string(hash)
	user.Nickname = nickname

	return nil
}

// VerifyPassword 校验明文密码是否与存储的哈希匹配
func (user *User) VerifyPassword(password string) bool {
	return bcrypt.CompareHashAndPassword([]byte(user.Password), []byte(password)) == nil
}
