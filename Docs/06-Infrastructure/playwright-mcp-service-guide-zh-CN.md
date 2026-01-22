# Playwright MCP 服务使用指南

## 概述

Playwright MCP 服务是集成在 CodeSpirit 项目中的浏览器自动化工具，通过 Model Context Protocol (MCP) 提供强大的 Web 应用功能测试和调查能力。开发者可以通过 AI 助手（如 Cursor）使用自然语言与浏览器交互，实现自动化测试、UI 调试和功能验证。

## 核心优势

✅ **无代码测试**：通过自然语言描述即可完成复杂的浏览器操作  
✅ **实时反馈**：立即查看操作结果和页面状态  
✅ **可访问性优先**：基于可访问性树进行元素定位，更稳定可靠  
✅ **完整记录**：自动记录控制台消息和网络请求  
✅ **截图支持**：快速捕获页面状态用于文档或调试  

## 前置条件

- Aspire 应用已启动运行（`aspire run`）
- 了解应用的 HTTP 端点地址
- Playwright MCP 服务已在项目中配置

## 快速开始

### 获取应用端点

首先使用 Aspire MCP 工具获取资源端点：

```
list_resources
```

输出示例：
```
资源列表：
- CodeSpirit.Web [.NET Project]
  状态: Running
  端点: https://localhost:7120    ← 这个就是我们需要的地址
  健康: Healthy
```

### 基本操作流程

**典型的测试流程**：

1. **导航到页面** → 2. **获取页面快照** → 3. **与元素交互** → 4. **验证结果**

## 工具详解

### 1. 导航工具

#### 1.1 导航到 URL (browser_navigate)

**用途**：在浏览器中打开指定的网址

**参数**：
- `url`: 要访问的完整 URL

**使用场景**：
- 开始测试前访问应用
- 切换到不同的页面
- 测试外部链接

**示例**：
```typescript
// 访问系统平台登录页
browser_navigate(url: "https://localhost:7120/login")

// 访问租户登录页
browser_navigate(url: "https://localhost:7120/default/login")

// 访问用户管理页面
browser_navigate(url: "https://localhost:7120/default/admin#/identity/users")
```

**返回信息**：
- 当前页面 URL
- 页面标题
- 页面可访问性快照（元素树）

#### 1.2 返回上一页 (browser_navigate_back)

**用途**：返回浏览器历史记录的上一页

**使用场景**：
- 测试导航流程
- 返回列表页
- 验证浏览器历史功能

**示例**：
```typescript
browser_navigate_back()
```

### 2. 页面检查工具

#### 2.1 获取页面快照 (browser_snapshot)

**用途**：获取当前页面的可访问性树快照

**核心功能**：
- 获取页面上所有可交互元素
- 获取元素的 `ref` 引用（用于后续交互）
- 查看页面结构和内容

**返回格式**：
```yaml
- generic [ref=e15]:              # 容器元素
  - heading "欢迎登录" [ref=e20]   # 标题
  - textbox "请输入用户名" [ref=e25]  # 输入框
  - button "登录" [ref=e30]        # 按钮
```

**元素属性说明**：
- `ref`: 元素引用标识符（交互时必需）
- `[disabled]`: 元素已禁用
- `[cursor=pointer]`: 可点击元素
- `[level=1]`: 标题级别

**使用场景**：
- 在执行任何交互前获取元素引用
- 查看页面当前状态
- 定位目标元素
- 验证页面渲染是否正确

**最佳实践**：
- 每次交互前先获取快照
- 使用快照中的 `ref` 值进行精确定位
- 不要手动猜测 `ref` 值

#### 2.2 截图 (browser_take_screenshot)

**用途**：截取页面或元素的屏幕截图

**参数**：
- `filename`（可选）：保存的文件名
- `fullPage`（可选）：是否截取完整页面（默认仅可见区域）
- `type`（可选）：图片格式（`png` 或 `jpeg`，默认 `png`）
- `element` 和 `ref`（可选）：截取特定元素

**使用场景**：
- 记录测试结果
- 创建文档截图
- 报告 Bug 时提供视觉证据
- 对比 UI 变化

