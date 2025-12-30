#!/usr/bin/env pwsh
<#
.SYNOPSIS
    运行 CodeSpirit.Caching 组件的 TTL 时间一致性测试

.DESCRIPTION
    此脚本用于运行缓存组件的 TTL 时间一致性测试，包括单元测试和集成测试。
    可以选择运行特定类型的测试、生成覆盖率报告等。

.PARAMETER TestType
    测试类型：All（全部）、Unit（单元测试）、Integration（集成测试）
    默认值: All

.PARAMETER Verbose
    显示详细的测试输出

.PARAMETER Coverage
    生成测试覆盖率报告

.PARAMETER Filter
    自定义测试过滤器

.EXAMPLE
    .\run-ttl-consistency-tests.ps1
    运行所有 TTL 一致性测试

.EXAMPLE
    .\run-ttl-consistency-tests.ps1 -TestType Unit
    只运行单元测试

.EXAMPLE
    .\run-ttl-consistency-tests.ps1 -TestType Integration -Verbose
    运行集成测试并显示详细输出

.EXAMPLE
    .\run-ttl-consistency-tests.ps1 -Coverage
    运行所有测试并生成覆盖率报告

.EXAMPLE
    .\run-ttl-consistency-tests.ps1 -Filter "L1Cache"
    只运行包含 "L1Cache" 的测试
#>

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("All", "Unit", "Integration")]
    [string]$TestType = "All",
    
    [Parameter(Mandatory=$false)]
    [switch]$Verbose,
    
    [Parameter(Mandatory=$false)]
    [switch]$Coverage,
    
    [Parameter(Mandatory=$false)]
    [string]$Filter = ""
)

# 设置错误处理
$ErrorActionPreference = "Stop"

# 获取脚本所在目录
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectPath = Join-Path $ScriptDir "CodeSpirit.Caching.Tests.csproj"

# 颜色输出函数
function Write-ColorOutput {
    param(
        [string]$Message,
        [ConsoleColor]$ForegroundColor = [ConsoleColor]::White
    )
    $originalColor = $Host.UI.RawUI.ForegroundColor
    $Host.UI.RawUI.ForegroundColor = $ForegroundColor
    Write-Output $Message
    $Host.UI.RawUI.ForegroundColor = $originalColor
}

# 打印标题
function Write-Title {
    param([string]$Title)
    Write-ColorOutput "`n========================================" -ForegroundColor Cyan
    Write-ColorOutput $Title -ForegroundColor Cyan
    Write-ColorOutput "========================================`n" -ForegroundColor Cyan
}

# 打印分隔线
function Write-Separator {
    Write-ColorOutput "----------------------------------------" -ForegroundColor Gray
}

# 检查项目文件是否存在
if (-not (Test-Path $ProjectPath)) {
    Write-ColorOutput "错误: 找不到测试项目文件: $ProjectPath" -ForegroundColor Red
    exit 1
}

# 打印测试信息
Write-Title "CodeSpirit.Caching TTL 时间一致性测试"

Write-ColorOutput "测试项目: $ProjectPath" -ForegroundColor Gray
Write-ColorOutput "测试类型: $TestType" -ForegroundColor Gray
if ($Filter) {
    Write-ColorOutput "过滤器: $Filter" -ForegroundColor Gray
}
Write-ColorOutput ""

# 构建测试过滤器
$TestFilter = ""
switch ($TestType) {
    "Unit" {
        $TestFilter = "FullyQualifiedName~TtlConsistencyTests&FullyQualifiedName!~Integration"
    }
    "Integration" {
        $TestFilter = "FullyQualifiedName~TtlConsistencyIntegrationTests"
    }
    "All" {
        $TestFilter = "FullyQualifiedName~TtlConsistency"
    }
}

# 如果提供了自定义过滤器，附加到现有过滤器
if ($Filter) {
    $TestFilter = "$TestFilter&FullyQualifiedName~$Filter"
}

