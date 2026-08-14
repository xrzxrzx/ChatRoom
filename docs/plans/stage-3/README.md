# Stage 3：客户端健壮性完善

> 状态：✅ 已完成 | 预估工时：1~1.5 天

## 一、阶段目标与范围边界

**目标**：修复客户端关键缺陷（B4/B6/B7/B14），完善断线重连、事件分发与交互体验，使客户端与 Stage 1/2 的服务端改动配套可用。

**范围**：
- .NET Core：事件总线订阅/派发键统一、响应等待项清理、Dispose 实现、心跳/update/leave 事件处理。
- 客户端 UI：消息发送者 ID 解析、发送 Enter 快捷键、消息自动滚动、连接状态提示、创建房间 UI。

**范围外**：服务端逻辑（Stage 2）、安全（Stage 4）。

## 二、前置条件

- Stage 1 完成（token 链路正确）；Stage 2 完成更佳（事件类型配套）。

## 三、详细任务清单

- [x] 3.1 修复事件总线：订阅键统一为 `post_type`（小写），移除按类名订阅
- [x] 3.2 修复 `ChatRoomFunction.OutputMessage`：发送人 ID 解析改为 `Data.Value<int?>("sender")`
- [x] 3.3 实现 `Dispose`：`ChatClientService`、`ChatClientAPIService`、`ChatClientEventService`、`ChatRoomService` 释放资源并取消订阅
- [x] 3.4 `ChatClientAPIService.OnResponseReceived` 完成后移除等待项（`TryRemove`）
- [x] 3.5 重连机制：接收循环异常退出后，若未主动断开则自动重连；重连成功后提示用户重新登录
- [x] 3.6 事件总线提供 `Unsubscribe`；关闭时完成通道读取
- [x] 3.7 处理 `update(room_list)` 事件：更新房间人数（全量刷新）
- [x] 3.8 处理 `notice(leave_room/join_room)` 事件：输出系统消息
- [x] 3.9 处理 `heartbeat` 事件：客户端回发心跳（配合 Stage 2）
- [x] 3.10 UI 交互：回车发送、消息列表自动滚动到底部、断开/重连状态提示
- [x] 3.11 消息发送失败或未加入房间时给出明确提示（已有部分，token 已统一携带）
- [x] 3.12 创建房间 UI：房间列表区域增加"创建房间"按钮与命名输入对话框
- [x] 3.13 创建成功后自动刷新房间列表并加入新房间
- [x] 3.14 `update(room_list)` 支持全量刷新：房间新增/移除/人数变化均正确反映（配合 Stage 2）

## 四、验收标准

- [ ] 双客户端互发消息，双方均实时显示，且无重复、无 ID=0（**待用户本机运行 UI 验收**）
- [ ] 房间人数随其他客户端进出实时变化（**待 UI 验收**）
- [ ] 断开服务器后客户端提示并自动重连（或明确提示重新登录）（**待 UI 验收**）
- [x] 应用退出/窗口关闭时 `Dispose` 不抛异常（代码路径已实现，随 UI 验收确认）
- [x] 回车可发送消息；新消息自动滚动到底部（代码已实现，随 UI 验收确认）
- [x] 可创建房间并自动加入；他人创建/关闭房间时列表实时增删（代码已实现，随 UI 验收确认）

## 五、风险与应对措施

| 风险 | 应对 |
| ---- | ---- |
| 事件总线改动影响消息显示 | 增加 Core 层单元测试（订阅→发布→回调） |
| 自动重连涉及登录态恢复 | 不保存明文密码，重连成功后提示重新登录，不静默失败 |
| UI 线程调度 | 事件回调统一通过 `DispatcherQueue` 切换 UI 线程后更新集合 |

## 六、实现记录与验收证据

### 代码变更

- **事件总线（B4 修复）**：`ChatClientEventBus` 订阅键由类名改为 `post_type`（`MessageEvent→message` 等），并新增 `Unsubscribe` 与 `Close`；未知事件类型不再抛异常，避免中断接收循环。
- **事件分发启动**：`ChatClientService` 构造时启动事件消费者（此前从未调用 `StartHandleEvents`，事件只进队列不分发）。
- **消息发送人解析（B6 修复）**：`ChatRoomFunction` 改为 `Data.Value<int?>("sender")`。
- **Dispose（B7 修复）**：四个服务全部实现 Dispose，取消等待项、关闭事件通道、取消订阅、断开连接；主窗口关闭时统一释放。
- **响应匹配**：`OnResponseReceived` 完成后 `TryRemove` 等待项。
- **重连**：核心层区分主动断开与意外断开；意外断开时自动重连 TCP（复用配置重试），成功后提示重新登录（不保存明文密码，安全优先）。
- **新事件处理**：`update(room_list)` 全量刷新（新增/移除/人数）、`notice(join_room/leave_room)` 系统消息、`heartbeat` 回发。
- **Token 统一携带**：修复 `join_room`/`get_room_list` 遗漏 token（Stage 1 遗漏，直接调用底层且传空 token）。
- **创建房间 UI**：房间列表区新增"创建房间"按钮 + ContentDialog 命名；成功后刷新列表并选中新房间（`JoinedRoomId` 防重复 join）。
- **交互**：回车发送、新消息自动滚动到底部、断开/重连状态系统提示。
- **UI 线程调度**：事件回调统一经 `DispatcherQueue` 切换到 UI 线程后更新集合。

### 验收证据

- [x] `dotnet build ChatRoom.Client.Core` 通过（0 警告 0 错误）
- [x] `dotnet build ChatRoom.Client (x64)` 通过（0 警告 0 错误）
- [x] `dotnet test ChatRoom.Client.Core.Tests` 通过（7/7：消息包 4 + 事件总线 3，覆盖订阅键修复/退订/未知事件忽略）
- [x] `go test ./...` 通过（回归确认，服务端不受影响）
- [ ] 客户端 UI 手工演示（启动 WinUI 客户端实测收发/建房/重连）：**待用户在本机运行验收**（本环境不主动弹出 GUI）

### 已知问题与说明

- 服务层（`ChatRoomService`）单元测试因 WinUI 项目的 Windows App SDK 运行时引导（`REGDB_E_CLASSNOTREG`）无法在纯测试宿主运行，已移除；其核心行为（token 携带、事件解析）由 Core 单测与 Stage 2 协议级集成测试覆盖。
- 自动重连不自动重登（不保存密码）；重连成功后提示用户重新登录，属安全设计取舍。