**示例**：
```typescript
// 截取当前可见区域
browser_take_screenshot()

// 截取完整页面
browser_take_screenshot(fullPage: true)

// 截取并保存为指定文件名
browser_take_screenshot(
  filename: "login-page.png",
  fullPage: true
)

// 截取特定元素
browser_take_screenshot(
  element: "登录表单",
  ref: "e15"
)
```

**注意事项**：
- 截图主要用于视觉查看，不能基于截图执行操作
- 要与页面交互必须使用 `browser_snapshot`

### 3. 交互工具

#### 3.1 点击 (browser_click)

**用途**：点击页面上的元素

**参数**：
- `element`: 元素的可读描述
- `ref`: 元素引用（从 snapshot 获取）
- `button`（可选）：鼠标按钮（`left`、`right`、`middle`，默认 `left`）
- `doubleClick`（可选）：是否双击（默认 `false`）
- `modifiers`（可选）：修饰键数组（`Alt`、`Control`、`Meta`、`Shift`）

**使用场景**：
- 点击按钮
- 选择链接
- 触发菜单
- 展开/折叠元素

**示例**：
```typescript
// 基本点击
browser_click(
  element: "登录按钮",
  ref: "e63"
)

// 右键点击
browser_click(
  element: "用户行",
  ref: "e100",
  button: "right"
)

// Ctrl + 点击
browser_click(
  element: "链接",
  ref: "e50",
  modifiers: ["Control"]
)

// 双击
browser_click(
  element: "文件项",
  ref: "e75",
  doubleClick: true
)
```

#### 3.2 输入文本 (browser_type)

**用途**：向可编辑元素输入文本

**参数**：
- `element`: 元素的可读描述
- `ref`: 元素引用
- `text`: 要输入的文本
- `slowly`（可选）：逐字符输入（触发键盘事件，默认 `false`）
- `submit`（可选）：输入后按回车（默认 `false`）

**使用场景**：
- 填写表单字段
- 输入搜索关键词
- 填写文本区域
- 测试输入验证

**示例**：
```typescript
// 基本输入
browser_type(
  element: "用户名输入框",
  ref: "e37",
  text: "admin"
)

// 输入并提交
browser_type(
  element: "搜索框",
  ref: "e25",
  text: "测试数据",
  submit: true
)

// 慢速输入（触发每个键的事件）
browser_type(
  element: "邮箱输入框",
  ref: "e40",
  text: "test@example.com",
  slowly: true
)
```

**注意事项**：
- 默认情况下会清空字段后再输入
- `slowly` 模式适合测试实时验证和自动完成功能
- `submit` 等同于输入后按 Enter 键

#### 3.3 悬停 (browser_hover)

**用途**：将鼠标悬停在元素上

**参数**：
- `element`: 元素的可读描述
- `ref`: 元素引用

**使用场景**：
- 触发悬停效果（tooltip、下拉菜单）
- 测试鼠标悬停交互
- 查看悬停提示信息

**示例**：
```typescript
browser_hover(
  element: "用户头像",
  ref: "e26"
)
```

#### 3.4 选择下拉选项 (browser_select_option)

**用途**：在下拉列表中选择选项

**参数**：
- `element`: 元素的可读描述
- `ref`: 元素引用
- `values`: 要选择的值数组

**使用场景**：
- 选择下拉菜单选项
- 多选列表
- 表单选择字段

**示例**：
```typescript
// 单选
browser_select_option(
  element: "语言选择器",
  ref: "e19",
  values: ["zh-CN"]
)

// 多选
browser_select_option(
  element: "角色选择",
  ref: "e55",
  values: ["Admin", "User"]
)
```

#### 3.5 填写表单 (browser_fill_form)

**用途**：批量填写多个表单字段

**参数**：
- `fields`: 字段数组，每个字段包含：
  - `name`: 字段描述
  - `ref`: 字段引用
  - `type`: 字段类型（`textbox`、`checkbox`、`radio`、`combobox`、`slider`）
  - `value`: 字段值

**使用场景**：
- 快速填写完整表单
- 批量数据输入
- 自动化表单测试

**示例**：
```typescript
browser_fill_form(
  fields: [
    {
      name: "用户名",
      ref: "e37",
      type: "textbox",
      value: "admin"
    },
    {
      name: "密码",
      ref: "e45",
      type: "textbox",
      value: "123@Admin"
    },
    {
      name: "记住登录",
      ref: "e56",
      type: "checkbox",
      value: "true"
    }
  ]
)
```

