package main

import (
	"ChatRoom.Server.Models/Protos/gRPCUserSession"
	"google.golang.org/grpc"
	"log"
	"net"
)

func main() {
	lis, err := net.Listen("tcp", ":12345")
	if err != nil {
		panic(err)
	}

	server := grpc.NewServer()

	gRPCUserSession.RegisterUserSessionServiceServer(server, &gRPCUserSession.Server{})

	log.Printf("服务器监听 %v", lis.Addr())

	for err := server.Serve(lis); true; err = server.Serve(lis) {
		if err != nil {
			panic(err)
		} else {
			log.Println("gRPC")
		}
	}
}
