# CodeSpirit 登录自动化脚本

使用 .NET 10 和 Playwright 实现的自动化登录测试脚本。

## 脚本列表

### 1. login-system.cs - 系统后台登录

自动化登录 CodeSpirit 系统管理平台。

**默认凭证**：
- 用户名：`systemadmin`
- 密码：`CodeSpirit@2025`
- 登录 URL：`{webHost}/login`
- 成功跳转：`{webHost}/admin`

### 2. login-tenant.cs - 租户后台登录

自动化登录 CodeSpirit 租户管理后台。

**默认凭证**：
- 租户 ID：`default`
- 用户名：`admin`
- 密码：`123@Admin`
- 登录 URL：`{webHost}/{tenantId}/login`
- 成功跳转：`{webHost}/{tenantId}/admin`

---

## 快速开始

### 前置条件

1. **安装 .NET 10 SDK**
   ```bash
   # 验证安装
   dotnet --version
   ```

2. **首次设置**（仅需执行一次）
   
   运行安装脚本：
   ```bash
   cd Scripts/login-tests
   powershell -ExecutionPolicy Bypass -File setup.ps1
   ```
   
   此脚本将自动安装：
   - dotnet-script 工具
   - Microsoft.Playwright.CLI 工具
   - Chromium 浏览器

3. **启动 Aspire 应用**
   ```bash
   cd Src/CodeSpirit.AppHost
   aspire run
   ```

### 运行脚本

#### 系统后台登录

```bash
# 使用默认配置
dotnet script login-system.cs

# 使用自定义配置
dotnet script login-system.cs -- https://localhost:7120 systemadmin CodeSpirit@2025

# 无头模式（不显示浏览器）
dotnet script login-system.cs -- https://localhost:7120 systemadmin CodeSpirit@2025 true
```

**参数说明**：
- 参数1: Web Host（默认: `https://localhost:7120`）
- 参数2: 用户名（默认: `systemadmin`）
- 参数3: 密码（默认: `CodeSpirit@2025`）
- 参数4: Headless 模式（默认: `false`）

#### 租户后台登录

```bash
# 使用默认配置（default 租户）
dotnet script login-tenant.cs

# 使用自定义租户
dotnet script login-tenant.cs -- https://localhost:7120 mytenant admin MyPass@123

# 无头模式（不显示浏览器）
dotnet script login-tenant.cs -- https://localhost:7120 default admin 123@Admin true
```

**参数说明**：
- 参数1: Web Host（默认: `https://localhost:7120`）
- 参数2: 租户 ID（默认: `default`）
- 参数3: 用户名（默认: `admin`）
- 参数4: 密码（默认: `123@Admin`）
- 参数5: Headless 模式（默认: `false`）

---

## 工作流程

### 系统后台登录流程

1. 初始化 Playwright 和浏览器
2. 导航到 `/login` 页面
3. 等待页面加载完成
4. 填充用户名和密码
5. 按 Enter 键提交表单
6. 等待 URL 跳转到 `/admin`（最多15秒）
7. 验证登录成功并输出结果

### 租户后台登录流程

1. 初始化 Playwright 和浏览器
2. 导航到 `/{tenantId}/login` 页面
3. 等待租户信息和页面加载完成
4. 填充用户名和密码
5. 按 Enter 键提交表单
6. 等待 URL 跳转到 `/{tenantId}/admin`（最多15秒）
7. 验证登录成功并输出结果

---

## 验证登录状态

脚本成功后，可以使用 Playwright MCP 工具进一步验证页面状态：

### 使用 Cursor 的 MCP 工具

在 Cursor 中调用 Playwright MCP 工具：

```
服务器: cursor-browser-extension
工具: browser_snapshot
说明: 获取当前页面快照，验证管理后台元素是否正常显示
```

这将帮助验证：
- 页面元素是否正确加载
- 导航菜单是否可见
- 用户信息是否显示
- 是否有 JavaScript 错误

---

## 输出示例

### 成功输出

```
============================================================
CodeSpirit 租户后台登录自动化
============================================================
Web Host: https://localhost:7120
Tenant ID: default
Username: admin
Headless: False
============================================================

[1/8] 初始化 Playwright...
[2/8] 启动 Chromium 浏览器...
[3/8] 创建浏览器上下文...
[4/8] 导航到租户登录页面: https://localhost:7120/default/login
    ✓ 页面加载成功，状态码: 200
    ✓ 页面标题: 租户登录 - CodeSpirit
[5/8] 等待租户信息加载...
    ✓ 租户信息加载完成
[6/8] 填充登录表单...
    ✓ 用户名已填充: admin
    ✓ 密码已填充: *********
[7/8] 提交登录表单...
[8/8] 等待登录完成...

============================================================
✅ 登录成功！
============================================================
租户 ID: default
当前 URL: https://localhost:7120/default/admin
页面标题: 管理后台 - CodeSpirit

💡 后续验证建议：
   使用 Playwright MCP 工具获取页面快照验证登录状态：
   - 服务器: cursor-browser-extension
   - 工具: browser_snapshot
   - 说明: 验证管理后台页面元素是否正常加载

按 Enter 键关闭浏览器...
```

### 失败输出

```
============================================================
❌ 登录失败或超时
============================================================
租户 ID: default
当前 URL: https://localhost:7120/default/login
页面标题: 租户登录 - CodeSpirit

⚠️ 检测到错误消息：
   - 用户名或密码不正确

🔍 可能的原因：
   1. 用户名或密码不正确
   2. 租户不存在或已禁用
   3. 账户被锁定
   4. 网络连接问题

📝 默认凭证（参考种子数据）：
   - 租户ID: default
   - 用户名: admin
   - 密码: 123@Admin
   - 来源: Src/ApiServices/CodeSpirit.IdentityApi/Data/Seeders/
           - UserSeeder.cs (第53行)
           - UnifiedUserSeederService.cs (第216行)
```

