# CodeSpirit.PdfGeneration 使用指南

## 1. 组件介绍

CodeSpirit.PdfGeneration 是一个高性能、跨平台的PDF生成组件，基于PuppeteerSharp实现，提供了从HTML内容生成PDF文件的功能。该组件针对高性能、大批量生成场景进行了优化，支持浏览器实例池化管理、并发控制、自动重试等特性。

## 2. 主要特性

- **高性能**：通过浏览器实例池化管理，避免频繁创建和销毁浏览器实例
- **并发控制**：通过信号量控制并发任务数量，避免资源过度占用
- **批量处理**：支持批量生成PDF，自动分批处理大量任务
- **自动重试**：内置重试机制，提高生成成功率
- **跨平台支持**：适配不同操作系统环境，支持容器化部署
- **资源优化**：支持浏览器内存限制，防止内存泄漏
- **状态监控**：提供服务状态监控和性能指标收集

## 3. 安装与配置

### 3.1 添加NuGet包引用

```xml
<PackageReference Include="CodeSpirit.PdfGeneration" Version="1.0.0" />
```

### 3.2 配置服务

在`Program.cs`或`Startup.cs`中注册服务：

```csharp
// 方式一：从配置文件注册
builder.Services.AddPdfGeneration(builder.Configuration);

// 方式二：通过代码配置
builder.Services.AddPdfGeneration(options =>
{
    options.MaxConcurrentJobs = 5;
    options.BrowserPoolSize = 3;
    options.BrowserTimeout = TimeSpan.FromMinutes(2);
    options.Headless = true;
    options.RetryCount = 3;
    options.BrowserMemoryLimit = 512;
});
```

### 3.3 配置文件示例

在`appsettings.json`中添加以下配置：

```json
{
  "PdfGeneration": {
    "MaxConcurrentJobs": 5,
    "BrowserPoolSize": 3,
    "BrowserTimeout": "00:02:00",
    "Headless": true,
    "RetryCount": 3,
    "BrowserMemoryLimit": 512,
    "BrowserArguments": [
      "--no-sandbox",
      "--disable-setuid-sandbox",
      "--disable-dev-shm-usage",
      "--disable-gpu",
      "--no-first-run",
      "--no-zygote",
      "--single-process"
    ]
  }
}
```

### 3.4 初始化服务

在应用启动时初始化PDF生成服务：

```csharp
// 在Program.cs中
app.UsePdfGenerationAsync().GetAwaiter().GetResult();

// 或者使用异步方式
await app.UsePdfGenerationAsync();
```

## 4. 使用示例

### 4.1 基本用法

```csharp
[ApiController]
[Route("api/[controller]")]
public class PdfController : ControllerBase
{
    private readonly IPdfGenerationService _pdfService;
    
    public PdfController(IPdfGenerationService pdfService)
    {
        _pdfService = pdfService;
    }
    
    [HttpPost("generate")]
    public async Task<IActionResult> GeneratePdf([FromBody] string htmlContent)
    {
        var pdfOptions = new PdfOptions
        {
            Format = PaperFormat.A4,
            PrintBackground = true,
            MarginOptions = new MarginOptions
            {
                Top = "10mm",
                Bottom = "10mm",
                Left = "10mm",
                Right = "10mm"
            }
        };
        
        var pdfBytes = await _pdfService.GeneratePdfAsync(htmlContent, pdfOptions);
        return File(pdfBytes, "application/pdf", "document.pdf");
    }
}
```

### 4.2 批量生成PDF

