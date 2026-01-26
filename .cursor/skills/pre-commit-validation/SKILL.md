---
name: pre-commit-validation
description: 提交前综合验证审阅。执行静态代码审查、运行时错误检查和功能验证。包括代码质量检查、Aspire应用日志分析、租户后台登录测试。当用户准备提交代码、需要全面验证、或要求提交前审查时使用。
---

# 提交前验证审阅技能

## 快速开始

本技能在代码提交前执行三层验证，确保代码质量和系统稳定性：

1. **静态代码审查**：使用现有 code-review 技能检查代码规范
2. **运行时验证**：使用 Aspire MCP 工具检查应用日志和资源状态
3. **功能验证**：使用 Playwright MCP 工具测试租户后台登录功能

### 前置条件

- Aspire 应用已启动（或可以启动）
- 待提交的代码已通过 `git add` 暂存
- MCP 工具可用（Aspire MCP 和 Playwright MCP）

---

## 验证工作流程

### 阶段1：静态代码审查

**目标**：检查代码规范、安全性和最佳实践

**步骤**：

1. **获取待提交文件列表**
   ```bash
   git status --short
   ```

2. **调用代码审查技能**
   - 参考现有的 [code-review](../code-review/SKILL.md) 技能
   - 对修改和新文件执行全面审查
   - 重点关注：
     - 🔴 严重级别：安全、数据库、异步编程
     - 🟡 重要级别：多语言、DTO、控制器、服务类
     - 🟢 建议级别：注释、命名、性能优化

3. **生成审查报告**
   - 统计问题数量和严重程度
   - 列出需要修复的问题
   - 提供修复建议

---

### 阶段2：运行时验证

**目标**：检查应用运行状态和错误日志

**步骤**：

1. **检查 Aspire 应用状态**
   - 使用 `list_resources` MCP 工具获取资源列表
   - 检查每个资源的 `state` 字段：
     - `Running`：正常运行
     - `Failed`：启动失败（需要检查日志）
     - `Starting`：启动中（等待后重试）
     - `Stopped`：已停止（需要启动应用）

2. **如果应用未运行**
   - 提示用户运行 `aspire run`
   - 等待应用启动完成（建议等待 30-60 秒）
   - 重新检查资源状态

3. **检查控制台日志**
   - 对每个资源使用 `list_console_logs` 工具
   - 参数：`resourceName` - 资源名称
   - 重点关注：
     - 启动错误
     - 配置加载失败
     - 依赖注入错误

4. **检查结构化日志**
   - 使用 `list_structured_logs` 工具
   - 筛选 Warning 和 Error 级别日志
   - 识别关键错误模式：
     - `Database connection failed`
     - `Unhandled exception`
     - `Dependency injection failed`
     - `Configuration loading error`

5. **分析日志内容**
   - 统计 Error 和 Warning 数量
   - 提取关键错误信息
   - 识别需要立即修复的问题

---

### 阶段3：功能验证

**目标**：验证系统后台和租户后台登录功能正常

**推荐方式：使用登录测试脚本**（更快速、更可靠）

**步骤**：

1. **准备测试配置**
   - 读取 [test-config.md](test-config.md) 获取测试凭证
   - 确认 Web 主机地址和端口
   - 默认配置：
     - Web Host: `https://localhost:7120`
     - 系统管理员: `systemadmin / CodeSpirit@2025`
     - 租户管理员: `admin / 123@Admin`（租户ID: `default`）

2. **运行系统后台登录测试**
   ```bash
   cd Scripts/login-tests
   dotnet script login-system.cs -- <webHost> <username> <password> true
   ```
   - 示例：`dotnet script login-system.cs -- https://localhost:7120 systemadmin CodeSpirit@2025 true`
   - 参数说明：
     - 参数1: Web Host
     - 参数2: 用户名
     - 参数3: 密码
     - 参数4: Headless 模式（true=无头模式，不显示浏览器）

3. **运行租户后台登录测试**
   ```bash
   cd Scripts/login-tests
   dotnet script login-tenant.cs -- <webHost> <tenantId> <username> <password> true
   ```
   - 示例：`dotnet script login-tenant.cs -- https://localhost:7120 default admin 123@Admin true`
   - 参数说明：
     - 参数1: Web Host
     - 参数2: 租户 ID
     - 参数3: 用户名
     - 参数4: 密码
     - 参数5: Headless 模式

