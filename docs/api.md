# API

## 格式（JSON）
##### 客户端 --> 服务端

```JSON
{
	command: "命令",
	params: 
	{
		"key1": "value1" ,
	    "key2": "value2" ,
		...
	}
}
```

对于`command`字段

| 值       | 注解   |
| ------- | ---- |
| message | 发送消息 |
| request | 请求   |
| notice  | 通知   |