```csharp
[HttpPost("batch-generate")]
public async Task<IActionResult> BatchGeneratePdf([FromBody] List<string> htmlContents)
{
    var pdfOptions = new PdfOptions
    {
        Format = PaperFormat.A4,
        PrintBackground = true
    };
    
    var pdfFiles = await _pdfService.GeneratePdfBatchAsync(htmlContents, pdfOptions);
    
    // 创建ZIP文件
    using var memoryStream = new MemoryStream();
    using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
    {
        for (int i = 0; i < pdfFiles.Count; i++)
        {
            var entry = archive.CreateEntry($"document_{i + 1}.pdf", CompressionLevel.Optimal);
            using var entryStream = entry.Open();
            await entryStream.WriteAsync(pdfFiles[i]);
        }
    }
    
    memoryStream.Position = 0;
    return File(memoryStream.ToArray(), "application/zip", "documents.zip");
}
```

### 4.3 获取服务状态

```csharp
[HttpGet("status")]
public async Task<IActionResult> GetStatus()
{
    var status = await _pdfService.GetStatusAsync();
    return Ok(status);
}
```

## 5. Docker 部署

在Dockerfile中添加必要的依赖：

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base

# 安装Chrome依赖
RUN apt-get update && apt-get install -y \
    chromium \
    libgbm1 \
    libnss3 \
    libxss1 \
    libasound2 \
    libatk1.0-0 \
    libatk-bridge2.0-0 \
    libcups2 \
    libdrm2 \
    libxkbcommon0 \
    libxcomposite1 \
    libxdamage1 \
    libxfixes3 \
    libxrandr2 \
    libgbm1 \
    libpango-1.0-0 \
    libcairo2 \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

# 设置环境变量
ENV PUPPETEER_EXECUTABLE_PATH=/usr/bin/chromium
ENV PDF_GENERATION_MAX_CONCURRENT=5
ENV PDF_GENERATION_POOL_SIZE=3
```

## 6. 性能优化建议

1. **调整浏览器池大小**：根据服务器资源和负载情况，调整`BrowserPoolSize`参数
2. **控制并发任务数**：通过`MaxConcurrentJobs`参数控制并发任务数量
3. **设置内存限制**：使用`BrowserMemoryLimit`参数限制浏览器进程内存使用
4. **批量处理**：对于大批量PDF生成任务，使用`GeneratePdfBatchAsync`方法
5. **预热服务**：在应用启动时初始化PDF生成服务，避免首次生成延迟

## 7. 常见问题

### 7.1 在Docker容器中运行时出现权限问题

确保在Dockerfile中添加了`--no-sandbox`和`--disable-setuid-sandbox`参数，或者在配置中设置：

```json
"BrowserArguments": [
  "--no-sandbox",
  "--disable-setuid-sandbox",
  "..."
]
```

### 7.2 生成PDF时出现中文乱码

确保Docker镜像中安装了中文字体，可以在Dockerfile中添加：

```dockerfile
RUN apt-get update && apt-get install -y fonts-noto-cjk
```

### 7.3 服务内存占用过高

调整`BrowserPoolSize`和`BrowserMemoryLimit`参数，减少浏览器实例数量或限制每个实例的内存使用。

## 8. 高级功能

### 8.1 自定义PDF选项

```csharp
var pdfOptions = new PdfOptions
{
    Format = PaperFormat.A4,
    PrintBackground = true,
    Scale = 1.0m,
    Landscape = false,
    DisplayHeaderFooter = true,
    HeaderTemplate = "<div style='font-size:10px;'>Header</div>",
    FooterTemplate = "<div style='font-size:10px;'>Footer</div>",
    MarginOptions = new MarginOptions
    {
        Top = "10mm",
        Bottom = "10mm",
        Left = "10mm",
        Right = "10mm"
    }
};
```

### 8.2 自定义浏览器路径

在某些环境中，可能需要指定自定义的浏览器可执行文件路径：

```csharp
services.AddPdfGeneration(options =>
{
    options.ExecutablePath = "/usr/bin/chromium";
});
```

## 9. 版本历史

- **1.0.0** - 初始版本
  - 基本PDF生成功能
  - 浏览器实例池化管理
  - 批量生成支持
  - 跨平台兼容性优化