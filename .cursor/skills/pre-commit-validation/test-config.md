# 测试配置文档

本文档包含提交前验证技能使用的测试配置信息。

## 默认测试配置

```yaml
# 系统管理员配置
system_username: "systemadmin"
system_password: "CodeSpirit@2025"

# 测试租户配置
test_tenant_id: "default"
test_username: "admin"
test_password: "123@Admin"

# 应用端点配置
web_host: "https://localhost:7120"
system_login_url: "https://localhost:7120/login"
tenant_login_url: "https://localhost:7120/{tenant_id}/login"
tenant_admin_url: "https://localhost:7120/{tenant_id}/admin"

# Aspire 配置
aspire_dashboard_url: "http://localhost:15888"

# 登录测试脚本路径
login_scripts_path: "Scripts/login-tests"
system_login_script: "login-system.cs"
tenant_login_script: "login-tenant.cs"
```

## 配置说明

### 系统管理员配置

**system_username**
- 类型：字符串
- 说明：系统管理员用户名
- 默认值：`"systemadmin"`
- 数据来源：`Src/ApiServices/CodeSpirit.IdentityApi/Data/Seeders/UnifiedUserSeederService.cs`（第196行）

**system_password**
- 类型：字符串
- 说明：系统管理员密码
- 默认值：`"CodeSpirit@2025"`
- ⚠️ **安全警告**：这是种子数据中的默认密码

### 测试租户配置

**test_tenant_id**
- 类型：字符串
- 说明：用于测试的租户标识符
- 默认值：`"default"`
- 修改方式：根据实际测试环境修改

**test_username**
- 类型：字符串
- 说明：租户管理员用户名
- 默认值：`"admin"`
- 数据来源：`Src/ApiServices/CodeSpirit.IdentityApi/Data/Seeders/UserSeeder.cs`（第53行）

**test_password**
- 类型：字符串
- 说明：租户管理员密码
- 默认值：`"123@Admin"`
- ⚠️ **安全警告**：这是种子数据中的默认密码
- 数据来源：`Src/ApiServices/CodeSpirit.IdentityApi/Data/Seeders/UnifiedUserSeederService.cs`（第216行）

### 应用端点配置

**web_host**
- 类型：URL 字符串
- 说明：Web 应用的主机地址和端口
- 默认值：`"https://localhost:7120"`（HTTPS）
- HTTP 端口：`"http://localhost:5145"`
- 修改方式：
  - 开发环境：查看 Aspire Dashboard 中 Web 资源的端口
  - 测试环境：根据实际部署地址修改

**system_login_url**
- 类型：URL 字符串
- 说明：系统管理平台登录页面 URL
- 格式：`{web_host}/login`
- 示例：`https://localhost:7120/login`

**tenant_login_url**
- 类型：URL 模板字符串
- 说明：租户登录页面的 URL 模板
- 格式：`{web_host}/{tenant_id}/login`
- 示例：`https://localhost:7120/default/login`

**tenant_admin_url**
- 类型：URL 模板字符串
- 说明：租户管理后台的 URL 模板
- 格式：`{web_host}/{tenant_id}/admin`
- 示例：`https://localhost:7120/default/admin`

### Aspire 配置

**aspire_dashboard_url**
- 类型：URL 字符串
- 说明：Aspire Dashboard 的访问地址
- 默认值：`"http://localhost:15888"`
- 用途：手动查看资源状态和日志

### 登录测试脚本配置

**login_scripts_path**
- 类型：相对路径字符串
- 说明：登录测试脚本所在目录
- 默认值：`"Scripts/login-tests"`
- 相对于项目根目录

**system_login_script**
- 类型：文件名字符串
- 说明：系统后台登录脚本文件名
- 默认值：`"login-system.cs"`
- 完整路径：`Scripts/login-tests/login-system.cs`

**tenant_login_script**
- 类型：文件名字符串
- 说明：租户后台登录脚本文件名
- 默认值：`"login-tenant.cs"`
- 完整路径：`Scripts/login-tests/login-tenant.cs`

## 环境特定配置

### 开发环境

```yaml
web_host: "https://localhost:7120"
test_tenant_id: "default"
system_username: "systemadmin"
system_password: "CodeSpirit@2025"
test_username: "admin"
test_password: "123@Admin"
```

### 测试环境

```yaml
web_host: "https://test.example.com"
test_tenant_id: "test-tenant"
system_username: "systemadmin"
test_username: "admin"
# 密码应从环境变量或密钥管理系统获取
```

### 生产环境

⚠️ **注意**：不建议在生产环境运行自动化测试

## 配置方式

### 方式1：修改配置文件

直接编辑 `test-config.md` 文件，修改相应的配置值。

### 方式2：环境变量（如果支持）

```bash
# Windows PowerShell
$env:TEST_TENANT_ID = "my-tenant"
$env:TEST_USERNAME = "admin"
$env:TEST_PASSWORD = "MyPassword123"

# Linux/Mac
export TEST_TENANT_ID="my-tenant"
export TEST_USERNAME="admin"
export TEST_PASSWORD="MyPassword123"
```

### 方式3：技能参数覆盖

在执行验证技能时，可以通过参数指定配置：

```
请使用租户ID "my-tenant" 和用户名 "admin" 进行验证
```

## 安全注意事项

### 密码管理

1. **不要提交真实密码**
   - 使用占位符或示例密码
   - 真实密码通过环境变量或密钥管理工具配置

