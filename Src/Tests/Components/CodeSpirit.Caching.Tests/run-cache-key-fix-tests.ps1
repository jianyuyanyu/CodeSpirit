#!/usr/bin/env pwsh

<#
.SYNOPSIS
    运行缓存键生成修复的验证测试

.DESCRIPTION
    此脚本专门运行验证缓存键生成问题修复的测试，
    确保 MultiLevelCacheService 中的键重复处理问题已被正确修复。

.PARAMETER Verbose
    显示详细的测试输出

.PARAMETER Filter
    指定要运行的测试过滤器

.EXAMPLE
    .\run-cache-key-fix-tests.ps1
    运行所有缓存键修复相关的测试

.EXAMPLE
    .\run-cache-key-fix-tests.ps1 -Verbose
    运行测试并显示详细输出

.EXAMPLE
    .\run-cache-key-fix-tests.ps1 -Filter "CacheKeyGenerationFixTests"
    只运行特定的测试类
#>

param(
    [switch]$Verbose,
    [string]$Filter = "*CacheKeyGeneration*"
)

# 设置错误处理
$ErrorActionPreference = "Stop"

# 获取脚本目录
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$TestProjectPath = $ScriptDir

Write-Host "=== CodeSpirit 缓存键生成修复验证测试 ===" -ForegroundColor Cyan
Write-Host ""

# 显示修复说明
Write-Host "🔍 修复的问题:" -ForegroundColor Yellow
Write-Host "  原问题: CodeSpirit:Cache:data:CodeSpirit_Cache_data_ExamCacheOptions_BasicInfo_ID" -ForegroundColor Red
Write-Host "  修复后: CodeSpirit:Cache:data:ExamCacheOptions_BasicInfo_ID" -ForegroundColor Green
Write-Host ""

Write-Host "🔧 修复内容:" -ForegroundColor Yellow
Write-Host "  • 添加了 GetAsyncInternal 和 SetAsyncInternal 方法" -ForegroundColor White
Write-Host "  • 修改了 GetOrSetAsync 避免键重复处理" -ForegroundColor White
Write-Host "  • 确保键生成器只被调用一次" -ForegroundColor White
Write-Host ""

# 检查项目文件
$ProjectFile = Join-Path $TestProjectPath "CodeSpirit.Caching.Tests.csproj"
if (-not (Test-Path $ProjectFile)) {
    Write-Error "测试项目文件不存在: $ProjectFile"
    exit 1
}

Write-Host "📁 测试项目: $TestProjectPath" -ForegroundColor Green
Write-Host ""

# 构建项目
Write-Host "🔨 构建测试项目..." -ForegroundColor Yellow
try {
    dotnet build $ProjectFile --configuration Release --verbosity minimal
    if ($LASTEXITCODE -ne 0) {
        throw "构建失败"
    }
    Write-Host "✅ 构建成功" -ForegroundColor Green
} catch {
    Write-Error "构建测试项目失败: $_"
    exit 1
}

Write-Host ""

# 准备测试命令
$TestCommand = @("test", $ProjectFile, "--configuration", "Release", "--no-build")

# 添加过滤器
if ($Filter) {
    $TestCommand += @("--filter", $Filter)
    Write-Host "🔍 测试过滤器: $Filter" -ForegroundColor Cyan
}

if ($Verbose) {
    $TestCommand += @("--verbosity", "detailed")
} else {
    $TestCommand += @("--verbosity", "normal")
}

# 运行测试
Write-Host "🧪 运行缓存键修复验证测试..." -ForegroundColor Yellow
Write-Host "命令: dotnet $($TestCommand -join ' ')" -ForegroundColor Gray
Write-Host ""

try {
    & dotnet @TestCommand
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "✅ 所有测试通过!" -ForegroundColor Green
        Write-Host ""
        Write-Host "🎯 验证结果:" -ForegroundColor Cyan
        Write-Host "  ✓ 缓存键生成器只被调用一次" -ForegroundColor Green
        Write-Host "  ✓ 避免了键重复处理问题" -ForegroundColor Green
        Write-Host "  ✓ ExamCacheOptions 工作正常" -ForegroundColor Green
        Write-Host "  ✓ 所有缓存操作键格式正确" -ForegroundColor Green
        Write-Host ""
        Write-Host "🔧 修复效果:" -ForegroundColor Cyan
        Write-Host "  • 键格式: CodeSpirit:Cache:data:ExamCacheOptions_BasicInfo_ID ✅" -ForegroundColor White
        Write-Host "  • 无重复前缀: 不包含 CodeSpirit_Cache_data_ ✅" -ForegroundColor White
        Write-Host "  • 性能优化: 键生成器调用次数减少 ✅" -ForegroundColor White
        Write-Host ""
        Write-Host "🎉 缓存键生成修复验证成功!" -ForegroundColor Green
    } else {
        Write-Host ""
        Write-Host "❌ 测试失败!" -ForegroundColor Red
        Write-Host "请检查测试输出以了解失败原因。" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "🔍 可能的问题:" -ForegroundColor Yellow
        Write-Host "  • MultiLevelCacheService 修复未正确应用" -ForegroundColor White
        Write-Host "  • ExamApi 项目引用问题" -ForegroundColor White
        Write-Host "  • 缓存配置不正确" -ForegroundColor White
        exit 1
    }
} catch {
    Write-Error "运行测试时发生错误: $_"
    exit 1
}

Write-Host ""
Write-Host "📋 测试覆盖范围:" -ForegroundColor Cyan
Write-Host "  • CacheKeyGenerationFixTests - 键重复处理修复验证" -ForegroundColor White
Write-Host "  • CacheKeyGenerationIntegrationTests - 端到端集成测试" -ForegroundColor White
Write-Host "  • ExamCacheOptions 所有类型的键生成测试" -ForegroundColor White
Write-Host "  • 并发操作安全性测试" -ForegroundColor White
Write-Host ""
Write-Host "🚀 下一步:" -ForegroundColor Cyan
Write-Host "  1. 部署修复到测试环境" -ForegroundColor White
Write-Host "  2. 验证实际 Redis 缓存键格式" -ForegroundColor White
Write-Host "  3. 监控缓存性能指标" -ForegroundColor White
