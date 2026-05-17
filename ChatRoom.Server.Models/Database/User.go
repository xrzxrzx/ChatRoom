package Database

type User struct {
	Id       int64
	Password string
	Nickname string
}

func (user *User) GetUserById(id string) error {
	query := "SELECT id, `password`, nickname\nFROM `user`\nWHERE id=?"
	err := db.QueryRow(query, id).
		Scan(&user.Id, &user.Password, &user.Nickname)
	if err != nil {
		return err
	}

	return nil
}

func (user *User) CreateUser(password string, nickname string) error {
	query := "INSERT INTO user (password, nickname) VALUES (?, ?)"
	result, err := db.Exec(query, password, nickname)
	if err != nil {
		return err
	}

	user.Id, _ = result.LastInsertId()
	user.Password = password
	user.Nickname = nickname

	return nil
}