---

## 故障排查

### 1. 工具未安装

**错误**：`Playwright driver is not installed` 或命令找不到

**解决方案**：
```bash
# 运行安装脚本
cd Scripts/login-tests
powershell -ExecutionPolicy Bypass -File setup.ps1
```

### 2. 页面无法访问

**错误**：`net::ERR_CONNECTION_REFUSED` 或 `HTTP 404`

**解决方案**：
1. 确认 Aspire 应用正在运行：
   ```bash
   cd Src/CodeSpirit.AppHost
   aspire run
   ```
2. 确认 Web Host 地址正确（检查 Aspire Dashboard 中的端口）
3. 等待所有资源启动完成（约1-2分钟）

### 3. 登录失败

**错误**：URL 未跳转到 `/admin`

**可能原因**：
1. **密码错误**：
   - 系统后台：检查是否为 `CodeSpirit@2025`
   - 租户后台：检查是否为 `123@Admin`（不是 `Admin@123`）
   
2. **账户不存在**：
   - 检查数据库是否初始化种子数据
   - 查看 IdentityApi 的启动日志
   
3. **租户不存在**：
   - 确认租户 ID 正确
   - 检查租户是否已启用

### 4. SSL 证书错误

**错误**：`net::ERR_CERT_AUTHORITY_INVALID`

**解决方案**：
- 脚本已配置忽略证书错误（`IgnoreHTTPSErrors = true`）
- 如果仍有问题，尝试使用 HTTP 端口而非 HTTPS

### 5. 元素未找到

**错误**：`Timeout waiting for selector`

**解决方案**：
1. 确认页面已完全加载
2. 检查表单元素选择器是否正确
3. 增加等待超时时间（修改脚本中的 Timeout 参数）

---

## 脚本特点

### 优点

1. **单文件执行**：使用 .NET 10 的 `#:package` 指令，无需创建项目
2. **可配置**：支持命令行参数自定义配置
3. **详细日志**：提供完整的执行过程输出
4. **错误处理**：包含完善的异常处理和故障排查提示
5. **跨平台**：支持 Windows、Linux、macOS
6. **MCP 集成**：提供与 Playwright MCP 工具配合使用的提示

### 与 MCP 工具配合

1. **脚本负责**：自动化登录流程，验证基本登录功能
2. **MCP 工具负责**：深度验证页面状态，检查元素加载情况
3. **优势**：脚本可独立运行，MCP 工具提供额外验证能力

---

## 配置说明

### 端口配置

根据实际 Aspire 应用端口调整 Web Host：

```bash
# HTTP 端口（通常是 5000-5999）
dotnet run login-system.cs http://localhost:5145

# HTTPS 端口（通常是 7000-7999）
dotnet run login-system.cs https://localhost:7120
```

### 密码来源

脚本中的默认密码来自种子数据文件：

- **系统管理员**：[`Src/ApiServices/CodeSpirit.IdentityApi/Data/Seeders/UnifiedUserSeederService.cs`](../../Src/ApiServices/CodeSpirit.IdentityApi/Data/Seeders/UnifiedUserSeederService.cs)（第196行）
- **租户管理员**：[`Src/ApiServices/CodeSpirit.IdentityApi/Data/Seeders/UserSeeder.cs`](../../Src/ApiServices/CodeSpirit.IdentityApi/Data/Seeders/UserSeeder.cs)（第53行）和 [`UnifiedUserSeederService.cs`](../../Src/ApiServices/CodeSpirit.IdentityApi/Data/Seeders/UnifiedUserSeederService.cs)（第216行）

---

## 与其他测试工具的关系

### pre-commit-validation 技能

提交前验证技能可以：
1. 先运行这些脚本确认登录功能正常
2. 然后使用 MCP 工具进行深度页面验证

### 手动测试

如果脚本失败，可以：
1. 查看脚本输出的错误信息
2. 手动访问登录页面验证
3. 检查应用日志排查问题

---

## 技术实现

### dotnet-script

脚本使用 `dotnet-script` 工具，支持单文件 C# 脚本执行。特点：

- **NuGet 包引用**：使用 `#r "nuget: PackageName, Version"` 语法
- **参数访问**：使用大写的 `Args` 集合（不是小写的 `args`）
- **运行方式**：`dotnet script file.cs -- arg1 arg2`（注意 `--` 分隔符）

### .NET 10 序列化支持

脚本包含序列化配置以支持 Playwright：

```csharp
AppContext.SetSwitch("System.Text.Json.JsonSerializer.IsReflectionEnabledByDefault", true);
```

这解决了 .NET 10 默认禁用反射序列化的兼容性问题。

### 登录成功检测

脚本通过以下方式检测登录成功：

1. **URL 变化**：检测 URL 是否从 `/login` 跳转走
2. **标题检查**：验证页面标题不再包含"登录"字样
3. **灵活适配**：不依赖固定的目标 URL（如 `/admin`），适应实际的路由结构

## 文件说明

- **login-system.cs**：系统后台登录脚本
- **login-tenant.cs**：租户后台登录脚本
- **setup.ps1**：环境安装脚本（安装 dotnet-script、Playwright CLI、Chromium）
- **README.md**：本文档

## 更新日志

- **2026-01-26**：
  - 初始版本，支持系统后台和租户后台登录
  - 使用 dotnet-script 实现单文件脚本
  - 添加 .NET 10 序列化支持
  - 改进登录成功检测逻辑
  - 添加自动化安装脚本
