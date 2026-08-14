# Stage 1：认证与会话链路修复

> 状态：⏳ 待批准 | 预估工时：1~1.5 天

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

- [ ] 3.1 修复 `ValidateSession`：`IsValid = !IsTokenExpired(...)`，并补充 Go 单元测试
- [ ] 3.2 修复 `main.go` 的 `Serve` 循环为单次 `Serve` + 错误退出
- [ ] 3.3 C++ `HandleLogin` 成功后将 `rpcResponse.user_id()` 写入 `this->userId`
- [ ] 3.4 C++ 登录响应 `data` 增加 `user_id` 字段（与客户端取值对齐）
- [ ] 3.5 C++ `ValidateToken` 改为按 token 声明中的用户身份校验（可先按 token 解析出的 userId 校验，不再依赖会话内 userId）
- [ ] 3.6 客户端 `ChatRoomService` 保存登录/注册返回的 `session_token`，`CallAPIAsync` 统一携带
- [ ] 3.7 客户端注册不再向服务端发送 `user_id`（协议未定义，注册由数据库自增分配）
- [ ] 3.8 客户端登录响应解析补充 `user_id` 兜底（服务端返回后主源）
- [ ] 3.9 编写/更新测试：Go `ValidateSession` 有效/过期/伪造 token 用例；.NET 请求包 token 携带用例
- [ ] 3.10 文档同步：`docs/api.md` 登录/注册返回字段修正
- [ ] 3.11 客户端 `send_message`/`get_room_list` 不再携带已废弃的 `sender` 参数（身份由 token 决定）

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
