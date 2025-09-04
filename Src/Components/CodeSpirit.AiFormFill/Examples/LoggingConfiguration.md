# AI表单填充响应内容日志配置

## 📋 概述

为了便于调试和监控 AI 表单填充功能，我们已经在各个关键环节添加了详细的日志输出，包括：

- LLM API 请求和响应内容
- 流式和非流式响应的完整数据
- JSON 解析过程
- 属性设置详情

## 🔧 日志级别配置

### appsettings.json 配置

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "CodeSpirit.AiFormFill": "Information",
      "CodeSpirit.AiFormFill.Services.AiFormFillLLMClient": "Information",
      "CodeSpirit.AiFormFill.Services.AiFormFillService": "Information",
      "CodeSpirit.AiFormFill.Services.AiFormResponseParser": "Information"
    }
  }
}
```

### 开发环境详细调试配置

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "CodeSpirit.AiFormFill": "Debug",
      "CodeSpirit.AiFormFill.Services.AiFormFillLLMClient": "Debug",
      "CodeSpirit.AiFormFill.Services.AiFormFillService": "Debug",
      "CodeSpirit.AiFormFill.Services.AiFormResponseParser": "Debug"
    }
  }
}
```

## 📊 日志输出内容

### 1. AiFormFillService 日志

```
[Information] 使用独立AI表单填充LLM配置，设置键：AiFormFillLLM
[Information] AI表单填充LLM响应内容：{"choices":[{"message":{"content":"{\"description\":\"基于人工智能技术的问卷调研\",\"targetAudience\":\"技术从业者\"}"}}]}
[Information] AI表单填充解析后的结果：{"Topic":"AI技术调研","Description":"基于人工智能技术的问卷调研","TargetAudience":"技术从业者"}
```

### 2. AiFormFillLLMClient 日志

#### 非流式响应：
```
[Information] 发送AI表单填充请求到: https://dashscope.aliyuncs.com/compatible-mode/v1/chat/completions
[Information] AI表单填充API响应状态码: OK
[Debug] AI表单填充API响应头: Content-Type: application/json, X-Request-Id: 12345
[Information] AI表单填充API完整响应内容: {"id":"chatcmpl-xxx","object":"chat.completion","created":1234567890,"model":"qwq-plus","choices":[{"index":0,"message":{"role":"assistant","content":"{\"description\":\"基于人工智能技术的问卷调研\",\"targetAudience\":\"技术从业者\"}"},"finish_reason":"stop"}]}
[Information] AI表单填充非流式响应提取的生成内容: {"description":"基于人工智能技术的问卷调研","targetAudience":"技术从业者"}
```

#### 流式响应：
```
[Information] 发送AI表单填充请求到: https://dashscope.aliyuncs.com/compatible-mode/v1/chat/completions
[Information] AI表单填充API响应状态码: OK
[Debug] AI表单填充流式内容片段: {
[Debug] AI表单填充流式内容片段: "description"
[Debug] AI表单填充流式内容片段: :
[Debug] AI表单填充流式内容片段: "基于人工智能技术的问卷调研"
[Debug] AI表单填充完整流式响应数据:
data: {"choices":[{"delta":{"content":"{"}}]}
data: {"choices":[{"delta":{"content":"\"description\""}}]}
data: {"choices":[{"delta":{"content":":"}}]}
data: {"choices":[{"delta":{"content":"\"基于人工智能技术的问卷调研\""}}]}
data: [DONE]
[Information] AI表单填充流式响应最终生成内容: {"description":"基于人工智能技术的问卷调研","targetAudience":"技术从业者"}
```

### 3. AiFormResponseParser 日志

```
[Information] AI表单填充提取的JSON内容：{"description":"基于人工智能技术的问卷调研","targetAudience":"技术从业者"}
[Information] AI表单填充成功设置属性 Description = 基于人工智能技术的问卷调研
[Information] AI表单填充成功设置属性 TargetAudience = 技术从业者
```

## 🔍 调试技巧

### 1. 查看完整请求体

在 `AiFormFillLLMClient` 中，请求体会以 Debug 级别记录：

```
[Debug] AI表单填充请求体: {"model":"qwq-plus","messages":[{"role":"system","content":"你是一个专业的AI助手..."},{"role":"user","content":"基于输入的问卷主题：\"AI技术调研\"..."}],"max_tokens":1500,"temperature":0.1,"top_p":0.9,"stream":false,"enable_thinking":false,"response_format":{"type":"json_object"}}
```

### 2. 监控响应解析过程

当 JSON 解析失败时，会记录详细的错误信息：

