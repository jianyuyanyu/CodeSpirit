# CodeSpirit.PdfGeneration - PuppeteerSharp 问题解决指南

## 概述

本文档介绍 CodeSpirit.PdfGeneration 组件使用 PuppeteerSharp 时可能遇到的问题及解决方案。

## 常见问题

### 1. ICU 数据文件错误 (Windows)

**错误信息：**
```
PuppeteerSharp.ProcessException: Failed to launch browser!
[1002/203109.989:ERROR:icu_util.cc(240)] Invalid file descriptor to ICU data received.
```

**问题原因：**
- Chromium 浏览器的 ICU（International Components for Unicode）数据文件无法正确加载
- Windows 环境下的权限或路径访问问题
- 浏览器下载不完整或损坏

**解决方案：**

#### 方案 1：使用清理脚本（推荐）

运行 PowerShell 清理脚本：
```powershell
.\Scripts\clean-puppeteer-browser.ps1
```

该脚本会：
1. 清理 PuppeteerSharp 的浏览器缓存（`~/.local-chromium`）
2. 清理临时用户数据目录
3. 允许应用程序重新下载干净的 Chromium 浏览器

#### 方案 2：手动清理

手动删除以下目录：
- `%USERPROFILE%\.local-chromium`
- `%TEMP%\puppeteer_dev_chrome_profile`

然后重新启动应用程序。

#### 方案 3：指定 Chrome 路径

在 `appsettings.json` 中配置已安装的 Chrome/Chromium 路径：
```json
{
  "PdfGeneration": {
    "ExecutablePath": "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe"
  }
}
```

或设置环境变量：
```powershell
$env:PUPPETEER_EXECUTABLE_PATH = "C:\Program Files\Google\Chrome\Application\chrome.exe"
```

## 已实施的优化

### 浏览器启动参数优化

已添加以下 Windows 特定的启动参数：

```csharp
BrowserArguments = new[]
{
    "--no-sandbox",                                    // 禁用沙箱
    "--disable-setuid-sandbox",                        // 禁用 setuid 沙箱
    "--disable-dev-shm-usage",                         // 禁用 /dev/shm 使用
    "--disable-gpu",                                   // 禁用 GPU 加速
    "--no-first-run",                                  // 跳过首次运行
    "--no-zygote",                                     // 禁用 zygote 进程
    "--single-process",                                // 单进程模式
    "--disable-features=RendererCodeIntegrity",        // Windows ICU 问题修复
    "--disable-blink-features=AutomationControlled",   // 禁用自动化控制检测
    "--disable-features=IsolateOrigins,site-per-process", // 禁用站点隔离
    "--disable-web-security"                           // 禁用 Web 安全策略
};
```

### 用户数据目录管理

在 Windows 环境下自动创建临时用户数据目录：
```csharp
var userDataDir = Path.Combine(Path.GetTempPath(), "puppeteer_dev_chrome_profile");
args.Add($"--user-data-dir={userDataDir}");
```

### 浏览器下载重试机制

实现了带重试机制的浏览器下载逻辑：
- 最多重试 3 次
- 每次重试间隔 2 秒
- 自动检测已下载的浏览器版本

## 配置选项

### 基本配置

在 `appsettings.json` 中配置 PDF 生成选项：

```json
{
  "PdfGeneration": {
    "MaxConcurrentJobs": 5,           // 最大并发任务数
    "BrowserPoolSize": 3,             // 浏览器池大小
    "BrowserTimeout": "00:02:00",     // 浏览器超时时间
    "Headless": true,                 // 无头模式
    "RetryCount": 3,                  // 重试次数
    "RetryDelay": "00:00:01",         // 重试延迟
    "BrowserMemoryLimit": 512,        // 浏览器内存限制（MB）
    "ExecutablePath": null,           // 浏览器可执行文件路径（可选）
    "BrowserArguments": [             // 自定义浏览器参数（可选）
      "--no-sandbox",
      "--disable-gpu"
    ]
  }
}
```

### 环境变量

支持以下环境变量：

