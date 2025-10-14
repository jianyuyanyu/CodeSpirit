#!/usr/bin/env pwsh

<#
.SYNOPSIS
    运行 CodeSpirit 考试 API 负载测试的 PowerShell 脚本

.DESCRIPTION
    此脚本提供了运行不同负载场景的便捷方式。
    支持的负载场景：WarmUp, NormalLoad, PeakLoad, StressTest

.PARAMETER Scenario
    要运行的负载场景名称（WarmUp, NormalLoad, PeakLoad, StressTest）

.PARAMETER TestScenario
    要运行的测试场景名称（可选，默认为 full-exam-flow）

.EXAMPLE
    .\run-load-tests.ps1 -Scenario WarmUp
    运行预热负载场景

.EXAMPLE
    .\run-load-tests.ps1 -Scenario PeakLoad -TestScenario mixed-operations
    运行峰值负载场景，使用混合操作测试场景
#>

param(
    [Parameter(Mandatory = $false)]
    [ValidateSet("WarmUp", "NormalLoad", "PeakLoad", "StressTest")]
    [string]$Scenario = "NormalLoad",
    
    [Parameter(Mandatory = $false)]
    [ValidateSet("get-profile", "get-available-exams", "get-exam-basic-info", "get-exam-light-info", "start-exam", "record-screen-switch", "mixed-operations", "full-exam-flow")]
    [string]$TestScenario = "full-exam-flow"
)

# 设置控制台编码为 UTF-8
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

Write-Host "🚀 开始运行 CodeSpirit 考试 API 负载测试" -ForegroundColor Green
Write-Host "📊 负载场景: $Scenario" -ForegroundColor Cyan
Write-Host "🎯 测试场景: $TestScenario" -ForegroundColor Cyan

# 设置环境变量
$env:LOAD_SCENARIO = $Scenario

# 检查项目文件是否存在
$projectFile = "CodeSpirit.ExamApi.LoadTests.csproj"
if (-not (Test-Path $projectFile)) {
    Write-Host "❌ 错误: 找不到项目文件 $projectFile" -ForegroundColor Red
    Write-Host "请确保在正确的目录中运行此脚本" -ForegroundColor Yellow
    exit 1
}

# 检查配置文件是否存在
$configFile = "appsettings.json"
if (-not (Test-Path $configFile)) {
    Write-Host "❌ 错误: 找不到配置文件 $configFile" -ForegroundColor Red
    exit 1
}

# 检查测试用户文件是否存在
$testUsersFile = "testusers.json"
if (-not (Test-Path $testUsersFile)) {
    Write-Host "⚠️  警告: 找不到测试用户文件 $testUsersFile" -ForegroundColor Yellow
    Write-Host "程序将尝试创建默认测试用户" -ForegroundColor Yellow
}

try {
    Write-Host "🔧 正在构建项目..." -ForegroundColor Yellow
    dotnet build --configuration Release --no-restore
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ 项目构建失败" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "✅ 项目构建成功" -ForegroundColor Green
    
    Write-Host "🏃 正在运行负载测试..." -ForegroundColor Yellow
    Write-Host "负载配置详情:" -ForegroundColor Cyan
    
    # 读取并显示配置
    $config = Get-Content $configFile | ConvertFrom-Json
    $loadConfig = $config.LoadTestScenarios.$Scenario
    
    Write-Host "  - 持续时间: $($loadConfig.Duration)" -ForegroundColor White
    Write-Host "  - 请求速率: $($loadConfig.Rate) RPS" -ForegroundColor White
    Write-Host ""
    
    # 运行测试
    dotnet run --configuration Release --no-build -- $TestScenario
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ 负载测试完成!" -ForegroundColor Green
        Write-Host "📁 测试报告已生成到 reports 目录" -ForegroundColor Cyan
        
        # 列出生成的报告文件
        $reportFiles = Get-ChildItem -Path "reports" -Filter "*.txt" | Sort-Object LastWriteTime -Descending | Select-Object -First 3
        if ($reportFiles) {
            Write-Host "📊 最新的测试报告:" -ForegroundColor Cyan
            foreach ($file in $reportFiles) {
                Write-Host "  - $($file.Name)" -ForegroundColor White
            }
        }
    } else {
        Write-Host "❌ 负载测试失败" -ForegroundColor Red
        exit 1
    }
}
catch {
    Write-Host "❌ 执行过程中发生错误: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
finally {
    # 清理环境变量
    Remove-Item Env:LOAD_SCENARIO -ErrorAction SilentlyContinue
}

Write-Host "🎉 脚本执行完成!" -ForegroundColor Green
