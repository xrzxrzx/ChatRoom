
@echo off
setlocal enabledelayedexpansion

:input_path
echo 请输入.proto文件路径(或拖放文件到窗口):
set /p proto_file=

:: 若未输入路径，默认使用当前目录
if "!proto_file!"=="" (
    set proto_file=%cd%
    echo 未输入路径，默认使用当前目录: "!proto_file!"
)

:: 去除路径两端的引号（如果有）
set proto_file=!proto_file:"=!

:: 判断输入是文件还是目录
set is_dir=0
if exist "!proto_file!\" set is_dir=1

if !is_dir!==0 (
    :: 检查文件是否存在
    if not exist "!proto_file!" (
        echo 错误：文件 "!proto_file!" 不存在
        goto input_path
    )

    :: 提取文件所在目录
    for %%i in ("!proto_file!") do set proto_dir=%%~dpi
) else (
    set proto_dir=!proto_file!
)

:: 规范化目录并设置统一输出目录
for %%i in ("!proto_dir!") do set proto_dir=%%~fi
set out_dir=!proto_dir!\gRPCOut
if not exist "!out_dir!" mkdir "!out_dir!"

:: gRPC C++ 插件路径
set grpc_plugin=E:\App\vcpkg\packages\grpc_x64-windows\tools\grpc\grpc_cpp_plugin.exe

:: 检查插件是否存在
if not exist "!grpc_plugin!" (
    echo 错误：未找到 gRPC 插件 "!grpc_plugin!"
    pause
    exit /b 1
)

:: 运行protoc命令（生成 C++ 与 gRPC C++ 代码）
if !is_dir!==0 (
    echo 正在编译为 C++: !proto_file!
    echo 输出目录: !out_dir!
    protoc -I "!proto_dir!" --cpp_out="!out_dir!" --grpc_out="!out_dir!" --plugin=protoc-gen-grpc="!grpc_plugin!" "!proto_file!"
) else (
    if not exist "!proto_dir!\*.proto" (
        echo 错误：目录 "!proto_dir!" 下未找到 .proto 文件
        pause
        exit /b 1
    )

    echo 输出目录: !out_dir!
    for %%f in ("!proto_dir!\*.proto") do (
        echo 正在编译为 C++: %%~ff
        protoc -I "!proto_dir!" --cpp_out="!out_dir!" --grpc_out="!out_dir!" --plugin=protoc-gen-grpc="!grpc_plugin!" "%%~ff"
        if errorlevel 1 (
            echo 错误：protoc命令执行失败，文件: %%~ff
            pause
            exit /b 1
        )
    )
)

:: 检查命令执行结果
if errorlevel 1 (
    echo 错误：protoc命令执行失败
    pause
    exit /b 1
) else (
    echo 编译成功完成！
)

pause
