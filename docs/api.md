# API

## 基本格式（JSON）
**客户端 --> 服务端**

```JSON
{
	command: "命令",
	params: 
	{
		"key1": "value1" ,
	    "key2": "value2" ,
		...
	},
	token: "令牌"
	echo: "回声"
}
```

**返回时：**

```JSON
{
	recode: 返回的状态码（没有错误时为0）,
	msg: "返回的错误消息（没有错误时为空）",
	data:
	{
		"key1": "value1" ,
	    "key2": "value2" ,
		...
	},
	echo: "回声"
}
```
> `echo` 字段为回声，每个未关闭的API请求都应有一个唯一的回声，以保证能够获取正确的API返回数据

**关于`command`字段**

| 值       | 注解   |
| ------- | ---- |
| [send_message](#send_message) | 发送消息 |
| [get_room_list](#get_room_list) | 获取房间信息列表 |
| [register](#register) | 注册 |
| [login](#login) | 登录 |
| [request](#request-预留) | 请求   |

***

## send_message

**请求参数**

| 字段      |   类型   | 注解   |
| ------- | ------- | ------- |
| sender |  `int32` |   发送人ID |
| message | `string` | 发送的消息   |

**返回数据**

| 字段      |   类型   | 注解   |
| ------- | ------- | ------- |
| message |  `string` |   发送的消息 |

***

## get_room_list

**请求参数**

| 字段      |   类型   | 注解   |
| ------- | ------- | ------- |
| sender |  `int32` |   发送人ID |

**返回数据**

| 字段      |   类型   | 注解   |
| ------- | ------- | ------- |
| room_info_list |  `array` |   房间信息列表 |

**关于`room_info_list`字段**

| 字段      |   类型   | 注解   |
| ------- | ------- | ------- |
| room_id |  `int32` |   房间ID |
| room_name |  `string` |   房间名称 |

***

## join_room

**请求参数**

| 字段      |   类型   | 注解   |
| ------- | ------- | ------- |
| sender |  `int32` |   发送人ID |
| room_id |  `int32` |   房间ID |

**返回数据**

| 字段      |   类型   | 注解   |
| ------- | ------- | ------- |
| success |  `bool` |   是否成功加入房间 |

***

## register

**请求参数**

| 字段      |   类型   | 注解   |
| ------- | ------- | ------- |
| password | `string` | 密码   |
| nickname | `string` | 昵称   |

**返回数据**

| 字段      |   类型   | 注解   |
| ------- | ------- | ------- |
| user_id |  `int32` |   用户ID |
| session_token |  `string` |  会话Token(JWT)  |
| nickname |  `string` |   用户昵称 |

***

## login

**请求参数**

| 字段      |   类型   | 注解   |
| ------- | ------- | ------- |
| user_id |  `int32` |   用户ID |
| password | `string` | 密码   |

**返回数据**

| 字段      |   类型   | 注解   |
| ------- | ------- | ------- |
| session_token |  `string` |  会话Token(JWT)  |
| nickname |  `string` |   用户昵称 |

***

## request （预留）

**请求参数**

| 字段      |   类型   | 注解   |
| ------- | ------- | ------- |
| sender |  `int32` |   发送人ID |

**返回数据**

| 字段      |   类型   | 注解   |
| ------- | ------- | ------- |
| sender |  `int32` |   发送人ID |