#### 3.6 拖拽 (browser_drag)

**用途**：拖拽元素到另一个元素

**参数**：
- `startElement`: 源元素描述
- `startRef`: 源元素引用
- `endElement`: 目标元素描述
- `endRef`: 目标元素引用

**使用场景**：
- 拖放排序
- 文件上传
- 可视化编辑器

**示例**：
```typescript
browser_drag(
  startElement: "任务项",
  startRef: "e100",
  endElement: "完成列",
  endRef: "e200"
)
```

### 4. 页面交互工具

#### 4.1 按键 (browser_press_key)

**用途**：模拟键盘按键

**参数**：
- `key`: 键名或字符（如 `Enter`、`Escape`、`ArrowDown`、`a`）

**使用场景**：
- 提交表单（Enter）
- 关闭对话框（Escape）
- 导航（方向键、Tab）
- 快捷键测试

**示例**：
```typescript
// 按 Enter 提交
browser_press_key(key: "Enter")

// 按 Escape 关闭
browser_press_key(key: "Escape")

// 按 Tab 切换焦点
browser_press_key(key: "Tab")

// 向下箭头
browser_press_key(key: "ArrowDown")
```

#### 4.2 等待 (browser_wait_for)

**用途**：等待特定条件或时间

**参数**（三选一）：
- `time`: 等待指定秒数
- `text`: 等待文本出现
- `textGone`: 等待文本消失

**使用场景**：
- 等待页面加载
- 等待异步操作完成
- 等待加载提示消失
- 等待成功消息出现

**示例**：
```typescript
// 等待 3 秒
browser_wait_for(time: 3)

// 等待文本出现
browser_wait_for(text: "登录成功")

// 等待加载提示消失
browser_wait_for(textGone: "正在加载...")
```

#### 4.3 处理对话框 (browser_handle_dialog)

**用途**：处理浏览器对话框（alert、confirm、prompt）

**参数**：
- `accept`: 是否接受对话框（`true` 或 `false`）
- `promptText`（可选）：prompt 对话框的输入文本

**使用场景**：
- 确认删除操作
- 处理警告提示
- 输入 prompt 对话框

**示例**：
```typescript
// 接受确认对话框
browser_handle_dialog(accept: true)

// 拒绝确认对话框
browser_handle_dialog(accept: false)

// 输入并接受 prompt
browser_handle_dialog(
  accept: true,
  promptText: "新名称"
)
```

### 5. 调试工具

#### 5.1 查看控制台消息 (browser_console_messages)

**用途**：获取页面的所有控制台消息

**返回信息**：
- 消息类型（log、info、warning、error）
- 消息内容
- 消息来源

**使用场景**：
- 查找 JavaScript 错误
- 验证日志输出
- 调试前端问题
- 检查 API 调用结果

**示例**：
```typescript
browser_console_messages()
```

**输出示例**：
```
控制台消息：
[Error] Uncaught TypeError: Cannot read property 'id' of undefined
  at UserService.js:45
[Warning] API deprecated: Use /api/v2/users instead
[Log] User login successful
```

**最佳实践**：
- 登录或重要操作后检查控制台
- 关注 Error 和 Warning 消息
- 验证预期的日志是否输出

#### 5.2 查看网络请求 (browser_network_requests)

**用途**：获取页面加载以来的所有网络请求

**返回信息**：
- 请求 URL
- 请求方法（GET、POST 等）
- 响应状态码
- 请求/响应头
- 请求/响应体
- 耗时

**使用场景**：
- 验证 API 调用是否正确
- 检查请求参数
- 分析响应数据
- 性能分析
- 调试网络问题

**示例**：
```typescript
browser_network_requests()
```

**输出示例**：
```
网络请求：
[1] POST https://localhost:7001/api/auth/login
    状态: 200 OK
    耗时: 245ms
    请求体: {"username":"admin","password":"***"}
    响应体: {"token":"eyJ...", "user":{...}}

[2] GET https://localhost:7001/api/users/current
    状态: 200 OK
    耗时: 52ms
    响应体: {"id":1,"name":"Admin",...}
```

