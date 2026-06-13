package Common

import (
	"fmt"
	"gopkg.in/yaml.v3"
	"path/filepath"
)
import "os"

type ServiceConfig struct {
	Port int32 `yaml:"port"`
}

func (config *ServiceConfig) LoadConfigFile(configFile string) {
	exe, err := os.Executable()
	if err != nil {
		panic(err)
	}

	exeDir := filepath.Dir(exe)

	data, err := os.ReadFile(filepath.Join(exeDir, configFile))
	if err != nil {
		panic(err)
	}

	err = yaml.Unmarshal(data, config)
	if err != nil {
		panic(err)
	}

	fmt.Println("已读取配置文件")
}
