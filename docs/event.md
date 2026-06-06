# Event

## 基本格式（JSON）
**服务端 --> 客户端**

```JSON
{
	post_type: "消息类型（有错误时为空）",
	data: //返回的正常数据（有错误时为空）
	{
		"key1": "value1",
		"key2": "value2",
		...
	}
}
```

**对于`post_type`字段**

| 值         | 注解   |
| --------- | ---- |
| [message](#message)   | 消息类型 |
| [notice](#notice)    | 通知类型 |
| [request](#request预留)   | 请求类型（预留） |
| [heartbeat](#heartbeat) | 心跳类型 |

***

## message

**事件数据**

| 字段      |   类型   | 注解   |
| ------- | ------- | ------- |
| sender |  `int32` | 发送人ID |
| nickname |  `string` | 发送人昵称 |
| message | `string` | 发送的消息 |

***

## notice

**事件数据**

| 字段      |   类型   | 注解   |
| ------- | ------- | ------- |
| user_id |  `int32` | 发送人ID |
| notice_type | `string` | 通知类型 |

**关于`notece_type`字段**

| 值       | 注解   |
| ------- | ---- |
| join_room | 加入聊天室 |
| leave_room | 离开聊天室 |

***

## update

**事件数据**

| 字段      |   类型   | 注解   |
| ------- | ------- | ------- |
| update_type | `string` | 更新类型 |
| update_data | `array` | 更新数据 |

**关于`update_type`字段**

| 值       | 注解   |
| ------- | ---- |
| [room_list](#room_list) | 聊天室列表 |

**关于`update_data`字段**

##### `room_list`
| 字段      |   类型   | 注解   |
| ------- | ------- | ------- |
| room_id |  `int32` |   房间ID |
| room_name |  `string` |   房间名称 |

***

## request（预留）

**事件数据**

| 字段      |   类型   | 注解   |
| ------- | ------- | ------- |
| time |  `int64` | 时间戳 |

***

## heartbeat

**事件数据**

| 字段      |   类型   | 注解   |
| ------- | ------- | ------- |
| time |  `int64` | 时间戳 |