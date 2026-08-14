# Stage 1：认证与会话链路修复

> 状态：✅ 已完成 | 预估工时：1~1.5 天

## 一、阶段目标与范围边界

**目标**：修复认证与会话链路中的关键缺陷（B1/B3/B5/B9），使"注册 → 登录 → 进房 → 收发消息"全链路真实可用，且 JWT 校验真正生效。

**范围**：
- Go 会话服务：`ValidateSession` 逻辑、`main.go` 循环、注册/登录响应语义。
- C++ 服务端：登录后写入 `userId`、登录响应返回 `user_id`、token 校验使用 token 中的用户身份。
- .NET 客户端：保存 `session_token` 并在所有 API 调用中携带；注册流程对齐协议。

**范围外**：会话生命周期管理（Stage 2）、密码哈希（Stage 4）、客户端事件分发（Stage 3）。

## 二、前置条件

- 无（本阶段不依赖其他阶段）。

## 三、详细任务清单

- [x] 3.1 修复 `ValidateSession`：`IsValid = !IsTokenExpired(...)`，并补充 Go 单元测试
- [x] 3.2 修复 `main.go` 的 `Serve` 循环为单次 `Serve` + 错误退出
- [x] 3.3 C++ `HandleLogin` 成功后将 `rpcResponse.user_id()` 写入 `this->userId`
- [x] 3.4 C++ 登录响应 `data` 增加 `user_id` 字段（含 proto `LoginResponse.user_id=4` 与代码再生成）
- [x] 3.5 C++ `ValidateToken` 改为按 token 声明中的用户身份校验（`GetSessionInfo` 校验，不再依赖会话内 userId）
- [x] 3.6 客户端 `ChatRoomService` 保存登录/注册返回的 `session_token`，`CallAPIAsync` 统一携带
- [x] 3.7 客户端注册不再向服务端发送 `user_id`（协议未定义，注册由数据库自增分配）
- [x] 3.8 客户端登录响应解析补充 `user_id` 兜底（服务端返回后主源）
- [x] 3.9 编写/更新测试：Go `ValidateSession` 有效/过期/伪造 token 用例；.NET 请求包 token 携带用例
- [x] 3.10 文档同步：`docs/api.md` 登录/注册返回字段修正（已提前完成）
- [x] 3.11 客户端 `send_message`/`get_room_list` 不再携带已废弃的 `sender` 参数（身份由 token 决定）

## 四、验收标准

- [ ] `go test ./...` 通过，覆盖 `ValidateSession` 有效、过期、伪造三种情况
- [ ] `dotnet build` 客户端 Core 通过；`ChatClient.yaml` 无变化时默认配置可读取
- [ ] 集成验证：登录返回 `user_id`；客户端后续请求均带 token；伪造 token 请求被服务端拒绝（返回 502）
- [ ] `docs/api.md` 登录、注册字段与实现一致

## 五、风险与应对措施

| 风险 | 应对 |
| ---- | ---- |
| 修复 B1 后客户端旧逻辑立即失效 | Stage 1 内客户端与服务端同步修改，统一验证 |
| 客户端与服务端字段不齐 | 以服务端响应为准，两端同步改并在验收中逐字段核对 |
| C++ 环境不可用 | 提供代码级验证 + Go/.NET 测试覆盖协议行为 |

## 六、实现记录与验收证据

### 代码变更

- Go：`ValidateSession` 逻辑反转修复；`main.go` 简化 `Serve`；JWT 新增 `GenerateTokenWithExpiry`（测试/刷新用）；新增 `jwt_test.go`、`gRPCUserSession_test.go`。
- 协议：`Protos/gRPCUserSession.proto` 的 `LoginResponse` 新增 `user_id=4`，Go/C++ 代码已用 protoc 重新生成（生成物在 `.gitignore` 中，由 `Compile Protos.bat` 产出）。
- C++：`UserSession::HandleLogin` 写入 `userId` 并返回 `user_id`；`ValidateToken` 改用 `GetSessionInfo` 按 token 身份校验。
- 客户端：`ChatRoomService` 保存/携带 `session_token`；注册签名去掉 `user_id`；`send_message` 去掉 `sender`；新增 `ChatRoom.Client.Core.Tests` 测试项目。

### 验收证据

- [x] `go test ./...` 通过（Database/JWT/gRPCUserSession 全部 ok，新增 7 个用例）
- [x] `dotnet build ChatRoom.Client.Core` 通过（0 警告 0 错误）
- [x] `dotnet build ChatRoom.Client (x64)` 通过（0 警告 0 错误）
- [x] `dotnet test ChatRoom.Client.Core.Tests` 通过（4/4）
- [x] C++ `ChatRoom.Server.vcxproj`（Debug|x64）编译通过，产出 `x64\Debug\ChatRoom.Server.exe`
- [x] `docs/api.md` 登录/注册字段与实现一致
- [ ] 端到端联调（登录返回 user_id、伪造 token 被拒）：**待 Stage 2 会话生命周期修复后执行**（当前 B2 会导致连接在 accept 后被立即关闭，属 Stage 2 范围）

### 已知问题（转入后续阶段）

- B2 会话生命周期：全链路联调依赖 Stage 2 修复。
- `logout` 服务端接口尚未实现（Stage 2 任务 3.15）。
