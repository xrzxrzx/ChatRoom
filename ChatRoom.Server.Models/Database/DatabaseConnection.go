package Database

import (
	"database/sql"
	_ "github.com/go-sql-driver/mysql"
	"log"
	"time"
)

var db *sql.DB

func init() {
	err := InitDB()
	if err != nil {
		log.Fatal(err)
	}
}

func InitDB() error {
	var err error

	dsn := "root:612278@tcp(127.0.0.1:3306)/chat_service?charset=utf8mb4&parseTime=True&loc=Local"

	db, err := sql.Open("mysql", dsn)
	if err != nil {
		return err
	}

	db.SetMaxOpenConns(25)
	db.SetMaxIdleConns(25)
	db.SetConnMaxLifetime(time.Minute * 5)

	return db.Ping()
}
