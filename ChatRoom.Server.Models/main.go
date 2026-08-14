package main

import (
	"ChatRoom.Server.Models/Common"
	"ChatRoom.Server.Models/Protos/gRPCUserSession"
	"fmt"
	"google.golang.org/grpc"
	"log"
	"net"
)

func main() {
	var serviceConfig Common.ServiceConfig
	serviceConfig.LoadConfigFile("config.yaml")

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
