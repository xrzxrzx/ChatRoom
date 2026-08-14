# Stage 2：服务端稳定性完善

> 状态：✅ 已完成 | 预估工时：1~2.5 天

## 一、阶段目标与范围边界

**目标**：修复服务端关键稳定性问题（B2/B8/B10/B11 中的服务端部分），补充会话持有、离开通知、房间人数事件、心跳保活与配置化，使服务端可长时间稳定运行。

**范围**：
- C++ 服务端：会话生命周期管理、断开清理、发送者过滤、房间初始化清理、心跳、配置化（端口/gRPC 地址）。
- Go 服务：`main.go` 修正（已在 Stage 1）、Logout 语义完善（Stage 4 配合）。

**范围外**：客户端重连（Stage 3）、安全加固（Stage 4）。

## 二、前置条件

- Stage 1 完成并验收（认证链路正确，token 校验生效）。

## 三、详细任务清单

- [x] 3.1 会话持有：`ChatServerService` 增加 `sessions` 容器保存 `shared_ptr<UserSession>`，`do_accept` 将新会话加入
- [x] 3.2 断开清理：读错误/EOF 时从容器移除会话、退出所在房间、广播 `leave_room` 通知
- [x] 3.3 发送者过滤：`HandleSendMessage` 广播时跳过发送者本人（避免与客户端本地回显重复）
- [x] 3.4 房间初始化清理：删除构造函数中写死的 5 个房间与 `Main.cpp` 中不可达的 3 个房间，改为配置/参数化
- [x] 3.5 `AddChatRoom` 移除 `static int id`，改用成员计数器
- [x] 3.6 心跳：服务端定期向客户端发送 heartbeat 事件，空闲超时无数据则断开
- [x] 3.7 房间人数事件：加入/离开房间时广播 `update` 事件（`update_type=room_list`，全量列表）
- [x] 3.8 服务端配置化：端口、gRPC 地址、心跳间隔等从 `server.json` 读取（提供默认值）
- [x] 3.9 消息校验：限制消息长度（默认 4096 字符），非法 JSON/超长消息返回 400 并断开
- [x] 3.10 服务端日志完善：连接建立/断开、房间进出、会话移除均有结构化日志
- [x] 3.11 新增 `create_room` API：校验房间名（非空、≤32 字符、不重名），创建房间并将创建者自动加入
- [x] 3.12 房间生命周期：`ChatRoom` 增加 `isSystem` 标识；用户创建的房间在最后一名成员离开（含断线、退出、换房）后自动移除
- [x] 3.13 创建/关闭房间时向在线客户端广播 `update(room_list)`（含房间新增与移除）
- [x] 3.14 系统默认房间标记为常驻，永不自动关闭
- [x] 3.15 实现 `logout` API：按 `session_token` 注销并清理会话状态（配合 Stage 4 token 黑名单）

## 四、验收标准

- [ ] 多客户端连接后服务端可稳定收发消息，断开后会话被正确清理（无泄漏）
- [ ] 发送消息不会在本人界面重复显示
- [ ] 客户端加入/离开房间，其余客户端房间人数实时更新（收到 update 事件）
- [ ] 心跳：客户端空闲时收到 heartbeat，服务器能检测死连接并清理
- [ ] 端口与 gRPC 地址可通过配置文件修改
- [ ] 服务端日志包含连接/断开/进出房记录
- [ ] 客户端可创建房间并自动加入；房间列表出现新房间
- [ ] 用户创建的房间最后一人离开后自动关闭并从列表消失；系统默认房间即使无人也不关闭

## 五、风险与应对措施

| 风险 | 应对 |
| ---- | ---- |
| 异步会话生命周期改动易引入悬垂指针 | 统一使用 `shared_ptr` 容器 + 读错误回调清理，避免裸指针跨回调 |
| 心跳与客户端版本兼容 | 客户端 Stage 3 同步实现心跳响应；心跳事件兼容旧客户端（忽略即可） |
| C++ 构建依赖本地 vcpkg | 改动控制在小范围，提供编译说明；必要时用 CI 的 windows-latest 验证 |

## 六、实现记录与验收证据

### 代码变更

- `ChatServerService`：新增 `sessions` 容器持有会话、心跳定时器（heartbeat + 空闲超时）、`CreateChatRoom/RemoveChatRoom/BroadcastRoomListUpdate/RequestRemoveSession`；系统房间由 `server.json` 的 `rooms` 配置生成；`do_accept` 不再丢失会话。
- `UserSession`：新增 `Close/CloseAfterFlush/LeaveCurrentRoom` 生命周期管理；断开时广播 `leave_room` 与房间列表更新；`send_message` 广播排除发送者本人；新增 `create_room`、`logout` 处理；消息长度校验与非法 JSON 处理；活跃时间跟踪。
- `ChatRoom`：新增 `isSystem` 标识与广播排除参数。
- `ServerConfig`：新增配置加载（端口、gRPC 地址、心跳/超时/消息长度、系统房间列表），默认 `127.0.0.1:50051`（避开 `localhost` 首连 10s+ 延迟）。
- `UserSessionServiceClient`：gRPC 地址改为构造注入。
- `Main.cpp`：移除死代码房间，改为加载配置。

### 过程中发现并修复的额外缺陷

| # | 问题 | 修复 |
| - | ---- | ---- |
| B2 根因 | boost.di 对 `std::shared_ptr<UserSession>` 的 deduced 作用域将其缓存为**注入器级单例**，导致所有连接复用同一会话对象（连接互相串线/状态错乱） | 会话工厂改为显式 `std::make_shared<UserSession>(...)`，每次 accept 创建全新会话 |
| B16 | `RequestBag` 构造函数从未解析 `token` 字段，服务端收到的 token 恒为空（被 Stage 1 前的 ValidateSession 反转掩盖） | 补 `token = jsonMessage.value("token", "")` |
| B17 | gRPC 默认地址 `localhost:50051` 首连延迟约 11s（IPv4/IPv6 解析回退），会阻塞单线程事件循环 | 默认改为 `127.0.0.1:50051`（首连 3.8ms） |

### 验收证据

- [x] C++ 服务端 Debug|x64 编译通过，产出 `x64\Debug\ChatRoom.Server.exe`
- [x] 集成回归测试全绿（`scripts/server_integration_test.ps1`，30 项断言）：
  - 注册/登录返回 user_id 与 session_token；带 token 的 get_room_list 成功
  - 伪造 token 返回 502
  - create_room 成功、重名返回 409
  - 用户房间空置自动关闭并从列表消失；系统默认房间始终存在
  - 发送消息不向发送者回显事件；双客户端互发消息 sender/nickname 正确
  - 收到 join_room 通知（含昵称）与 heartbeat 事件
  - 最后一人离开后房间关闭；空闲超时后连接被服务端断开
- [x] `docs/api.md`、`docs/event.md` 与实现一致（create_room/heartbeat/leave_room/update 全量列表）

### 已知限制

- gRPC 调用仍为同步调用（在 io_context 线程上执行），当前规模下延迟毫秒级；若未来并发量大，可改为异步/线程池。
- `logout` 依赖 Go 侧 token 黑名单（Stage 4 实现）才能真正使 token 失效。
