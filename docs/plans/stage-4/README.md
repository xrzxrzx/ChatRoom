# Stage 4：安全与配置加固

> 状态：✅ 已完成 | 预估工时：0.5~1 天

## 一、阶段目标与范围边界

**目标**：消除硬编码密钥/连接串、密码明文存储等安全隐患，补齐数据库初始化脚本（B12）。

**范围**：
- Go：密码 bcrypt 哈希、JWT 密钥/DB DSN 配置化、注册防重复昵称、Logout 语义（可选黑名单）。
- 工程：MySQL 初始化 SQL（建库建表 + 唯一索引）。

**范围外**：传输层加密（TLS，作为后续建议）、限流。

## 二、前置条件

- Stage 1 完成（认证链路已修正）。

## 三、详细任务清单

- [x] 3.1 Go 引入 `golang.org/x/crypto/bcrypt`，注册时哈希存储、登录时比对
- [x] 3.2 JWT 密钥改为配置文件读取（`config.yaml` 增加 `jwt_secret`），本地开发给默认值并警告
- [x] 3.3 MySQL DSN 改为配置文件读取（`config.yaml` 的 `db` 段，拆分 host/port/user/password/database）
- [x] 3.4 `user.nickname` 增加唯一索引；注册时对重复昵称返回明确错误码
- [x] 3.5 提供 `docs/sql/init.sql`：建库 `chat_service`、建表 `user`、唯一索引
- [x] 3.6 `Logout` 语义完善：记录已注销 token 到内存黑名单，`ValidateSession`/`GetSessionInfo` 检查黑名单
- [x] 3.7 `SessionInfoResponse` 失败语义与注释对齐（RPC 错误 + user_id=0）
- [x] 3.8 更新 `User_test.go` 适配哈希存储（密码字段改为哈希值比对）

## 四、验收标准

- [ ] 数据库中密码为 bcrypt 哈希，登录使用哈希比对
- [ ] 密钥/连接串不出现于源码硬编码（默认值仅限本地开发配置）
- [ ] `init.sql` 可在空 MySQL 上直接执行并启动服务
- [ ] 重复昵称注册被拒绝
- [ ] 登出后旧 token 立即失效

## 五、风险与应对措施

| 风险 | 应对 |
| ---- | ---- |
| bcrypt 影响注册/登录性能 | 仅登录注册路径使用，成本因子取默认（10） |
| 旧库已有明文数据 | `init.sql` 面向新部署；已有环境提供迁移说明 |
| 黑名单内存方案重启失效 | 阶段内先做内存实现并注明局限，后续可换 Redis |

## 六、实现记录与验收证据

### 代码变更

- **密码哈希**：`Database/User.go` 注册时 `bcrypt` 哈希存储（默认成本因子），新增 `VerifyPassword`；登录改用哈希比对；重复昵称（MySQL 1062）映射为 `ErrNicknameTaken`。
- **配置化**：`config.yaml` 增加 `jwt_secret` 与 `db`（host/port/user/password/database）；`ServiceConfig` 提供 `MySQLDSN()`；`JWT.SetSecret` 注入密钥（保留默认占位并启动警告）。
- **数据库初始化显式化**：移除 `Database` 包 `init()` 隐式连接，`main.go` 启动时 `InitDB(dsn)`。
- **注销黑名单**：`JWT.RevokeToken/IsTokenRevoked`（`sync.Map` 内存实现，过期条目尽力清理）；`Logout` 真正注销；`ValidateSession`/`GetSessionInfo` 检查黑名单。
- **协议注释对齐**：`SessionInfoResponse` 注释改为"失败时 RPC 返回错误，user_id=0"。
- **初始化脚本**：新增 `docs/sql/init.sql`（utf8mb4、`user` 表、`uk_nickname` 唯一索引、`created_at`）。

### 验收证据

- [x] `go test ./...` 全部通过（JWT：新增注销/密钥用例；Database：哈希存储/重复昵称/密码校验；gRPCUserSession：已注销 token 校验）
- [x] 本地 MySQL 已应用 `init.sql` 与唯一索引；新注册用户密码为 bcrypt 哈希（`$2a$10$...`）
- [x] 集成回归测试 33 项全部通过（新增：重复昵称注册被拒、logout 后旧 token 返回 502）
- [x] `go build` 通过（`ChatRoom.Server.Models/Build/usersession.exe`）
- [ ] 服务重启后黑名单失效（内存实现，已注明；生产可换 Redis）
