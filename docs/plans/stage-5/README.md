# Stage 5：文档与工程化

> 状态：⏳ 待批准 | 预估工时：0.5~1 天

## 一、阶段目标与范围边界

**目标**：补齐根 README、修正协议文档、补充 CI 与构建脚本，让项目达到"开箱可构建、文档可对齐、回归可自动化"（B13）。

**范围**：
- 根 README、架构说明、构建/运行手册。
- `docs/api.md`、`docs/event.md` 与实现全面对齐。
- GitHub Actions：Go 测试、.NET Core 构建、C++ 构建（windows-latest + vcpkg，尽力而为）。
- protoc 编译脚本编码修正；`.gitignore`/`docsify` 配置核对。

**范围外**：新增功能开发。

## 二、前置条件

- Stage 1~4 完成并验收（文档与代码一致的基础）。

## 三、详细任务清单

- [ ] 3.1 根 `README.md`：项目简介、架构图、目录结构、环境要求（.NET 10 / VS / vcpkg / Go / MySQL）
- [ ] 3.2 运行手册：MySQL 初始化 → Go 服务 → C++ 服务 → 客户端，含配置文件说明
- [ ] 3.3 `docs/api.md` 修正：`action` 拼写、字段补全（login 返回 user_id、新增 create_room）、register 参数对齐
- [ ] 3.4 `docs/event.md` 修正：heartbeat/update/leave_room 与实现对齐，update(room_list) 覆盖新增/移除
- [ ] 3.5 `docs/README.md`/`_sidebar.md` 增加规划文档与 SQL 文档入口
- [ ] 3.6 GitHub Actions：`go test`、`dotnet build ChatRoom.Client.Core`、C++ 构建（若环境可行）
- [ ] 3.7 `Compile Protos.bat` 编码修正（当前为 GBK 乱码），统一 UTF-8
- [ ] 3.8 补充/整理 `.gitignore`、`.editorconfig`（可选）
- [ ] 3.9 文档一致性检查：`docs` 与代码逐项核对，输出检查清单

## 四、验收标准

- [ ] 根 README 可指导新人在干净环境完成构建与运行
- [ ] `docs/api.md`、`docs/event.md` 与代码一致（无已删除/未实现接口）
- [ ] CI 至少覆盖 Go 测试与 .NET Core 构建并全部通过
- [ ] `Compile Protos.bat` 显示正常中文提示

## 五、风险与应对措施

| 风险 | 应对 |
| ---- | ---- |
| C++ 构建在 CI 环境依赖复杂 | CI 先跑 Go/.NET，C++ 作为独立 job 尽力验证，失败不阻塞其他检查 |
| 文档遗漏 | 以"逐接口核对表"方式验收，避免凭印象 |
| protoc/工具链版本差异 | 脚本内固定插件路径并提供环境变量覆盖 |
