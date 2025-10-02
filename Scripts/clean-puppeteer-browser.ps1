# =====================================================
# PuppeteerSharp 浏览器清理脚本
# =====================================================
# 用途：清理 PuppeteerSharp 下载的 Chromium 浏览器缓存
# 解决：浏览器启动失败、ICU 错误等问题
# =====================================================

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "PuppeteerSharp 浏览器清理工具" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# 获取 PuppeteerSharp 浏览器缓存目录
$userProfile = $env:USERPROFILE
$puppeteerCache = Join-Path $userProfile ".local-chromium"
$tempUserData = Join-Path $env:TEMP "puppeteer_dev_chrome_profile"

Write-Host "检查 PuppeteerSharp 缓存目录..." -ForegroundColor Yellow
Write-Host "  浏览器缓存: $puppeteerCache" -ForegroundColor Gray
Write-Host "  临时数据: $tempUserData" -ForegroundColor Gray
Write-Host ""

# 检查并清理浏览器缓存
if (Test-Path $puppeteerCache) {
    Write-Host "发现浏览器缓存目录，准备清理..." -ForegroundColor Yellow
    
    $confirm = Read-Host "是否要删除浏览器缓存？这将重新下载 Chromium (Y/N)"
    
    if ($confirm -eq 'Y' -or $confirm -eq 'y') {
        try {
            Write-Host "正在删除浏览器缓存..." -ForegroundColor Yellow
            Remove-Item -Path $puppeteerCache -Recurse -Force
            Write-Host "✓ 浏览器缓存已清理" -ForegroundColor Green
        }
        catch {
            Write-Host "✗ 清理浏览器缓存失败: $_" -ForegroundColor Red
        }
    }
    else {
        Write-Host "跳过浏览器缓存清理" -ForegroundColor Gray
    }
}
else {
    Write-Host "未发现浏览器缓存目录" -ForegroundColor Gray
}

Write-Host ""

# 检查并清理临时用户数据
if (Test-Path $tempUserData) {
    Write-Host "发现临时用户数据目录，准备清理..." -ForegroundColor Yellow
    
    try {
        Write-Host "正在删除临时用户数据..." -ForegroundColor Yellow
        Remove-Item -Path $tempUserData -Recurse -Force
        Write-Host "✓ 临时用户数据已清理" -ForegroundColor Green
    }
    catch {
        Write-Host "✗ 清理临时用户数据失败: $_" -ForegroundColor Red
        Write-Host "提示：如果浏览器正在运行，请先关闭应用程序后重试" -ForegroundColor Yellow
    }
}
else {
    Write-Host "未发现临时用户数据目录" -ForegroundColor Gray
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "清理完成！" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "提示：" -ForegroundColor Yellow
Write-Host "  1. 重新启动应用程序，PuppeteerSharp 将自动下载 Chromium" -ForegroundColor Gray
Write-Host "  2. 如果问题仍然存在，请检查防火墙和网络连接" -ForegroundColor Gray
Write-Host "  3. 也可以手动指定 Chrome/Chromium 路径来避免自动下载" -ForegroundColor Gray
Write-Host ""

