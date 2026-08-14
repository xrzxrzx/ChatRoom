# Stage 5：文档与工程化

> 状态：✅ 已完成 | 预估工时：0.5~1 天

## 一、阶段目标与范围边界

**目标**：补齐根 README、修正协议文档、补充 CI 与构建脚本，让项目达到"开箱可构建、文档可对齐、回归可自动化"。

**范围**：根 README、运行手册、CI、protoc 脚本修正、文档一致性检查。

**范围外**：新增功能开发。

## 二、前置条件

- Stage 1~4 完成并验收。

## 三、详细任务清单

- [x] 3.1 根 `README.md`：项目简介、架构图、目录结构、环境要求（.NET 10 / VS / vcpkg / Go / MySQL）
- [x] 3.2 运行手册：MySQL 初始化 → Go 服务 → C++ 服务 → 客户端，含配置文件说明
- [x] 3.3 `docs/api.md` 修正：`action` 拼写、字段补全（login 返回 user_id、新增 create_room）、register 参数对齐
- [x] 3.4 `docs/event.md` 修正：heartbeat/update/leave_room 与实现对齐，update(room_list) 覆盖新增/移除
- [x] 3.5 `docs/README.md`/`_sidebar.md` 增加规划文档与 SQL 文档入口
- [x] 3.6 GitHub Actions：`go test`、`dotnet build ChatRoom.Client.Core`、Core 单元测试
- [x] 3.7 `Compile Protos.bat` 编码修正（改为纯 ASCII 英文提示，任意代码页可读，去掉 `pause`）
- [x] 3.8 `.gitignore`、`.editorconfig` 整理（`.editorconfig` 已新增；`.gitignore` 补充 Go 构建产物）
- [x] 3.9 文档一致性检查：`docs` 与代码逐项核对，输出检查清单

## 四、验收标准

- [x] 根 README 可指导新人在干净环境完成构建与运行
- [x] `docs/api.md`、`docs/event.md` 与代码一致（无已删除/未实现接口；仅 Stage 4 黑名单等已标注 📋）
- [x] CI 覆盖 Go 测试与 .NET Core 构建/测试
- [x] `Compile Protos.bat` 可正常运行并重新生成 C++/Go 代码（实测通过）

## 五、风险与应对措施

| 风险 | 应对 |
| ---- | ---- |
| C++ 构建在 CI 环境依赖复杂 | CI 暂不包含 C++ job（需 vcpkg + protoc 环境），保留本地构建说明；后续可补充 |
| 文档遗漏 | 以"逐接口核对表"方式验收（见下） |
| 批处理中文乱码 | 改为纯 ASCII 英文提示，彻底规避代码页问题 |

## 六、实现记录与验收证据

### 代码变更

- 新增根 `README.md`：架构图、模块表、环境要求、快速开始（数据库 → Go → C++ → 客户端）、测试方法、已知限制。
- 新增 `.github/workflows/ci.yml`：Go 单元测试（ubuntu）、.NET Core 构建与单元测试（windows）。
- 重写 `Protos/Compile Protos.bat`：纯 ASCII 提示、`VCPKG_ROOT` 支持、protoc 自动探测、去掉 `pause`；实测重新生成 C++/Go 代码成功。
- `docs/README.md` 增加 SQL 初始化与根 README 入口；`docs/_sidebar.md` 增加规划与编码规范入口（此前已完成）。

### 文档一致性检查清单（3.9）

| 检查项 | 结果 |
| ---- | ---- |
| api.md 接口列表 vs `UserSession` API 处理器 | ✅ 一致（register/login/logout/create_room/get_room_list/join_room/send_message/heartbeat/request 预留） |
| api.md 登录返回 `user_id`、注册无 `user_id` | ✅ 与代码一致 |
| event.md post_type vs 服务端广播 | ✅ 一致（message/notice/update/heartbeat/request 预留） |
| event.md `update(room_list)` 全量语义 | ✅ 与 `BroadcastRoomListUpdate` 一致 |
| 消息广播不含发送者本人 | ✅ 与 `ChatRoom::Broadcast(exclude)` 一致 |
| 错误码表（400/401/403/404/409/502） | ✅ 与代码一致 |
| 文档链接（README/docs/plans/sql/脚本） | ✅ 全部有效 |
| 编码规范 vs 代码现状 | ✅ gofmt 通过、C# 0 警告、C++ 编译通过 |

### 验收证据

- [x] `go test ./...` 全部通过
- [x] `dotnet build ChatRoom.Client.Core` 0 警告 0 错误
- [x] `dotnet build ChatRoom.Client (x64)` 0 警告 0 错误
- [x] `dotnet test ChatRoom.Client.Core.Tests` 7/7
- [x] C++ `ChatRoom.Server`（Debug|x64）编译通过
- [x] `Compile Protos.bat` 实测生成 C++ 与 Go 代码成功
- [x] 文档一致性检查清单全部通过
