#!/usr/bin/env dotnet script
#r "nuget: Microsoft.Playwright, 1.49.0"

using Microsoft.Playwright;
using System;
using System.Threading.Tasks;

/// <summary>
/// CodeSpirit 系统后台登录自动化脚本
/// 使用 Playwright 进行自动化登录测试
/// </summary>

// ============= .NET 10 序列化配置 =============
// 启用反射序列化以支持 Playwright
AppContext.SetSwitch("System.Text.Json.JsonSerializer.IsReflectionEnabledByDefault", true);

// ============= 配置参数 =============
var webHost = Args.Count > 0 ? Args[0] : "https://localhost:7120";
var username = Args.Count > 1 ? Args[1] : "systemadmin";
var password = Args.Count > 2 ? Args[2] : "CodeSpirit@2025";
var headless = Args.Count > 3 ? bool.Parse(Args[3]) : false; // 默认显示浏览器

Console.WriteLine("=".PadRight(60, '='));
Console.WriteLine("CodeSpirit 系统后台登录自动化");
Console.WriteLine("=".PadRight(60, '='));
Console.WriteLine($"Web Host: {webHost}");
Console.WriteLine($"Username: {username}");
Console.WriteLine($"Headless: {headless}");
Console.WriteLine("=".PadRight(60, '='));
Console.WriteLine();

