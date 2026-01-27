#!/usr/bin/env dotnet script
#r "nuget: Microsoft.Playwright, 1.49.0"

using Microsoft.Playwright;
using System;
using System.Threading.Tasks;

/// <summary>
/// CodeSpirit 租户后台登录并访问多个页面以触发审计日志
/// 使用 Playwright 进行自动化操作
/// </summary>

// ============= .NET 10 序列化配置 =============
// 启用反射序列化以支持 Playwright
AppContext.SetSwitch("System.Text.Json.JsonSerializer.IsReflectionEnabledByDefault", true);

// ============= 配置参数 =============
var webHost = Args.Count > 0 ? Args[0] : "https://localhost:7120";
var tenantId = Args.Count > 1 ? Args[1] : "default";
var username = Args.Count > 2 ? Args[2] : "Admin";
var password = Args.Count > 3 ? Args[3] : "Admin@123";
var headless = Args.Count > 4 ? bool.Parse(Args[4]) : false; // 默认显示浏览器

Console.WriteLine("=".PadRight(60, '='));
Console.WriteLine("CodeSpirit 租户后台登录并访问页面");
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
    Console.WriteLine("[1/10] 初始化 Playwright...");
    using var playwright = await Playwright.CreateAsync();
    
    // 2. 启动浏览器
    Console.WriteLine("[2/10] 启动 Chromium 浏览器...");
    await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
    {
        Headless = headless,
        Args = new[] { "--ignore-certificate-errors" } // 忽略自签名证书错误
    });
    
    // 3. 创建浏览器上下文
    Console.WriteLine("[3/10] 创建浏览器上下文...");
    await using var context = await browser.NewContextAsync(new BrowserNewContextOptions
    {
        IgnoreHTTPSErrors = true // 忽略 HTTPS 证书错误
    });
    var page = await context.NewPageAsync();
    
    // 4. 导航到租户登录页面
    var loginUrl = $"{webHost}/{tenantId}/login";
    Console.WriteLine($"[4/10] 导航到租户登录页面: {loginUrl}");
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
    Console.WriteLine("[5/10] 等待租户信息加载...");
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
    Console.WriteLine("[6/10] 填充登录表单...");
    
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
    Console.WriteLine("[7/10] 提交登录表单...");
    await page.PressAsync("input[name='password'], input[type='password']", "Enter");
    
    // 8. 等待登录完成（URL 跳转离开 /login）
    Console.WriteLine("[8/10] 等待登录完成...");
    
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
        Console.WriteLine("✅ 登录成功！");
        Console.WriteLine($"    当前 URL: {currentUrl}");
        Console.WriteLine($"    页面标题: {pageTitle}");
        Console.WriteLine();
        
        // 等待页面稳定
        await Task.Delay(2000);
        
        // 9. 访问用户管理页面
        Console.WriteLine("[9/10] 访问用户管理页面...");
        var userManagementUrl = $"{webHost}/{tenantId}/identity/users";
        Console.WriteLine($"    导航到: {userManagementUrl}");
        
        await page.GotoAsync(userManagementUrl, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle,
            Timeout = 30000
        });
        
        await Task.Delay(2000); // 等待页面加载和审计日志记录
        
        var userPageTitle = await page.TitleAsync();
        Console.WriteLine($"    ✓ 用户管理页面加载成功");
        Console.WriteLine($"    ✓ 页面标题: {userPageTitle}");
        Console.WriteLine($"    ✓ 审计日志已触发");
        Console.WriteLine();
        
        // 10. 访问角色管理页面
        Console.WriteLine("[10/10] 访问角色管理页面...");
        var roleManagementUrl = $"{webHost}/{tenantId}/identity/roles";
        Console.WriteLine($"    导航到: {roleManagementUrl}");
        
        await page.GotoAsync(roleManagementUrl, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle,
            Timeout = 30000
        });
        
        await Task.Delay(2000); // 等待页面加载和审计日志记录
        
        var rolePageTitle = await page.TitleAsync();
        Console.WriteLine($"    ✓ 角色管理页面加载成功");
        Console.WriteLine($"    ✓ 页面标题: {rolePageTitle}");
        Console.WriteLine($"    ✓ 审计日志已触发");
        Console.WriteLine();
        
        // 完成
        Console.WriteLine("=".PadRight(60, '='));
        Console.WriteLine("✅ 所有页面访问完成！");
        Console.WriteLine("=".PadRight(60, '='));
        Console.WriteLine($"租户 ID: {tenantId}");
        Console.WriteLine($"访问的页面:");
        Console.WriteLine($"  1. 登录页面");
        Console.WriteLine($"  2. 用户管理页面");
        Console.WriteLine($"  3. 角色管理页面");
        Console.WriteLine();
        Console.WriteLine("💡 审计日志已记录到 GreptimeDB");
        Console.WriteLine("   可以通过以下方式查看审计日志:");
        Console.WriteLine("   - Aspire Dashboard: https://localhost:17109");
        Console.WriteLine("   - GreptimeDB: http://localhost:4000");
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

2. 使用默认配置运行（租户: default, 用户: Admin, 密码: Admin@123）：
   dotnet script navigate-pages.cs

3. 使用自定义租户运行：
   dotnet script navigate-pages.cs -- https://localhost:7120 mytenant Admin MyPass@123

4. 使用无头模式运行：
   dotnet script navigate-pages.cs -- https://localhost:7120 default Admin Admin@123 true

参数说明：
- 参数1: Web Host（默认: https://localhost:7120）
- 参数2: 租户ID（默认: default）
- 参数3: 用户名（默认: Admin）
- 参数4: 密码（默认: Admin@123）
- 参数5: Headless 模式（默认: false，显示浏览器）

注意：
- 使用 dotnet script 运行脚本，参数需要用 -- 分隔
- 脚本已配置 .NET 10 序列化支持（AppContext.SetSwitch）
- 脚本会自动访问用户管理和角色管理页面以触发审计日志
*/