# 构建 dotnet test 命令参数
$TestArgs = @(
    "test",
    $ProjectPath,
    "--filter", $TestFilter,
    "--nologo"
)

# 添加详细输出
if ($Verbose) {
    $TestArgs += "--verbosity", "detailed"
} else {
    $TestArgs += "--verbosity", "normal"
}

# 添加覆盖率选项
if ($Coverage) {
    Write-ColorOutput "启用代码覆盖率收集..." -ForegroundColor Yellow
    $TestArgs += "/p:CollectCoverage=true"
    $TestArgs += "/p:CoverletOutputFormat=opencover"
    $TestArgs += "/p:CoverletOutput=./TestResults/coverage.opencover.xml"
}

# 运行测试前的准备
Write-Separator
Write-ColorOutput "开始运行测试..." -ForegroundColor Green
Write-ColorOutput "过滤器: $TestFilter" -ForegroundColor Gray
Write-Separator
Write-ColorOutput ""

# 记录开始时间
$StartTime = Get-Date

# 运行测试
try {
    & dotnet @TestArgs
    $ExitCode = $LASTEXITCODE
} catch {
    Write-ColorOutput "`n错误: 运行测试时发生异常" -ForegroundColor Red
    Write-ColorOutput $_.Exception.Message -ForegroundColor Red
    exit 1
}

# 记录结束时间
$EndTime = Get-Date
$Duration = $EndTime - $StartTime

# 打印测试结果摘要
Write-ColorOutput ""
Write-Separator

if ($ExitCode -eq 0) {
    Write-ColorOutput "✓ 所有测试通过!" -ForegroundColor Green
} else {
    Write-ColorOutput "✗ 部分测试失败" -ForegroundColor Red
}

Write-ColorOutput "总耗时: $($Duration.TotalSeconds.ToString("0.00")) 秒" -ForegroundColor Gray

# 如果生成了覆盖率报告，提供信息
if ($Coverage) {
    Write-Separator
    $CoverageFile = Join-Path $ScriptDir "TestResults\coverage.opencover.xml"
    if (Test-Path $CoverageFile) {
        Write-ColorOutput "覆盖率报告已生成: $CoverageFile" -ForegroundColor Green
        Write-ColorOutput "可以使用 ReportGenerator 生成HTML报告:" -ForegroundColor Yellow
        Write-ColorOutput "  dotnet tool install -g dotnet-reportgenerator-globaltool" -ForegroundColor Gray
        Write-ColorOutput "  reportgenerator -reports:`"$CoverageFile`" -targetdir:`"TestResults\CoverageReport`" -reporttypes:Html" -ForegroundColor Gray
    }
}

# 打印测试详情链接
Write-Separator
Write-ColorOutput "测试详细说明文档:" -ForegroundColor Cyan
Write-ColorOutput "  $(Join-Path $ScriptDir 'Services\TtlConsistencyTests说明.md')" -ForegroundColor Gray

# 打印快速参考
Write-Separator
Write-ColorOutput "快速参考:" -ForegroundColor Cyan
Write-ColorOutput "  运行所有测试:     .\run-ttl-consistency-tests.ps1" -ForegroundColor Gray
Write-ColorOutput "  只运行单元测试:   .\run-ttl-consistency-tests.ps1 -TestType Unit" -ForegroundColor Gray
Write-ColorOutput "  只运行集成测试:   .\run-ttl-consistency-tests.ps1 -TestType Integration" -ForegroundColor Gray
Write-ColorOutput "  生成覆盖率报告:   .\run-ttl-consistency-tests.ps1 -Coverage" -ForegroundColor Gray
Write-ColorOutput "  详细输出:         .\run-ttl-consistency-tests.ps1 -Verbose" -ForegroundColor Gray
Write-ColorOutput "  自定义过滤:       .\run-ttl-consistency-tests.ps1 -Filter `"L1Cache`"" -ForegroundColor Gray
Write-ColorOutput ""

# 退出并返回测试结果代码
exit $ExitCode

