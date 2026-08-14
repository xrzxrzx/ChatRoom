package Common

import (
	"fmt"
	"gopkg.in/yaml.v3"
	"path/filepath"
)
import "os"

type ServiceConfig struct {
	Port      int32          `yaml:"port"`
	JwtSecret string         `yaml:"jwt_secret"`
	Database  DatabaseConfig `yaml:"db"`
}

type DatabaseConfig struct {
	Host     string `yaml:"host"`
	Port     int    `yaml:"port"`
	User     string `yaml:"user"`
	Password string `yaml:"password"`
	Name     string `yaml:"database"`
}

func (config *ServiceConfig) LoadConfigFile(configFile string) {
	exe, err := os.Executable()
	if err != nil {
		panic(err)
	}

	exeDir := filepath.Dir(exe)

	//依次尝试可执行文件目录与当前工作目录
	candidates := []string{
		filepath.Join(exeDir, configFile),
		filepath.Join(".", configFile),
	}

	var data []byte
	var loadedPath string
	for _, candidate := range candidates {
		data, err = os.ReadFile(candidate)
		if err == nil {
			loadedPath = candidate
			break
		}
	}
	if loadedPath == "" {
		panic(fmt.Sprintf("未找到配置文件 %s", configFile))
	}

	err = yaml.Unmarshal(data, config)
	if err != nil {
		panic(err)
	}

	fmt.Printf("已读取配置文件: %s\n", loadedPath)
}

// MySQLDSN 根据配置拼装数据库连接串
func (config *ServiceConfig) MySQLDSN() string {
	db := config.Database
	host := db.Host
	if host == "" {
		host = "127.0.0.1"
	}
	port := db.Port
	if port == 0 {
		port = 3306
	}
	user := db.User
	if user == "" {
		user = "root"
	}
	return fmt.Sprintf("%s:%s@tcp(%s:%d)/%s?charset=utf8mb4&parseTime=True&loc=Local",
		user, db.Password, host, port, db.Name)
}
