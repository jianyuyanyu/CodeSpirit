#!/usr/bin/env dotnet script
#r "nuget: Microsoft.Playwright, 1.49.0"

using Microsoft.Playwright;
using System;
using System.Threading.Tasks;

/// <summary>
/// CodeSpirit 租户后台登录自动化脚本
/// 使用 Playwright 进行自动化登录测试
/// </summary>

// ============= .NET 10 序列化配置 =============
// 启用反射序列化以支持 Playwright
AppContext.SetSwitch("System.Text.Json.JsonSerializer.IsReflectionEnabledByDefault", true);

// ============= 配置参数 =============
var webHost = Args.Count > 0 ? Args[0] : "https://localhost:7120";
var tenantId = Args.Count > 1 ? Args[1] : "default";
var username = Args.Count > 2 ? Args[2] : "admin";
var password = Args.Count > 3 ? Args[3] : "123@Admin";
var headless = Args.Count > 4 ? bool.Parse(Args[4]) : false; // 默认显示浏览器

Console.WriteLine("=".PadRight(60, '='));
Console.WriteLine("CodeSpirit 租户后台登录自动化");
Console.WriteLine("=".PadRight(60, '='));
Console.WriteLine($"Web Host: {webHost}");
Console.WriteLine($"Tenant ID: {tenantId}");
Console.WriteLine($"Username: {username}");
Console.WriteLine($"Headless: {headless}");
Console.WriteLine("=".PadRight(60, '='));
Console.WriteLine();

try
{
    // 1. 初始化 Playwright
    Console.WriteLine("[1/8] 初始化 Playwright...");
    using var playwright = await Playwright.CreateAsync();
    
    // 2. 启动浏览器
    Console.WriteLine("[2/8] 启动 Chromium 浏览器...");
    await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
    {
        Headless = headless,
        Args = new[] { "--ignore-certificate-errors" } // 忽略自签名证书错误
    });
    
    // 3. 创建浏览器上下文
    Console.WriteLine("[3/8] 创建浏览器上下文...");
    await using var context = await browser.NewContextAsync(new BrowserNewContextOptions
    {
        IgnoreHTTPSErrors = true // 忽略 HTTPS 证书错误
    });
    var page = await context.NewPageAsync();
    
    // 4. 导航到租户登录页面
    var loginUrl = $"{webHost}/{tenantId}/login";
    Console.WriteLine($"[4/8] 导航到租户登录页面: {loginUrl}");
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
    
    // 5. 等待租户信息加载
    Console.WriteLine("[5/8] 等待租户信息加载...");
    try
    {
        // 等待加载提示消失或表单出现
        await page.WaitForSelectorAsync("input[name='userName'], input[type='text']", new PageWaitForSelectorOptions
        {
            Timeout = 15000,
            State = WaitForSelectorState.Visible
        });
        Console.WriteLine("    ✓ 租户信息加载完成");
    }
    catch (TimeoutException)
    {
        Console.WriteLine("    ⚠️ 租户信息加载超时，尝试继续...");
    }
    
    // 6. 填充登录表单
    Console.WriteLine("[6/8] 填充登录表单...");
    
    // 等待并填充用户名
    await page.WaitForSelectorAsync("input[name='userName'], input[type='text']", new PageWaitForSelectorOptions
    {
        Timeout = 10000
    });
    await page.FillAsync("input[name='userName'], input[type='text']", username);
    Console.WriteLine($"    ✓ 用户名已填充: {username}");
    
    // 等待并填充密码
    await page.WaitForSelectorAsync("input[name='password'], input[type='password']", new PageWaitForSelectorOptions
    {
        Timeout = 5000
    });
    await page.FillAsync("input[name='password'], input[type='password']", password);
    Console.WriteLine($"    ✓ 密码已填充: {"*".PadLeft(password.Length, '*')}");
    
    // 7. 提交表单（按 Enter 键）
    Console.WriteLine("[7/8] 提交登录表单...");
    await page.PressAsync("input[name='password'], input[type='password']", "Enter");
    
    // 8. 等待登录完成（URL 跳转离开 /login）
    Console.WriteLine("[8/8] 等待登录完成...");
    
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
        Console.WriteLine($"租户 ID: {tenantId}");
        Console.WriteLine($"当前 URL: {currentUrl}");
        Console.WriteLine($"页面标题: {pageTitle}");
        Console.WriteLine();
        
        // 提示使用 MCP 工具验证
        Console.WriteLine("💡 后续验证建议：");
        Console.WriteLine("   使用 Playwright MCP 工具获取页面快照验证登录状态：");
        Console.WriteLine("   - 服务器: cursor-browser-extension");
        Console.WriteLine("   - 工具: browser_snapshot");
        Console.WriteLine("   - 说明: 验证管理后台页面元素是否正常加载");
        Console.WriteLine();
        
        // 等待几秒让用户看到页面
        if (!headless)
        {
            Console.WriteLine("按 Enter 键关闭浏览器...");
            Console.ReadLine();
        }
        else
        {
            // Headless 模式等待 2 秒
            await Task.Delay(2000);
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
        Console.WriteLine($"租户 ID: {tenantId}");
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
        Console.WriteLine("   2. 租户不存在或已禁用");
        Console.WriteLine("   3. 账户被锁定");
        Console.WriteLine("   4. 网络连接问题");
        Console.WriteLine();
        Console.WriteLine("📝 默认凭证（参考种子数据）：");
        Console.WriteLine($"   - 租户ID: {tenantId}");
        Console.WriteLine("   - 用户名: admin");
        Console.WriteLine("   - 密码: 123@Admin");
        Console.WriteLine("   - 来源: Src/ApiServices/CodeSpirit.IdentityApi/Data/Seeders/");
        Console.WriteLine("           - UserSeeder.cs (第53行)");
        Console.WriteLine("           - UnifiedUserSeederService.cs (第216行)");
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
    Console.WriteLine("   3. 确认租户 ID 存在");
    Console.WriteLine("   4. 检查 Playwright 是否正确安装");
    Console.WriteLine("   5. 查看应用日志排查错误");
    Console.WriteLine();
    
    Environment.Exit(1);
}

/*
使用说明：
==========

1. 安装 Playwright（首次使用）：
   在脚本所在目录运行：
   pwsh bin/Debug/net10.0/playwright.ps1 install chromium

2. 使用默认配置运行（租户: default）：
   dotnet script login-tenant.cs

3. 使用自定义租户运行：
   dotnet script login-tenant.cs -- https://localhost:7120 mytenant admin MyPass@123

4. 使用无头模式运行：
   dotnet script login-tenant.cs -- https://localhost:7120 default admin 123@Admin true

参数说明：
- 参数1: Web Host（默认: https://localhost:7120）
- 参数2: 租户ID（默认: default）
- 参数3: 用户名（默认: admin）
- 参数4: 密码（默认: 123@Admin）
- 参数5: Headless 模式（默认: false，显示浏览器）

注意：
- 使用 dotnet script 运行脚本，参数需要用 -- 分隔
- 脚本已配置 .NET 10 序列化支持（AppContext.SetSwitch）
*/
