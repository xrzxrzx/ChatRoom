# Event

## 基本格式（JSON）
**服务端 --> 客户端**

```JSON
{
	recode: 返回的状态码（没有错误时为0）,
	msg: "返回的错误消息（没有错误时为空）",
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
| sender |  `int32` | 发送人ID |
| notice_type | `string` | 发送的消息 |

**关于`notece_type`字段**

| 值       | 注解   |
| ------- | ---- |
| join | 加入聊天室 |
| leave | 离开聊天室 |

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