**最佳实践**：
- 验证 API 端点是否正确
- 检查请求参数格式
- 确认响应数据结构
- 查找 4xx 或 5xx 错误

#### 5.3 执行 JavaScript (browser_evaluate)

**用途**：在页面或元素上下文中执行 JavaScript 代码

**参数**：
- `function`: JavaScript 函数代码
- `element`（可选）：元素描述
- `ref`（可选）：元素引用

**使用场景**：
- 获取复杂的页面状态
- 执行自定义脚本
- 修改页面内容或样式
- 测试 JavaScript API

**示例**：
```typescript
// 在页面上下文执行
browser_evaluate(
  function: "() => { return document.title; }"
)

// 在元素上下文执行
browser_evaluate(
  element: "输入框",
  ref: "e37",
  function: "(element) => { return element.value; }"
)

// 执行复杂操作
browser_evaluate(
  function: `() => {
    const users = JSON.parse(localStorage.getItem('users'));
    return users.length;
  }`
)
```

**注意事项**：
- 函数必须是完整的箭头函数或普通函数
- 元素上下文时函数接收元素作为第一个参数
- 可以返回数据到外部使用

### 6. 窗口管理工具

#### 6.1 调整窗口大小 (browser_resize)

**用途**：调整浏览器窗口尺寸

**参数**：
- `width`: 宽度（像素）
- `height`: 高度（像素）

**使用场景**：
- 测试响应式设计
- 模拟不同设备尺寸
- 截取特定尺寸的截图

**示例**：
```typescript
// 桌面屏幕
browser_resize(width: 1920, height: 1080)

// 平板
browser_resize(width: 768, height: 1024)

// 手机
browser_resize(width: 375, height: 667)
```

#### 6.2 标签页管理 (browser_tabs)

**用途**：管理浏览器标签页

**参数**：
- `action`: 操作类型
  - `list`: 列出所有标签页
  - `new`: 新建标签页
  - `close`: 关闭标签页
  - `select`: 切换标签页
- `index`（可选）：标签页索引（用于 close 和 select）

**使用场景**：
- 多标签页测试
- 切换不同页面
- 清理测试标签页

**示例**：
```typescript
// 列出所有标签页
browser_tabs(action: "list")

// 新建标签页
browser_tabs(action: "new")

// 切换到第二个标签页
browser_tabs(action: "select", index: 1)

// 关闭当前标签页
browser_tabs(action: "close")

// 关闭指定标签页
browser_tabs(action: "close", index: 2)
```

## 实战案例

### 案例 1: 用户登录测试

**目标**：测试租户登录功能

**步骤**：

```typescript
// 1. 导航到登录页
browser_navigate(url: "https://localhost:7120/default/login")

// 2. 等待页面加载
browser_wait_for(time: 2)

// 3. 获取页面快照
browser_snapshot()
// 从输出中找到用户名输入框 ref: e37，密码输入框 ref: e45，登录按钮 ref: e63

// 4. 填写登录表单
browser_fill_form(
  fields: [
    { name: "用户名", ref: "e37", type: "textbox", value: "admin" },
    { name: "密码", ref: "e45", type: "textbox", value: "123@Admin" }
  ]
)

// 5. 点击登录按钮
browser_click(element: "登录按钮", ref: "e63")

// 6. 等待登录完成
browser_wait_for(time: 3)

// 7. 验证登录成功
browser_snapshot()
// 应该看到管理后台界面

// 8. 检查网络请求
browser_network_requests()
// 验证登录 API 返回 200 状态码

// 9. 检查控制台
browser_console_messages()
// 确保没有错误信息
```

### 案例 2: 用户管理 CRUD 测试

**目标**：测试用户创建、列表、编辑、删除功能

**步骤**：

