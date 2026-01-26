# 快速启动指南

## 1. 首次设置（仅一次）

```bash
cd Scripts/login-tests
powershell -ExecutionPolicy Bypass -File setup.ps1
```

此命令会自动安装所有必需的工具。

## 2. 启动 Aspire 应用

```bash
cd Src/CodeSpirit.AppHost
aspire run
```

等待所有资源启动完成（约1-2分钟）。

## 3. 运行登录测试

### 系统后台登录

```bash
cd Scripts/login-tests
dotnet script login-system.cs
```

### 租户后台登录

```bash
cd Scripts/login-tests
dotnet script login-tenant.cs
```

## 常见问题

### 问题：Chromium 启动失败

**解决**：重新运行安装脚本

```bash
powershell -ExecutionPolicy Bypass -File setup.ps1
```

### 问题：连接被拒绝

**原因**：Aspire 应用未运行

**解决**：确保 Aspire 应用正在运行并完全启动

### 问题：登录失败

**检查**：
1. 用户名密码是否正确（默认：systemadmin / CodeSpirit@2025）
2. 数据库种子数据是否已初始化
3. 查看 Aspire Dashboard 中的应用日志

## 参数自定义

### 系统后台

```bash
dotnet script login-system.cs -- <webHost> <username> <password> <headless>
```

示例：
```bash
dotnet script login-system.cs -- https://localhost:7120 systemadmin CodeSpirit@2025 false
```

### 租户后台

```bash
dotnet script login-tenant.cs -- <webHost> <tenantId> <username> <password> <headless>
```

示例：
```bash
dotnet script login-tenant.cs -- https://localhost:7120 default admin 123@Admin false
```

## 更多信息

详细文档请参考 [README.md](README.md)