2. **使用测试账户**
   - 创建专用的测试账户
   - 测试账户权限应足够但不包含敏感操作

3. **定期更换密码**
   - 测试账户密码应定期更换
   - 避免使用生产环境密码

### 敏感信息保护

- ✅ **可以提交**：占位符、示例配置、URL 模板
- ❌ **不要提交**：真实密码、生产环境配置、API 密钥

## 配置验证

在执行验证前，确认以下配置：

- [ ] 测试租户 ID 有效且可访问
- [ ] 测试账户存在且密码正确
- [ ] Web 主机地址可访问
- [ ] 登录 URL 格式正确
- [ ] 管理后台 URL 格式正确

## 常见问题

### Q: 如何获取测试租户 ID？

A: 可以通过以下方式获取：
1. 查看 Aspire Dashboard 中的资源列表
2. 检查数据库中的租户表
3. 询问项目管理员

### Q: 测试账户密码忘记了怎么办？

A: 
1. 重置测试账户密码
2. 或创建新的测试账户
3. 更新配置文件中的密码

### Q: 如何测试不同的租户？

A: 
1. 修改 `test_tenant_id` 配置
2. 或通过技能参数指定租户 ID
3. 确保该租户有可用的测试账户

### Q: Web 主机端口不是 7120 怎么办？

A: 
1. 检查 Aspire Dashboard 中 Web 资源的实际端口
2. 修改 `web_host` 配置中的端口号
3. 或通过环境变量覆盖
4. 注意区分 HTTP 端口（通常 5xxx）和 HTTPS 端口（通常 7xxx）

### Q: 如何运行登录测试脚本？

A:
1. 首次使用需要安装依赖：
   ```bash
   cd Scripts/login-tests
   powershell -ExecutionPolicy Bypass -File setup.ps1
   ```
2. 运行系统后台登录测试：
   ```bash
   dotnet script login-system.cs
   ```
3. 运行租户后台登录测试：
   ```bash
   dotnet script login-tenant.cs
   ```
4. 使用自定义配置：
   ```bash
   dotnet script login-system.cs -- https://localhost:7120 systemadmin CodeSpirit@2025 true
   ```

## 配置示例

### 示例1：使用默认配置

```yaml
system_username: "systemadmin"
system_password: "CodeSpirit@2025"
test_tenant_id: "default"
test_username: "admin"
test_password: "123@Admin"
web_host: "https://localhost:7120"
```

### 示例2：使用自定义租户

```yaml
system_username: "systemadmin"
system_password: "CodeSpirit@2025"
test_tenant_id: "acme-corp"
test_username: "corp-admin"
test_password: "CorpAdmin@123"
web_host: "https://localhost:7120"
```

### 示例3：使用 HTTP 端口

```yaml
system_username: "systemadmin"
system_password: "CodeSpirit@2025"
test_tenant_id: "default"
test_username: "admin"
test_password: "123@Admin"
web_host: "http://localhost:5145"
```

### 示例4：运行脚本命令

```bash
# 系统后台登录（使用默认配置）
dotnet script login-system.cs

# 系统后台登录（自定义配置）
dotnet script login-system.cs -- https://localhost:7120 systemadmin CodeSpirit@2025 true

# 租户后台登录（使用默认配置）
dotnet script login-tenant.cs

# 租户后台登录（自定义配置）
dotnet script login-tenant.cs -- https://localhost:7120 default admin 123@Admin true
```

## 登录脚本详细说明

### 脚本特点

1. **单文件执行**：使用 `dotnet script` 运行，无需项目文件
2. **自动化测试**：完整的登录流程自动化
3. **详细输出**：提供步骤日志和错误诊断
4. **无头模式**：支持无界面运行，适合 CI/CD

### 脚本输出

**成功示例**：
```
============================================================
CodeSpirit 系统后台登录自动化
============================================================
Web Host: https://localhost:7120
Username: systemadmin
Headless: True
============================================================

[1/7] 初始化 Playwright...
[2/7] 启动 Chromium 浏览器...
[3/7] 创建浏览器上下文...
[4/7] 导航到系统登录页面: https://localhost:7120/login
    ✓ 页面加载成功，状态码: 200
    ✓ 页面标题: 登录 - CodeSpirit
[5/7] 填充登录表单...
    ✓ 用户名已填充: systemadmin
    ✓ 密码已填充: ***************
[6/7] 提交登录表单...
[7/7] 等待登录完成...

============================================================
✅ 登录成功！
============================================================
当前 URL: https://localhost:7120/#/
页面标题: 管理后台 - CodeSpirit
```

**失败示例**：
```
============================================================
❌ 登录失败或超时
============================================================
当前 URL: https://localhost:7120/login
页面标题: 登录 - CodeSpirit

🔍 可能的原因：
   1. 用户名或密码不正确
   2. 账户被锁定或禁用
   3. 网络连接问题
   4. 应用服务未正常运行
```

### 首次安装

运行脚本前需要安装依赖（仅需一次）：

```bash
cd Scripts/login-tests
powershell -ExecutionPolicy Bypass -File setup.ps1
```

此脚本会自动安装：
- dotnet-script 工具
- Microsoft.Playwright.CLI 工具
- Chromium 浏览器

## 更新日志

- **2026-01-26**：
  - 初始版本，包含基本测试配置
  - 添加登录测试脚本配置
  - 更新默认端口为 7120（HTTPS）
  - 添加系统管理员配置
  - 添加脚本使用说明