```typescript
// 1. 导航到用户管理页面
browser_navigate(url: "https://localhost:7120/default/admin#/identity/users")
browser_wait_for(time: 2)

// 2. 点击新建用户按钮
browser_snapshot()
browser_click(element: "新建用户按钮", ref: "e100")

// 3. 填写用户信息
browser_wait_for(text: "新建用户")
browser_snapshot()

browser_fill_form(
  fields: [
    { name: "用户名", ref: "e110", type: "textbox", value: "testuser" },
    { name: "邮箱", ref: "e115", type: "textbox", value: "test@example.com" },
    { name: "姓名", ref: "e120", type: "textbox", value: "测试用户" }
  ]
)

// 4. 提交表单
browser_click(element: "确定按钮", ref: "e150")

// 5. 等待成功提示
browser_wait_for(text: "创建成功")

// 6. 截图记录
browser_take_screenshot(filename: "user-created.png")

// 7. 搜索新创建的用户
browser_type(
  element: "搜索框",
  ref: "e200",
  text: "testuser",
  submit: true
)
browser_wait_for(time: 1)

// 8. 验证用户出现在列表中
browser_snapshot()

// 9. 编辑用户
browser_click(element: "编辑按钮", ref: "e250")
browser_wait_for(text: "编辑用户")

browser_type(
  element: "姓名输入框",
  ref: "e120",
  text: "测试用户（已修改）"
)

browser_click(element: "确定按钮", ref: "e150")
browser_wait_for(text: "更新成功")

// 10. 删除用户
browser_click(element: "删除按钮", ref: "e260")
browser_wait_for(text: "确认删除")

browser_click(element: "确认按钮", ref: "e300")
browser_wait_for(text: "删除成功")

// 11. 验证用户已删除
browser_snapshot()
```

### 案例 3: 表单验证测试

**目标**：测试表单字段验证规则

**步骤**：

```typescript
// 1. 导航到表单页面
browser_navigate(url: "https://localhost:7120/default/admin#/identity/users/create")
browser_wait_for(time: 2)

// 2. 测试必填字段验证
browser_snapshot()

// 不填写任何字段直接提交
browser_click(element: "确定按钮", ref: "e150")

// 验证错误提示出现
browser_wait_for(text: "用户名不能为空")
browser_snapshot()

// 3. 测试邮箱格式验证
browser_type(
  element: "邮箱输入框",
  ref: "e115",
  text: "invalid-email"
)

browser_click(element: "确定按钮", ref: "e150")
browser_wait_for(text: "邮箱格式不正确")

// 4. 测试密码强度验证
browser_type(
  element: "密码输入框",
  ref: "e125",
  text: "123",  // 弱密码
  slowly: true  // 触发实时验证
)

browser_wait_for(text: "密码强度: 弱")
browser_snapshot()

// 5. 填写正确的数据
browser_fill_form(
  fields: [
    { name: "用户名", ref: "e110", type: "textbox", value: "validuser" },
    { name: "邮箱", ref: "e115", type: "textbox", value: "valid@example.com" },
    { name: "密码", ref: "e125", type: "textbox", value: "Strong@Pass123" }
  ]
)

browser_click(element: "确定按钮", ref: "e150")
browser_wait_for(text: "创建成功")
```

### 案例 4: 响应式设计测试

**目标**：测试不同屏幕尺寸下的页面表现

**步骤**：

```typescript
// 1. 桌面端测试
browser_resize(width: 1920, height: 1080)
browser_navigate(url: "https://localhost:7120/default/admin")
browser_wait_for(time: 2)

browser_take_screenshot(
  filename: "desktop-view.png",
  fullPage: true
)

// 验证侧边栏展开
browser_snapshot()
// 应该看到完整的侧边栏菜单

// 2. 平板端测试
browser_resize(width: 768, height: 1024)
browser_wait_for(time: 1)

browser_take_screenshot(
  filename: "tablet-view.png",
  fullPage: true
)

browser_snapshot()
// 侧边栏可能折叠或变为汉堡菜单

// 3. 手机端测试
browser_resize(width: 375, height: 667)
browser_wait_for(time: 1)

browser_take_screenshot(
  filename: "mobile-view.png",
  fullPage: true
)

browser_snapshot()
// 应该看到移动端优化的布局

// 4. 测试移动端菜单
browser_click(element: "菜单按钮", ref: "e10")
browser_wait_for(time: 0.5)
browser_snapshot()
// 验证菜单展开
```

### 案例 5: 性能和错误监控

**目标**：监控页面性能和错误

**步骤**：

