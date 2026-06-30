package Database

import (
	"database/sql"
	"regexp"
	"testing"

	"github.com/DATA-DOG/go-sqlmock"
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
		WithArgs("pwd123", "Bob").
		WillReturnResult(sqlmock.NewResult(5, 1))

	user := &User{}
	err = user.CreateUser("pwd123", "Bob")
	if err != nil {
		t.Errorf("unexpected error: %v", err)
	}
	if user.Id != 5 {
		t.Errorf("expected Id=5, got %d", user.Id)
	}
	if user.Password != "pwd123" {
		t.Errorf("expected Password=pwd123, got %s", user.Password)
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
		WithArgs("pwd123", "Bob").
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
