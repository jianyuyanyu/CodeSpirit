# OperationAttribute Actions 配置使用指南

## 概述

`OperationAttribute` 特性现在支持自定义底部按钮配置，通过 `Actions` 属性可以灵活控制对话框、表单等操作界面的底部按钮显示。

## Actions 属性说明

### 属性定义
```csharp
/// <summary>
/// 自定义底部按钮配置，JSON格式的字符串数组
/// 如果为null，则使用默认按钮；如果为空数组，则不显示底部按钮
/// 示例：[{"type":"button","label":"确定","actionType":"submit"},{"type":"button","label":"取消","actionType":"close"}]
/// </summary>
public string Actions { get; set; }
```

### 支持的值

1. **null（默认值）**：使用系统默认的底部按钮
2. **空字符串 `""`**：不显示任何底部按钮
3. **JSON数组字符串**：自定义按钮配置

## 使用示例

### 1. 使用默认按钮
```csharp
[HttpPost("custom-action")]
[Operation("自定义操作", OperationActionType.Form)]
[DisplayName("自定义操作")]
public async Task<ActionResult<ApiResponse>> CustomAction(CustomDto dto)
{
    // 实现逻辑
    return Ok(ApiResponse.Success());
}
```

### 2. 不显示底部按钮
```csharp
[HttpPost("no-buttons")]
[Operation("无按钮操作", OperationActionType.Form, Actions = "")]
[DisplayName("无按钮操作")]
public async Task<ActionResult<ApiResponse>> NoButtonsAction(CustomDto dto)
{
    // 实现逻辑
    return Ok(ApiResponse.Success());
}
```

### 3. 自定义按钮配置
```csharp
[HttpPost("custom-buttons")]
[Operation("自定义按钮", OperationActionType.Form, 
    Actions = """[
        {
            "type": "button",
            "label": "保存并继续",
            "actionType": "submit",
            "level": "primary",
            "icon": "fa fa-save"
        },
        {
            "type": "button", 
            "label": "保存并关闭",
            "actionType": "submit",
            "level": "success",
            "icon": "fa fa-check",
            "close": true
        },
        {
            "type": "button",
            "label": "取消",
            "actionType": "close",
            "level": "default"
        }
    ]""")]
[DisplayName("自定义按钮")]
public async Task<ActionResult<ApiResponse>> CustomButtonsAction(CustomDto dto)
{
    // 实现逻辑
    return Ok(ApiResponse.Success());
}
```

### 4. Service 类型操作的自定义按钮
```csharp
[HttpGet("service-with-actions")]
[Operation("服务操作", OperationActionType.Service,
    Actions = """[
        {
            "type": "button",
            "label": "刷新数据",
            "actionType": "reload",
            "target": "serviceContent",
            "level": "info"
        },
        {
            "type": "button",
            "label": "关闭",
            "actionType": "close",
            "level": "default"
        }
    ]""")]
[DisplayName("服务操作")]
public async Task<ActionResult<ApiResponse<AmisSchema>>> ServiceWithActions()
{
    // 返回 AMIS 配置
    return Ok(ApiResponse.Success(new AmisSchema()));
}
```

## 支持的操作类型

以下操作类型支持 `Actions` 配置：

- **Form**：表单操作
- **Service**：服务操作  
- **Return-Form**：返回结果表单
- **AiForm**：AI表单操作

## 按钮配置选项

### 基础属性
- `type`: 按钮类型，通常为 "button"
- `label`: 按钮显示文本
- `actionType`: 按钮动作类型

### 常用动作类型
- `submit`: 提交表单
- `close`: 关闭对话框
- `cancel`: 取消操作
- `reload`: 重新加载
- `ajax`: 发送AJAX请求
- `link`: 链接跳转

### 样式属性
- `level`: 按钮样式级别（primary, success, info, warning, danger, default）
- `icon`: 按钮图标（Font Awesome 图标类名）
- `className`: 自定义CSS类名

### 行为属性
- `close`: 是否在执行后关闭对话框
- `confirmText`: 确认提示文本
- `disabled`: 是否禁用
- `visibleOn`: 显示条件表达式

## 注意事项

1. **JSON格式**：Actions 属性值必须是有效的JSON数组字符串
2. **错误处理**：如果JSON解析失败，系统会记录警告并使用默认按钮
3. **兼容性**：现有代码无需修改，默认行为保持不变
4. **性能**：JSON解析在按钮创建时进行，不影响运行时性能

## 最佳实践

1. **使用原始字符串字面量**：使用 `"""` 包围JSON字符串，避免转义字符
2. **保持简洁**：只配置必要的按钮，避免界面过于复杂
3. **一致性**：在同一应用中保持按钮样式和行为的一致性
4. **测试**：确保自定义按钮的功能正常工作
