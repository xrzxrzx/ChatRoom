package main

import (
	"ChatRoom.Server.Models/Common"
	"ChatRoom.Server.Models/Database"
	"ChatRoom.Server.Models/JWT"
	"ChatRoom.Server.Models/Protos/gRPCUserSession"
	"fmt"
	"google.golang.org/grpc"
	"log"
	"net"
)

func main() {
	var serviceConfig Common.ServiceConfig
	serviceConfig.LoadConfigFile("config.yaml")

	if err := Database.InitDB(serviceConfig.MySQLDSN()); err != nil {
		log.Fatalf("数据库初始化失败: %v", err)
	}

	JWT.SetSecret([]byte(serviceConfig.JwtSecret))
	if serviceConfig.JwtSecret == "" || serviceConfig.JwtSecret == "your-secret-key-change-this" {
		log.Println("警告: 正在使用默认/占位 JWT 密钥，生产环境必须通过 config.yaml 配置 jwt_secret")
	}

	lis, err := net.Listen("tcp", fmt.Sprintf(":%d", serviceConfig.Port))
	if err != nil {
		panic(err)
	}

	server := grpc.NewServer()

	gRPCUserSession.RegisterUserSessionServiceServer(server, &gRPCUserSession.Server{})

	log.Printf("服务器监听 %v", lis.Addr())

	if err := server.Serve(lis); err != nil {
		panic(err)
	}
}