try
{
    // 1. 初始化 Playwright
    Console.WriteLine("[1/7] 初始化 Playwright...");
    using var playwright = await Playwright.CreateAsync();
    
    // 2. 启动浏览器
    Console.WriteLine("[2/7] 启动 Chromium 浏览器...");
    await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
    {
        Headless = headless,
        Args = new[] { "--ignore-certificate-errors" } // 忽略自签名证书错误
    });
    
    // 3. 创建浏览器上下文
    Console.WriteLine("[3/7] 创建浏览器上下文...");
    await using var context = await browser.NewContextAsync(new BrowserNewContextOptions
    {
        IgnoreHTTPSErrors = true // 忽略 HTTPS 证书错误
    });
    var page = await context.NewPageAsync();
    
    // 4. 导航到登录页面
    var loginUrl = $"{webHost}/login";
    Console.WriteLine($"[4/7] 导航到系统登录页面: {loginUrl}");
    var response = await page.GotoAsync(loginUrl, new PageGotoOptions
    {
        WaitUntil = WaitUntilState.NetworkIdle,
        Timeout = 30000
    });
    
    if (response == null || !response.Ok)
    {
        throw new Exception($"无法访问登录页面，HTTP 状态: {response?.Status}");
    }
    
    Console.WriteLine($"    ✓ 页面加载成功，状态码: {response.Status}");
    Console.WriteLine($"    ✓ 页面标题: {await page.TitleAsync()}");
    
    // 5. 填充登录表单
    Console.WriteLine("[5/7] 填充登录表单...");
    
    // 等待表单元素加载
    await page.WaitForSelectorAsync("input[name='userName'], input[type='text']", new PageWaitForSelectorOptions
    {
        Timeout = 10000
    });
    
    // 填充用户名
    await page.FillAsync("input[name='userName'], input[type='text']", username);
    Console.WriteLine($"    ✓ 用户名已填充: {username}");
    
    // 填充密码
    await page.FillAsync("input[name='password'], input[type='password']", password);
    Console.WriteLine($"    ✓ 密码已填充: {"*".PadLeft(password.Length, '*')}");
    
    // 6. 提交表单（按 Enter 键）
    Console.WriteLine("[6/7] 提交登录表单...");
    await page.PressAsync("input[name='password'], input[type='password']", "Enter");
    
    // 7. 等待登录完成（URL 跳转离开 /login）
    Console.WriteLine("[7/7] 等待登录完成...");
    try
    {
        await page.WaitForURLAsync(url => !url.Contains("/login"), new PageWaitForURLOptions
        {
            Timeout = 15000
        });
        
        var currentUrl = page.Url;
        var pageTitle = await page.TitleAsync();
        
        // 检查是否真的登录成功（页面标题不应该是"登录"）
        if (pageTitle.Contains("登录") || pageTitle.Contains("Login"))
        {
            throw new TimeoutException("登录后页面标题仍显示为登录页");
        }
        
        Console.WriteLine();
        Console.WriteLine("=".PadRight(60, '='));
        Console.WriteLine("✅ 登录成功！");
        Console.WriteLine("=".PadRight(60, '='));
        Console.WriteLine($"当前 URL: {currentUrl}");
        Console.WriteLine($"页面标题: {pageTitle}");
        Console.WriteLine();
        
        // 提示使用 MCP 工具验证
        Console.WriteLine("💡 后续验证建议：");
        Console.WriteLine("   使用 Playwright MCP 工具获取页面快照验证登录状态：");
        Console.WriteLine("   - 工具: browser_snapshot");
        Console.WriteLine("   - 服务器: cursor-browser-extension");
        Console.WriteLine();
        
        // 等待几秒让用户看到页面
        if (!headless)
        {
            Console.WriteLine("按 Enter 键关闭浏览器...");
            Console.ReadLine();
        }
        
        Environment.Exit(0);
    }
    catch (TimeoutException)
    {
        var currentUrl = page.Url;
        Console.WriteLine();
        Console.WriteLine("=".PadRight(60, '='));
        Console.WriteLine("❌ 登录失败或超时");
        Console.WriteLine("=".PadRight(60, '='));
        Console.WriteLine($"当前 URL: {currentUrl}");
        Console.WriteLine($"页面标题: {await page.TitleAsync()}");
        Console.WriteLine();
        
        // 检查是否有错误消息
        var errorElements = await page.QuerySelectorAllAsync(".cxd-Toast--error, .error-message, [class*='error']");
        if (errorElements.Count > 0)
        {
            Console.WriteLine("⚠️ 检测到错误消息：");
            foreach (var errorElement in errorElements.Take(3))
            {
                var errorText = await errorElement.TextContentAsync();
                if (!string.IsNullOrWhiteSpace(errorText))
                {
                    Console.WriteLine($"   - {errorText.Trim()}");
                }
            }
            Console.WriteLine();
        }
        
        Console.WriteLine("🔍 可能的原因：");
        Console.WriteLine("   1. 用户名或密码不正确");
        Console.WriteLine("   2. 账户被锁定或禁用");
        Console.WriteLine("   3. 网络连接问题");
        Console.WriteLine("   4. 应用服务未正常运行");
        Console.WriteLine();
        Console.WriteLine("📝 默认凭证（参考种子数据）：");
        Console.WriteLine("   - 用户名: systemadmin");
        Console.WriteLine("   - 密码: CodeSpirit@2025");
        Console.WriteLine("   - 来源: Src/ApiServices/CodeSpirit.IdentityApi/Data/Seeders/UnifiedUserSeederService.cs");
        Console.WriteLine();
        
        if (!headless)
        {
            Console.WriteLine("按 Enter 键关闭浏览器...");
            Console.ReadLine();
        }
        
        Environment.Exit(1);
    }
}
catch (Exception ex)
{
    Console.WriteLine();
    Console.WriteLine("=".PadRight(60, '='));
    Console.WriteLine("❌ 脚本执行失败");
    Console.WriteLine("=".PadRight(60, '='));
    Console.WriteLine($"错误类型: {ex.GetType().Name}");
    Console.WriteLine($"错误消息: {ex.Message}");
    Console.WriteLine();
    
    if (ex.InnerException != null)
    {
        Console.WriteLine($"内部错误: {ex.InnerException.Message}");
        Console.WriteLine();
    }
    
    Console.WriteLine("🔍 故障排查建议：");
    Console.WriteLine("   1. 确认 Aspire 应用已启动（aspire run）");
    Console.WriteLine("   2. 确认 Web Host 地址正确");
    Console.WriteLine("   3. 检查 Playwright 是否正确安装");
    Console.WriteLine("   4. 查看浏览器控制台错误信息");
    Console.WriteLine();
    
    Environment.Exit(1);
}

/*
使用说明：
==========

1. 安装 Playwright（首次使用）：
   pwsh bin/Debug/net10.0/playwright.ps1 install chromium

2. 使用默认配置运行：
   dotnet script login-system.cs

3. 使用自定义配置运行：
   dotnet script login-system.cs -- https://localhost:7120 systemadmin CodeSpirit@2025

4. 使用无头模式运行：
   dotnet script login-system.cs -- https://localhost:7120 systemadmin CodeSpirit@2025 true

参数说明：
- 参数1: Web Host（默认: https://localhost:7120）
- 参数2: 用户名（默认: systemadmin）
- 参数3: 密码（默认: CodeSpirit@2025）
- 参数4: Headless 模式（默认: false，显示浏览器）

注意：
- 使用 dotnet script 运行脚本，参数需要用 -- 分隔
- 脚本已配置 .NET 10 序列化支持（AppContext.SetSwitch）
*/