| 环境变量 | 说明 | 示例 |
|---------|------|------|
| `PUPPETEER_EXECUTABLE_PATH` | Chrome/Chromium 可执行文件路径 | `C:\Program Files\Google\Chrome\Application\chrome.exe` |

## 最佳实践

### 开发环境

1. **首次使用**：让 PuppeteerSharp 自动下载 Chromium（无需配置）
2. **遇到问题**：运行清理脚本重新下载
3. **网络受限**：手动下载 Chromium 或使用本地 Chrome

### 生产环境

#### Docker 容器

Dockerfile 示例：
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0

# 安装 Chromium 依赖
RUN apt-get update && apt-get install -y \
    chromium \
    fonts-liberation \
    libasound2 \
    libatk-bridge2.0-0 \
    libatk1.0-0 \
    libatspi2.0-0 \
    libcups2 \
    libdbus-1-3 \
    libdrm2 \
    libgbm1 \
    libgtk-3-0 \
    libnspr4 \
    libnss3 \
    libxcomposite1 \
    libxdamage1 \
    libxfixes3 \
    libxkbcommon0 \
    libxrandr2 \
    xdg-utils \
    && rm -rf /var/lib/apt/lists/*

# 设置 Chromium 路径
ENV PUPPETEER_EXECUTABLE_PATH=/usr/bin/chromium

# 复制应用程序
COPY . /app
WORKDIR /app

ENTRYPOINT ["dotnet", "YourApp.dll"]
```

#### Windows Server

1. 安装 Google Chrome
2. 在 `appsettings.Production.json` 中配置路径：
```json
{
  "PdfGeneration": {
    "ExecutablePath": "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe"
  }
}
```

## 故障排查

### 问题诊断步骤

1. **检查日志**
   ```
   正在初始化浏览器池...
   开始下载 Chromium 浏览器...
   Chromium 浏览器下载完成，版本: XXX
   ```

2. **验证浏览器缓存**
   - Windows: `%USERPROFILE%\.local-chromium`
   - Linux: `~/.local-chromium`

3. **测试浏览器启动**
   - 查看详细错误信息
   - 检查浏览器启动参数

### 常见错误码

| 错误信息 | 可能原因 | 解决方案 |
|---------|---------|---------|
| `Invalid file descriptor to ICU data` | ICU 数据文件损坏 | 运行清理脚本 |
| `Failed to launch browser` | 权限或路径问题 | 检查用户数据目录权限 |
| `Timeout waiting for browser` | 浏览器启动超时 | 增加 BrowserTimeout 配置 |
| `Could not find browser` | 浏览器未下载 | 检查网络连接或手动指定路径 |

### 调试技巧

启用详细日志：
```json
{
  "Logging": {
    "LogLevel": {
      "CodeSpirit.PdfGeneration": "Debug",
      "Default": "Information"
    }
  }
}
```

在开发环境中禁用 Headless 模式以查看浏览器行为：
```json
{
  "PdfGeneration": {
    "Headless": false
  }
}
```

## 性能优化

### 浏览器池配置

根据服务器资源调整浏览器池大小：

| 服务器内存 | 建议 BrowserPoolSize | 建议 MaxConcurrentJobs |
|-----------|---------------------|----------------------|
| 4GB       | 1-2                 | 2-3                  |
| 8GB       | 2-3                 | 3-5                  |
| 16GB+     | 3-5                 | 5-10                 |

### 内存管理

限制单个浏览器进程的内存使用：
```json
{
  "PdfGeneration": {
    "BrowserMemoryLimit": 512  // MB
  }
}
```

## 参考资源

- [PuppeteerSharp 官方文档](https://www.puppeteersharp.com/)
- [Chromium 命令行参数](https://peter.sh/experiments/chromium-command-line-switches/)
- [CodeSpirit.PdfGeneration 组件源码](../../Src/Components/CodeSpirit.PdfGeneration/)

## 更新日志

### 2025-10-02
- ✅ 添加 Windows ICU 错误修复参数
- ✅ 实现用户数据目录自动管理
- ✅ 添加浏览器下载重试机制
- ✅ 创建清理脚本工具
- ✅ 增强日志记录和错误处理

