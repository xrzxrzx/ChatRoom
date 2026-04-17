# Event

## 格式（JSON）
##### 服务端 --> 客户端

```JSON
{
	recode: 返回的状态码（没有错误时为0）,
	msg: "返回的错误消息（没有错误时为空）",
	type: "消息类型（有错误时为空）",
	data: //返回的正常数据（有错误时为空）
	{
		"key1": "value1",
		"key2": "value2",
		...
	}
}
```

对于`type`字段

| 值         | 注解   |
| --------- | ---- |
| message   | 消息类型 |
| notice    | 通知类型 |
| heartbeat | 心跳类型 |
| request   | 请求类型 |

对于`data`字段

