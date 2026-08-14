package Database

import (
	"database/sql"
	"errors"
	"regexp"
	"testing"

	"github.com/DATA-DOG/go-sqlmock"
	"github.com/go-sql-driver/mysql"
	"golang.org/x/crypto/bcrypt"
)

func TestGetUserById_Success(t *testing.T) {
	dbMock, mock, err := sqlmock.New()
	if err != nil {
		t.Fatalf("failed to create mock db: %v", err)
	}
	defer dbMock.Close()

	origDB := db
	db = dbMock
	defer func() { db = origDB }()

	rows := sqlmock.NewRows([]string{"id", "password", "nickname"}).
		AddRow(1, "secret123", "Alice")
	mock.ExpectQuery(regexp.QuoteMeta("SELECT id, `password`, nickname\nFROM `user`\nWHERE id=?")).
		WithArgs(1).
		WillReturnRows(rows)

	user := &User{}
	err = user.GetUserById(1)
	if err != nil {
		t.Errorf("unexpected error: %v", err)
	}
	if user.Id != 1 {
		t.Errorf("expected Id=1, got %d", user.Id)
	}
	if user.Password != "secret123" {
		t.Errorf("expected Password=secret123, got %s", user.Password)
	}
	if user.Nickname != "Alice" {
		t.Errorf("expected Nickname=Alice, got %s", user.Nickname)
	}

	if err := mock.ExpectationsWereMet(); err != nil {
		t.Errorf("unmet expectations: %v", err)
	}
}

func TestGetUserById_NotFound(t *testing.T) {
	dbMock, mock, err := sqlmock.New()
	if err != nil {
		t.Fatalf("failed to create mock db: %v", err)
	}
	defer dbMock.Close()

	origDB := db
	db = dbMock
	defer func() { db = origDB }()

	mock.ExpectQuery(regexp.QuoteMeta("SELECT id, `password`, nickname\nFROM `user`\nWHERE id=?")).
		WithArgs(999).
		WillReturnError(sql.ErrNoRows)

	user := &User{}
	err = user.GetUserById(999)
	if err == nil {
		t.Error("expected error for non-existent user, got nil")
	}

	if err := mock.ExpectationsWereMet(); err != nil {
		t.Errorf("unmet expectations: %v", err)
	}
}

func TestCreateUser_Success(t *testing.T) {
	dbMock, mock, err := sqlmock.New()
	if err != nil {
		t.Fatalf("failed to create mock db: %v", err)
	}
	defer dbMock.Close()

	origDB := db
	db = dbMock
	defer func() { db = origDB }()

	mock.ExpectExec(regexp.QuoteMeta("INSERT INTO user (password, nickname) VALUES (?, ?)")).
		WithArgs(sqlmock.AnyArg(), "Bob").
		WillReturnResult(sqlmock.NewResult(5, 1))

	user := &User{}
	err = user.CreateUser("pwd123", "Bob")
	if err != nil {
		t.Errorf("unexpected error: %v", err)
	}
	if user.Id != 5 {
		t.Errorf("expected Id=5, got %d", user.Id)
	}
	if bcrypt.CompareHashAndPassword([]byte(user.Password), []byte("pwd123")) != nil {
		t.Errorf("存储的密码应为 pwd123 的 bcrypt 哈希，实际: %s", user.Password)
	}
	if user.Nickname != "Bob" {
		t.Errorf("expected Nickname=Bob, got %s", user.Nickname)
	}

	if err := mock.ExpectationsWereMet(); err != nil {
		t.Errorf("unmet expectations: %v", err)
	}
}

func TestCreateUser_Error(t *testing.T) {
	dbMock, mock, err := sqlmock.New()
	if err != nil {
		t.Fatalf("failed to create mock db: %v", err)
	}
	defer dbMock.Close()

	origDB := db
	db = dbMock
	defer func() { db = origDB }()

	mock.ExpectExec(regexp.QuoteMeta("INSERT INTO user (password, nickname) VALUES (?, ?)")).
		WithArgs(sqlmock.AnyArg(), "Bob").
		WillReturnError(sql.ErrConnDone)

	user := &User{}
	err = user.CreateUser("pwd123", "Bob")
	if err == nil {
		t.Error("expected error on insert failure, got nil")
	}

	if err := mock.ExpectationsWereMet(); err != nil {
		t.Errorf("unmet expectations: %v", err)
	}
}

func TestCreateUser_DuplicateNickname(t *testing.T) {
	dbMock, mock, err := sqlmock.New()
	if err != nil {
		t.Fatalf("failed to create mock db: %v", err)
	}
	defer dbMock.Close()

	origDB := db
	db = dbMock
	defer func() { db = origDB }()

	mock.ExpectExec(regexp.QuoteMeta("INSERT INTO user (password, nickname) VALUES (?, ?)")).
		WithArgs(sqlmock.AnyArg(), "Bob").
		WillReturnError(&mysql.MySQLError{Number: 1062, Message: "Duplicate entry 'Bob' for key 'uk_nickname'"})

	user := &User{}
	err = user.CreateUser("pwd123", "Bob")
	if !errors.Is(err, ErrNicknameTaken) {
		t.Errorf("重复昵称应返回 ErrNicknameTaken，实际: %v", err)
	}

	if err := mock.ExpectationsWereMet(); err != nil {
		t.Errorf("unmet expectations: %v", err)
	}
}

func TestVerifyPassword(t *testing.T) {
	hash, _ := bcrypt.GenerateFromPassword([]byte("pwd123"), bcrypt.DefaultCost)
	user := &User{Password: string(hash)}

	if !user.VerifyPassword("pwd123") {
		t.Error("正确密码应通过校验")
	}
	if user.VerifyPassword("wrong") {
		t.Error("错误密码不应通过校验")
	}
}