```typescript
// 1. 导航到目标页面
browser_navigate(url: "https://localhost:7120/default/admin#/exam/questions")
browser_wait_for(time: 3)

// 2. 检查网络请求
browser_network_requests()

// 分析：
// - 哪些请求耗时最长？
// - 是否有失败的请求（4xx, 5xx）？
// - 是否有重复的请求？
// - 响应体大小是否合理？

// 3. 检查控制台错误
browser_console_messages()

// 查找：
// - JavaScript 错误
// - API 调用失败
// - 警告信息
// - 性能警告

// 4. 执行性能测试
browser_evaluate(
  function: `() => {
    const perf = performance.timing;
    return {
      loadTime: perf.loadEventEnd - perf.navigationStart,
      domReady: perf.domContentLoadedEventEnd - perf.navigationStart,
      firstByte: perf.responseStart - perf.navigationStart
    };
  }`
)

// 5. 测试大数据量加载
browser_click(element: "加载更多按钮", ref: "e200")
browser_wait_for(time: 2)

browser_network_requests()
// 检查请求是否使用分页
// 验证响应时间是否可接受

// 6. 压力测试 - 快速点击
for (let i = 0; i < 10; i++) {
  browser_click(element: "刷新按钮", ref: "e100")
}

browser_wait_for(time: 2)
browser_console_messages()
// 检查是否有错误或重复请求
```

## 最佳实践

### 1. 元素定位策略

**始终使用 snapshot 获取最新的 ref**：
```typescript
// ✅ 正确
browser_snapshot()
// 查看输出，找到目标元素的 ref
browser_click(element: "登录按钮", ref: "e63")

// ❌ 错误
browser_click(element: "登录按钮", ref: "e63")  // 没有先 snapshot
```

**提供清晰的元素描述**：
```typescript
// ✅ 正确
browser_click(element: "用户列表第一行的编辑按钮", ref: "e250")

// ❌ 不够清晰
browser_click(element: "按钮", ref: "e250")
```

### 2. 等待策略

**根据场景选择合适的等待方式**：

```typescript
// 固定等待 - 用于页面加载
browser_wait_for(time: 2)

// 条件等待 - 用于异步操作
browser_wait_for(text: "保存成功")

// 消失等待 - 用于加载提示
browser_wait_for(textGone: "正在加载...")
```

**避免过度等待**：
```typescript
// ❌ 不推荐
browser_wait_for(time: 10)  // 过长

// ✅ 推荐
browser_wait_for(time: 2)
browser_wait_for(text: "加载完成")
```

### 3. 错误处理

**每个关键步骤后验证结果**：

```typescript
// 1. 执行操作
browser_click(element: "保存按钮", ref: "e150")

// 2. 等待结果
browser_wait_for(time: 1)

// 3. 验证成功
browser_snapshot()  // 检查成功提示

// 4. 检查错误
browser_console_messages()  // 查找错误
```

### 4. 测试数据管理

**使用可清理的测试数据**：

```typescript
// 创建测试数据时使用特殊前缀
const testUserName = "test_user_" + Date.now()

browser_type(
  element: "用户名输入框",
  ref: "e110",
  text: testUserName
)

// 测试结束后清理
browser_type(
  element: "搜索框",
  ref: "e200",
  text: "test_user_",
  submit: true
)

// 批量删除测试数据
```

### 5. 截图管理

**关键步骤截图**：

```typescript
// 测试前 - 初始状态
browser_take_screenshot(filename: "01-initial-state.png")

// 测试中 - 关键步骤
browser_take_screenshot(filename: "02-form-filled.png")

// 测试后 - 最终结果
browser_take_screenshot(filename: "03-result.png")
```

### 6. 与 Aspire MCP 工具配合

**完整的测试工作流**：

```typescript
// 1. 检查应用状态（Aspire MCP）
list_resources()
// 确保所有服务运行正常

// 2. 获取端点地址
// 从 list_resources 输出获取 Web 应用 URL

// 3. 开始 Playwright 测试
browser_navigate(url: "https://localhost:7120/...")

// 4. 执行测试...

// 5. 遇到问题时查看后端日志（Aspire MCP）
list_structured_logs(resourceName: "CodeSpirit.IdentityApi")

// 6. 查看分布式追踪（Aspire MCP）
list_traces()

// 7. 对比前后端日志
browser_console_messages()  // 前端错误
list_console_logs(resourceName: "CodeSpirit.IdentityApi")  // 后端日志
```

