# Event

服务端 → 客户端事件推送协议。

## 概览

- 传输/编码/消息边界与 [API](api.md) 一致。
- 方向：服务端 → 客户端（客户端 → 服务端请求见 [API](api.md)）。

> **版本说明**：本文档描述目标协议。标注 📋 的字段/事件为规划中内容（对应 `docs/plans/` 中的阶段），实现后移除标记。

## 基本格式

```json
{
  "post_type": "消息类型",
  "data": {
    "key1": "value1",
    "key2": "value2"
  }
}
```

| 字段 | 类型 | 说明 |
| ---- | ---- | ---- |
| post_type | `string` | 事件类型（见下表） |
| data | `object` | 事件数据 |

## 关于 post_type 字段

| 值 | 注解 |
| ---- | ---- |
| [message](#message) | 消息 |
| [notice](#notice) | 通知（加入/离开房间） |
| [update](#update) | 数据更新（房间列表） |
| [heartbeat](#heartbeat) | 心跳 📋 |
| [request](#request预留) | 请求（预留） |

---

## message

**事件数据**

| 字段 | 类型 | 注解 |
| ---- | ---- | ---- |
| sender | `int32` | 发送人 ID |
| nickname | `string` | 发送人昵称 |
| message | `string` | 消息内容 |

**说明**

- 房间消息广播，仅发送给房间内**其他成员**，**不含发送者本人**（📋 Stage 2 起生效；本人消息通过 `send_message` 的 API 响应回显）。

---

## notice

**事件数据**

| 字段 | 类型 | 注解 |
| ---- | ---- | ---- |
| user_id | `int32` | 用户 ID |
| nickname | `string` | 用户昵称 📋 |
| notice_type | `string` | 通知类型 |

**关于 notice_type 字段**

| 值 | 注解 |
| ---- | ---- |
| join_room | 加入聊天室（当前已实现） |
| leave_room | 离开聊天室 📋（Stage 2 实现：离开/断线时广播） |

---

## update

**事件数据**

| 字段 | 类型 | 注解 |
| ---- | ---- | ---- |
| update_type | `string` | 更新类型 |
| update_data | `array` | 更新数据 |

**关于 update_type 字段**

| 值 | 注解 |
| ---- | ---- |
| room_list | 聊天室列表（全量）📋 |

**关于 update_data 元素（room_list）**

| 字段 | 类型 | 注解 |
| ---- | ---- | ---- |
| room_id | `int32` | 房间 ID |
| room_name | `string` | 房间名称 |
| user_count | `int32` | 房间人数 |

**说明**

- `room_list` 为**全量房间列表**（📋 Stage 2 起），客户端应以全量替换本地列表，覆盖房间新增、移除与人数变化。
- 用户创建的房间空置后自动关闭，并在此事件中消失；系统默认房间常驻（📋 Stage 2）。

---

## heartbeat

> 📋 规划中，Stage 2 实现。

**事件数据**

| 字段 | 类型 | 注解 |
| ---- | ---- | ---- |
| time | `int64` | 服务端时间戳 |

**说明**

- 服务端定期向在线客户端下发；客户端收到后回发 `heartbeat` API（见 [API](api.md#heartbeat)）。
- 服务端据此维持连接状态，长时间无数据的连接视为死连接并断开。

---

## request（预留）

**事件数据**

| 字段 | 类型 | 注解 |
| ---- | ---- | ---- |
| time | `int64` | 时间戳 |