4. **检查测试结果**
   - 脚本退出码：
     - `0`：登录成功
     - `1`：登录失败或脚本执行错误
   - 输出信息：
     - ✅ 登录成功：显示当前 URL 和页面标题
     - ❌ 登录失败：显示错误原因和排查建议

5. **验证关键功能**
   - 系统后台：URL 应跳转到管理后台，标题包含"管理后台"
   - 租户后台：URL 应跳转到 `/{tenantId}/admin`，标题包含租户信息

**备用方式：使用 Playwright MCP 工具**

如果登录脚本不可用，可以使用 MCP 工具：

1. 导航到登录页：`browser_navigate`
2. 填充登录表单：`browser_fill_form`
3. 点击登录按钮：`browser_click`
4. 验证页面跳转：`browser_snapshot`

---

## 测试配置

测试配置请参考 [test-config.md](test-config.md)。

**默认配置**：
- 测试租户 ID：`default`
- 测试用户名：`admin`
- 测试密码：`Admin@123`（请根据实际情况修改）
- Web 主机：`http://localhost:5000`

**配置方式**：
1. 修改 `test-config.md` 文件
2. 或通过环境变量覆盖（如果支持）

---

## 验证检查清单

详细的检查清单请参考 [validation-checklist.md](validation-checklist.md)。

**快速检查项**：

- [ ] 代码审查无严重问题
- [ ] 所有 API 服务成功启动
- [ ] 无 Error 级别日志
- [ ] 租户后台可以正常登录
- [ ] 登录后管理页面可访问

---

## 报告生成

验证完成后，使用 [report-template.md](report-template.md) 生成综合验证报告。

**报告包含**：
1. 执行摘要（各阶段状态统计）
2. 静态代码审查结果
3. 运行时验证结果（资源状态、错误日志）
4. 功能验证结果（登录测试）
5. 修复建议
6. 下一步行动

---

## 故障排查

### MCP 工具不可用

如果 MCP 工具调用失败：

1. **检查 MCP 服务器状态**
   - 确认 Aspire MCP 和 Playwright MCP 已配置
   - 检查 MCP 服务器是否正常运行

2. **手动验证步骤**
   - 手动运行 `aspire run` 启动应用
   - 访问 Aspire Dashboard 查看资源状态
   - 手动打开浏览器测试登录功能

### 应用启动失败

如果应用无法启动：

1. **检查资源日志**
   - 使用 `list_console_logs` 查看失败资源的详细日志
   - 查找启动错误信息

2. **常见问题**：
   - 端口被占用：检查端口冲突
   - 数据库连接失败：检查数据库配置和连接字符串
   - 依赖注入错误：检查服务注册配置

### 登录测试失败

如果登录测试失败：

1. **检查页面加载**
   - 确认登录页面 URL 正确
   - 验证页面元素是否存在

2. **检查表单字段**
   - 确认字段选择器正确
   - 验证表单字段名称匹配

3. **检查网络请求**
   - 查看浏览器控制台网络请求
   - 确认登录 API 调用是否成功

---

## 使用示例

### 触发方式

用户可以通过以下方式触发技能：

- "请验证我的代码是否可以提交"
- "提交前检查"
- "运行提交前验证"
- "全面审查待提交的代码"
- "验证代码并检查错误日志"

### 执行流程

1. **自动执行阶段1**：代码审查
2. **询问用户**：是否继续阶段2（运行时验证）
3. **如果阶段2通过**：询问是否继续阶段3（功能验证）
4. **生成报告**：汇总所有验证结果

### 渐进式验证

如果某个阶段失败：

- **阶段1失败**：询问是否修复后继续，或跳过后续阶段
- **阶段2失败**：提供错误详情，询问是否继续功能验证
- **阶段3失败**：记录失败原因，不影响整体评估

---

## 相关资源

- [代码审查技能](../code-review/SKILL.md)
- [测试配置文档](test-config.md)
- [验证检查清单](validation-checklist.md)
- [报告模板](report-template.md)
- [Aspire 官方文档](https://aspire.dev)

---

## 注意事项

1. **敏感信息保护**：测试密码不要提交到代码库，使用占位符
2. **MCP 工具错误处理**：如果工具不可用，提供手动验证步骤
3. **报告简洁性**：日志输出可能很长，只包含关键错误摘要
4. **跨平台兼容**：路径和命令兼容 Windows 和 Unix 系统
5. **性能考虑**：验证过程可能需要几分钟，请耐心等待