## 常见问题

### Q1: 找不到元素的 ref

**原因**：
- 页面尚未加载完成
- 元素是动态生成的
- 使用了旧的快照

**解决方案**：
```typescript
// 1. 等待页面加载
browser_wait_for(time: 2)

// 2. 获取最新快照
browser_snapshot()

// 3. 从最新快照中找到 ref
// 4. 如果元素需要交互触发才出现，先执行触发操作
browser_click(element: "展开按钮", ref: "e10")
browser_snapshot()  // 再次获取快照
```

### Q2: 点击没有反应

**可能原因**：
- 元素被遮挡
- 元素被禁用
- 需要等待前置操作完成
- JavaScript 事件未绑定

**解决方案**：
```typescript
// 1. 检查元素状态
browser_snapshot()
// 查看元素是否有 [disabled] 标记

// 2. 等待元素可用
browser_wait_for(time: 1)

// 3. 尝试悬停后再点击
browser_hover(element: "目标元素", ref: "e50")
browser_click(element: "目标元素", ref: "e50")

// 4. 使用 JavaScript 执行点击
browser_evaluate(
  element: "目标元素",
  ref: "e50",
  function: "(el) => el.click()"
)
```

### Q3: 表单提交后没有响应

**排查步骤**：

```typescript
// 1. 检查网络请求
browser_network_requests()
// 查看是否有 API 调用，状态码是什么

// 2. 检查控制台错误
browser_console_messages()
// 查找 JavaScript 错误

// 3. 检查表单验证
browser_snapshot()
// 查看是否有验证错误提示

// 4. 查看后端日志（Aspire MCP）
list_structured_logs(resourceName: "后端服务名")
```

### Q4: 截图显示空白或不完整

**解决方案**：

```typescript
// 1. 等待页面完全加载
browser_wait_for(time: 3)

// 2. 使用 fullPage 参数
browser_take_screenshot(fullPage: true)

// 3. 调整窗口大小
browser_resize(width: 1920, height: 1080)

// 4. 滚动到目标元素
browser_evaluate(
  element: "目标元素",
  ref: "e100",
  function: "(el) => el.scrollIntoView()"
)
browser_take_screenshot()
```

### Q5: 如何测试需要权限的功能

**解决方案**：

```typescript
// 1. 先登录获取权限
browser_navigate(url: "https://localhost:7120/login")
// ... 执行登录 ...

// 2. Cookie 会自动保持，后续请求会携带认证信息

// 3. 测试需要权限的功能
browser_navigate(url: "https://localhost:7120/admin/...")

// 4. 测试不同角色
// 重新登录为不同角色的用户
// 验证权限控制是否正确
```

## 工具对比

### Playwright MCP vs 传统自动化测试

| 特性 | Playwright MCP | 传统 Playwright 脚本 |
|------|----------------|---------------------|
| 编写方式 | 自然语言描述 | 编写代码 |
| 学习曲线 | 低 | 中等 |
| 灵活性 | 高（AI 理解意图） | 高（完全控制） |
| 调试 | 实时反馈 | 需要运行才能看到结果 |
| 维护成本 | 低 | 中等 |
| 适用场景 | 快速验证、探索性测试 | 回归测试、CI/CD |

**建议**：
- 开发阶段使用 Playwright MCP 快速验证
- 稳定后编写传统自动化测试脚本用于 CI/CD

## 总结

Playwright MCP 服务为 CodeSpirit 项目提供了强大的浏览器自动化能力：

✅ **导航和页面检查**：快速访问和分析页面状态  
✅ **元素交互**：点击、输入、悬停、拖拽等完整操作  
✅ **表单处理**：高效的表单填写和验证测试  
✅ **调试工具**：控制台消息、网络请求、JavaScript 执行  
✅ **截图记录**：视觉化记录测试结果  
✅ **窗口管理**：响应式设计测试支持  

配合 Aspire MCP 工具，可以实现前后端一体化的测试和调试工作流，大幅提升开发效率。

## 相关文档

- [Aspire MCP 工具使用指南](./aspire-mcp-tools-guide-zh-CN.md)
- [开发环境搭建指南](../01-Core-Docs/03-development-environment-setup-zh-CN.md)
- [Playwright 官方文档](https://playwright.dev)