```
[Warning] 转换属性值失败：Description, 值：null
[Warning] JSON中未找到属性：TargetAudience（JSON属性名：TargetAudience）
```

### 3. 检查响应结构

当响应结构不符合预期时：

```
[Warning] AI表单填充响应结构不符合预期，完整响应: {"error":{"message":"Invalid request","type":"invalid_request_error"}}
```

### 4. 错误状态码处理

当 API 返回非成功状态码时，系统会自动打印错误内容：

#### HTTP 错误响应（非流式）：
```
[Information] AI表单填充API响应状态码: BadRequest
[Error] AI表单填充API请求失败，状态码: BadRequest, 错误内容: {"error":{"message":"Invalid API key","type":"authentication_error","code":"invalid_api_key"}}
```

#### HTTP 错误响应（流式）：
```
[Information] AI表单填充API响应状态码: Unauthorized
[Warning] AI表单填充流式响应状态码不成功: Unauthorized，尝试读取错误流内容
[Error] AI表单填充流式响应失败，状态码: Unauthorized, 完整流式内容: {"error":{"message":"Unauthorized","type":"authentication_error"}}
```

#### 网络错误：
```
[Error] AI表单填充HTTP请求失败: The SSL connection could not be established
[Error] AI表单填充HTTP请求异常详细信息: RemoteCertificateNameMismatch: True
```

#### 超时错误：
```
[Error] AI表单填充请求超时: The operation was canceled.
```

## 🚨 常见问题排查

### 1. 响应内容为空
- 检查 API 密钥是否正确
- 确认模型名称是否支持
- 查看完整响应内容判断 API 是否返回错误

### 2. JSON 解析失败
- 检查 `ResponseFormatType` 是否设置为 `"json_object"`
- 查看 `DisableThinking` 是否设置为 `true`
- 检查提示词是否明确要求 JSON 格式输出

### 3. 属性设置失败
- 确认 DTO 属性名与 JSON 字段名匹配
- 检查属性类型是否兼容
- 查看是否有 `JsonProperty` 特性影响映射

### 4. HTTP 错误状态码排查

#### 401 Unauthorized
```
[Error] AI表单填充API请求失败，状态码: Unauthorized, 错误: {"error":{"message":"Invalid API key"}}
```
- 检查 API 密钥是否正确
- 确认 API 密钥是否有效且未过期
- 验证 API 基础地址是否正确

#### 400 Bad Request
```
[Error] AI表单填充API请求失败，状态码: BadRequest, 错误: {"error":{"message":"Invalid model name"}}
```
- 检查模型名称是否正确
- 确认请求参数格式是否符合 API 要求
- 验证 `enable_thinking` 和 `response_format` 参数是否被目标模型支持

#### 400 Bad Request - 只支持流式模式
```
[Error] AI表单填充API请求失败，状态码: BadRequest, 错误: {"error":{"message":"This model only support stream mode, please enable the stream parameter to access the model."}}
[Warning] 检测到模型只支持流式模式，尝试启用流式响应重新请求
[Information] 使用流式模式重新发送AI表单填充请求
[Information] AI表单填充流式重试响应状态码: OK
[Information] AI表单填充流式响应最终生成内容: {"description":"智能生成的内容"}
```
- 系统会自动检测此错误并启用流式模式重试
- 无需手动配置，组件会自动处理
- 如果希望避免重试，可以在配置中直接启用 `EnableStreaming: true`

#### 429 Too Many Requests
```
[Error] AI表单填充API请求失败，状态码: TooManyRequests, 错误: {"error":{"message":"Rate limit exceeded"}}
```
- 检查是否触发了 API 调用频率限制
- 考虑添加重试机制或降低调用频率

#### 500 Internal Server Error
```
[Error] AI表单填充API请求失败，状态码: InternalServerError, 错误: {"error":{"message":"Internal server error"}}
```
- API 服务端错误，通常是临时性问题
- 可以尝试重新请求
- 检查请求参数是否导致服务端异常

### 5. 网络连接问题
```
[Error] AI表单填充HTTP请求失败: The SSL connection could not be established
[Error] AI表单填充请求超时: The operation was canceled.
```
- 检查网络连接是否正常
- 确认防火墙或代理设置
- 调整超时时间设置
- 验证 SSL 证书配置

## 📝 生产环境建议

在生产环境中，建议：

1. 将日志级别设置为 `Information`，避免过多的 Debug 日志
2. 使用结构化日志记录关键信息
3. 监控响应时间和成功率
4. 定期检查错误日志并优化配置

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "CodeSpirit.AiFormFill": "Information"
    }
  }
}
```
