$ErrorActionPreference = 'Stop'

$root = 'D:\Works\ChatRoom'
$testDir = Join-Path $root '_test'
New-Item -ItemType Directory -Force -Path $testDir | Out-Null

# 服务端集成回归测试
# 前置条件：MySQL 运行中；C++ 服务端已编译（x64\Debug\ChatRoom.Server.exe）。
# 用法：powershell -ExecutionPolicy Bypass -File scripts\server_integration_test.ps1
# 说明：会临时生成 ChatRoom.Server\server.json（心跳2s/空闲10s）以加速验证，结束后自动清理。

# ---- 临时心跳配置（加速验证） ----
$serverJson = Join-Path $root 'ChatRoom.Server\server.json'
@'
{
  "heartbeat_interval_sec": 2,
  "idle_timeout_sec": 10,
  "max_message_length": 4096
}
'@ | Set-Content -Encoding UTF8 -Path $serverJson

# ---- 启动 Go 会话服务 ----
$goExe = Join-Path $root 'ChatRoom.Server.Models\Build\usersession.exe'
if (-not (Test-Path $goExe)) {
    Push-Location (Join-Path $root 'ChatRoom.Server.Models')
    go build -o 'Build\usersession.exe' .
    Pop-Location
}

$goProc = $null
$cppProc = $null
try {
    $goProc = Start-Process -FilePath $goExe -WorkingDirectory (Join-Path $root 'ChatRoom.Server.Models\Build') -WindowStyle Hidden -RedirectStandardOutput (Join-Path $testDir 'go.out.log') -RedirectStandardError (Join-Path $testDir 'go.err.log') -PassThru

    # ---- 启动 C++ 服务端 ----
    $cppExe = Join-Path $root 'x64\Debug\ChatRoom.Server.exe'
    $cppProc = Start-Process -FilePath $cppExe -WorkingDirectory (Join-Path $root 'ChatRoom.Server') -WindowStyle Hidden -RedirectStandardOutput (Join-Path $testDir 'server.out.log') -RedirectStandardError (Join-Path $testDir 'server.err.log') -PassThru

    # ---- 等待端口就绪 ----
    function Wait-Port([int]$port, [int]$timeoutSec = 20) {
        $deadline = (Get-Date).AddSeconds($timeoutSec)
        while ((Get-Date) -lt $deadline) {
            $ok = Test-NetConnection -ComputerName 127.0.0.1 -Port $port -WarningAction SilentlyContinue -InformationLevel Quiet
            if ($ok) { return $true }
            Start-Sleep -Milliseconds 500
        }
        return $false
    }

    if (-not (Wait-Port 50051)) { throw 'Go gRPC 服务未就绪' }
    if (-not (Wait-Port 12345)) { throw 'C++ 服务未就绪' }

    # ---- TCP 客户端辅助 ----
    function New-ChatConn {
        $tcp = [System.Net.Sockets.TcpClient]::new()
        $tcp.Connect('127.0.0.1', 12345)
        $stream = $tcp.GetStream()
        $stream.ReadTimeout = 15000
        $reader = [System.IO.StreamReader]::new($stream, [System.Text.Encoding]::UTF8, $false, 4096, $true)
        $writer = [System.IO.StreamWriter]::new($stream, [System.Text.Encoding]::UTF8, 4096, $true)
        [pscustomobject]@{ Tcp = $tcp; Stream = $stream; Reader = $reader; Writer = $writer }
    }

    function Send-Json($conn, [hashtable]$obj) {
        $json = $obj | ConvertTo-Json -Compress -Depth 10
        $conn.Writer.Write($json + "`n")
        $conn.Writer.Flush()
    }

    function Read-Message($conn) {
        $line = $conn.Reader.ReadLine()
        if ($null -eq $line) { throw '连接已关闭' }
        return $line | ConvertFrom-Json
    }

    function Read-Until($conn, [scriptblock]$pred, [int]$timeoutSec = 10) {
        $deadline = (Get-Date).AddSeconds($timeoutSec)
        while ((Get-Date) -lt $deadline) {
            $msg = Read-Message $conn
            if (& $pred $msg) { return $msg }
        }
        throw '等待超时'
    }

    function Read-Response($conn, $echo, [int]$timeoutSec = 10) {
        return Read-Until $conn { param($m) $m.echo -eq $echo } $timeoutSec
    }

    function Assert-True($cond, $msg) {
        if (-not $cond) { throw "断言失败: $msg" }
        Write-Output "[PASS] $msg"
    }

    $suffix = Get-Random -Minimum 10000 -Maximum 99999
    $nickA = "testerA_$suffix"
    $nickB = "testerB_$suffix"

    # ---- 1. 注册用户 A ----
    $connA = New-ChatConn
    Send-Json $connA @{ action = 'register'; params = @{ password = 'pass123'; nickname = $nickA }; token = ''; echo = 'e-reg-a' }
    $resp = Read-Response $connA 'e-reg-a'
    Assert-True ($resp.recode -eq 0) "注册成功 (nickname=$nickA)"
    Assert-True ([int]$resp.data.user_id -gt 0) "注册返回 user_id=$($resp.data.user_id)"
    Assert-True (-not [string]::IsNullOrEmpty($resp.data.session_token)) "注册返回 session_token"
    $userIdA = [int]$resp.data.user_id

    # ---- 2. 登录用户 A ----
    $connA2 = New-ChatConn
    Send-Json $connA2 @{ action = 'login'; params = @{ user_id = $userIdA; password = 'pass123' }; token = ''; echo = 'e-login-a' }
    $resp = Read-Response $connA2 'e-login-a'
    Assert-True ($resp.recode -eq 0) "登录成功"
    Assert-True ([int]$resp.data.user_id -eq $userIdA) "登录返回 user_id 与注册一致"
    Assert-True (-not [string]::IsNullOrEmpty($resp.data.session_token)) "登录返回 session_token"
    $tokenA = [string]$resp.data.session_token

    # ---- 3. 带 token 获取房间列表 ----
    Send-Json $connA2 @{ action = 'get_room_list'; params = @{}; token = $tokenA; echo = 'e-rooms-1' }
    $resp = Read-Response $connA2 'e-rooms-1'
    Assert-True ($resp.recode -eq 0) "get_room_list 成功"
    $systemRooms = @($resp.data.room_info_list | Where-Object { $_.room_name -in @('通用','游戏开黑','技术交流') })
    Assert-True ($systemRooms.Count -eq 3) "系统默认房间共3个"

    # ---- 4. 伪造 token 被拒 ----
    Send-Json $connA2 @{ action = 'get_room_list'; params = @{}; token = 'forged.token.here'; echo = 'e-forged' }
    $resp = Read-Response $connA2 'e-forged'
    Assert-True ($resp.recode -eq 502) "伪造 token 返回 502"

    # ---- 5. 创建房间 + 重名 409 ----
    $roomName = "集成测试房_$suffix"
    Send-Json $connA2 @{ action = 'create_room'; params = @{ room_name = $roomName }; token = $tokenA; echo = 'e-create-1' }
    $resp = Read-Response $connA2 'e-create-1'
    Assert-True ($resp.recode -eq 0) "create_room 成功"
    Assert-True ([int]$resp.data.room_id -ge 0) "create_room 返回 room_id=$($resp.data.room_id)"
    $createdRoomId = [int]$resp.data.room_id

    Send-Json $connA2 @{ action = 'create_room'; params = @{ room_name = $roomName }; token = $tokenA; echo = 'e-create-2' }
    $resp = Read-Response $connA2 'e-create-2'
    Assert-True ($resp.recode -eq 409) "重名房间返回 409"

    # ---- 6. 空房间自动关闭（创建者离开后） ----
    Send-Json $connA2 @{ action = 'join_room'; params = @{ room_id = 0 }; token = $tokenA; echo = 'e-join-sys' }
    $resp = Read-Response $connA2 'e-join-sys'
    Assert-True ($resp.recode -eq 0) "加入系统房间成功"

    Send-Json $connA2 @{ action = 'get_room_list'; params = @{}; token = $tokenA; echo = 'e-rooms-2' }
    $resp = Read-Response $connA2 'e-rooms-2'
    $closed = -not (@($resp.data.room_info_list | Where-Object { $_.room_id -eq $createdRoomId }).Count -gt 0)
    Assert-True $closed "用户创建的房间空置后自动关闭并从列表消失"
    $sysStillThere = (@($resp.data.room_info_list | Where-Object { $_.room_name -eq '通用' }).Count -eq 1)
    Assert-True $sysStillThere "系统默认房间仍然存在"

    # ---- 7. 发送消息不回显给发送者 ----
    Send-Json $connA2 @{ action = 'send_message'; params = @{ message = 'hello-sys-room' }; token = $tokenA; echo = 'e-msg-1' }
    $resp = Read-Response $connA2 'e-msg-1'
    Assert-True ($resp.recode -eq 0) "send_message 成功"
    $connA2.Stream.ReadTimeout = 1500
    $echoed = $false
    try {
        while ($true) {
            $m = Read-Message $connA2
            if ($m.post_type -eq 'message') { $echoed = $true }
        }
    } catch { }
    Assert-True (-not $echoed) "发送者未收到自己消息的事件广播"
    $connA2.Stream.ReadTimeout = 15000

    # ---- 8. 双客户端：B 建房，A 加入并互发消息 ----
    $connB = New-ChatConn
    Send-Json $connB @{ action = 'register'; params = @{ password = 'pass123'; nickname = $nickB }; token = ''; echo = 'e-reg-b' }
    $resp = Read-Response $connB 'e-reg-b'
    Assert-True ($resp.recode -eq 0) "注册用户 B 成功"
    $userIdB = [int]$resp.data.user_id
    $tokenB = [string]$resp.data.session_token

    $roomName2 = "双人测试房_$suffix"
    Send-Json $connB @{ action = 'create_room'; params = @{ room_name = $roomName2 }; token = $tokenB; echo = 'e-create-b' }
    $resp = Read-Response $connB 'e-create-b'
    Assert-True ($resp.recode -eq 0) "用户 B 创建房间成功"
    $room2Id = [int]$resp.data.room_id

    Send-Json $connA2 @{ action = 'join_room'; params = @{ room_id = $room2Id }; token = $tokenA; echo = 'e-join-b' }
    $resp = Read-Response $connA2 'e-join-b'
    Assert-True ($resp.recode -eq 0) "用户 A 加入 B 的房间成功"

    $notice = Read-Until $connB { param($m) $m.post_type -eq 'notice' -and $m.data.notice_type -eq 'join_room' -and [int]$m.data.user_id -eq $userIdA } 8
    Assert-True ($notice.data.nickname -eq $nickA) "B 收到 join_room 通知，含昵称"

    Send-Json $connA2 @{ action = 'send_message'; params = @{ message = 'hello from A' }; token = $tokenA; echo = 'e-msg-2' }
    $resp = Read-Response $connA2 'e-msg-2'
    Assert-True ($resp.recode -eq 0) "A 发送消息成功"

    $msgEvent = Read-Until $connB { param($m) $m.post_type -eq 'message' -and $m.data.message -eq 'hello from A' -and [int]$m.data.sender -eq $userIdA } 8
    Assert-True ($msgEvent.data.nickname -eq $nickA) "B 收到 A 的消息事件，sender/nickname 正确"

    # ---- 9. 心跳事件 ----
    $hb = Read-Until $connA2 { param($m) $m.post_type -eq 'heartbeat' } 8
    Assert-True ([int64]$hb.data.time -gt 0) "收到 heartbeat 事件"

    # ---- 10. 房间在最后一人离开后关闭 ----
    $connA2.Tcp.Close()
    Start-Sleep -Seconds 1
    Send-Json $connB @{ action = 'get_room_list'; params = @{}; token = $tokenB; echo = 'e-rooms-b1' }
    $resp = Read-Response $connB 'e-rooms-b1'
    $stillExists = @($resp.data.room_info_list | Where-Object { $_.room_id -eq $room2Id }).Count -eq 1
    Assert-True $stillExists "A 离开后房间仍存在（B 还在）"

    $connB.Tcp.Close()
    Start-Sleep -Seconds 1

    $connC = New-ChatConn
    Send-Json $connC @{ action = 'login'; params = @{ user_id = $userIdA; password = 'pass123' }; token = ''; echo = 'e-login-c' }
    $resp = Read-Response $connC 'e-login-c'
    $tokenC = [string]$resp.data.session_token
    Send-Json $connC @{ action = 'get_room_list'; params = @{}; token = $tokenC; echo = 'e-rooms-c' }
    $resp = Read-Response $connC 'e-rooms-c'
    $roomClosed = -not (@($resp.data.room_info_list | Where-Object { $_.room_id -eq $room2Id }).Count -gt 0)
    Assert-True $roomClosed "最后一人离开后用户房间自动关闭"
    $sysCount = @($resp.data.room_info_list | Where-Object { $_.room_name -in @('通用','游戏开黑','技术交流') }).Count
    Assert-True ($sysCount -eq 3) "系统默认房间始终存在"

    # ---- 11. 空闲超时断连 ----
    $connD = New-ChatConn
    Send-Json $connD @{ action = 'login'; params = @{ user_id = $userIdA; password = 'pass123' }; token = ''; echo = 'e-login-d' }
    $resp = Read-Response $connD 'e-login-d'
    Assert-True ($resp.recode -eq 0) "用户 D 登录成功（用于空闲超时测试）"
    $connD.Stream.ReadTimeout = 20000
    $closedByTimeout = $false
    try {
        while ($true) {
            $line = $connD.Reader.ReadLine()
            if ($null -eq $line) { $closedByTimeout = $true; break }
        }
    } catch { }
    Assert-True $closedByTimeout "空闲超时后连接被服务端断开"

    # ---- 清理连接 ----
    $connA.Tcp.Close()
    $connC.Tcp.Close()
    try { $connD.Tcp.Close() } catch { }

    Write-Output 'ALL_TESTS_PASSED'
}
finally {
    if ($cppProc) { Stop-Process -Id $cppProc.Id -Force -ErrorAction SilentlyContinue }
    if ($goProc) { Stop-Process -Id $goProc.Id -Force -ErrorAction SilentlyContinue }
    Remove-Item -LiteralPath $serverJson -Force -ErrorAction SilentlyContinue
}
