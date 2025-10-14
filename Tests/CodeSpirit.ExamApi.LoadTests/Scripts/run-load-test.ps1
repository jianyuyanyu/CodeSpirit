# CodeSpirit 考试系统负载测试运行脚本
param(
    [string]$Scenario = "all",
    [string]$Environment = "Development",
    [string]$BaseUrl = "https://localhost:7001",
    [string]$IdentityUrl = "https://localhost:7002",
    [int]$Duration = 10,
    [int]$MaxUsers = 100,
    [bool]$SetupData = $false,
    [bool]$CleanupAfter = $false
)

Write-Host "CodeSpirit 考试系统负载测试" -ForegroundColor Green
Write-Host "==============================" -ForegroundColor Green
Write-Host "测试场景: $Scenario" -ForegroundColor Yellow
Write-Host "测试环境: $Environment" -ForegroundColor Yellow
Write-Host "API地址: $BaseUrl" -ForegroundColor Yellow
Write-Host "认证地址: $IdentityUrl" -ForegroundColor Yellow
Write-Host "测试时长: $Duration 分钟" -ForegroundColor Yellow
Write-Host "最大用户: $MaxUsers" -ForegroundColor Yellow

# 获取脚本目录
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectDir = Split-Path -Parent $scriptDir

try {
    # 设置工作目录
    Set-Location $projectDir
    Write-Host "工作目录: $projectDir" -ForegroundColor Cyan

    # 准备测试数据
    if ($SetupData) {
        Write-Host "正在准备测试数据..." -ForegroundColor Yellow
        & "$scriptDir\setup-test-data.ps1" -UserCount $MaxUsers -Environment $Environment
        if ($LASTEXITCODE -ne 0) {
            throw "测试数据准备失败"
        }
    }

    # 构建项目
    Write-Host "正在构建负载测试项目..." -ForegroundColor Yellow
    dotnet build --configuration Release
    if ($LASTEXITCODE -ne 0) {
        throw "项目构建失败"
    }

    # 更新配置文件
    Write-Host "正在更新测试配置..." -ForegroundColor Yellow
    $configPath = Join-Path $projectDir "appsettings.json"
    $config = Get-Content $configPath | ConvertFrom-Json
    
    $config.TestConfiguration.BaseUrl = $BaseUrl
    $config.TestConfiguration.IdentityApiUrl = $IdentityUrl
    $config.TestConfiguration.ExamApiUrl = $BaseUrl
    $config.TestConfiguration.TestUsers.Count = $MaxUsers
    
    $config | ConvertTo-Json -Depth 10 | Set-Content $configPath

    # 创建报告目录
    $reportDir = Join-Path $projectDir "reports"
    if (-not (Test-Path $reportDir)) {
        New-Item -ItemType Directory -Path $reportDir -Force
    }

    # 构建命令行参数
    $args = @()
    if ($Scenario -ne "all") {
        $args += "--scenario=$Scenario"
    }

    # 运行负载测试
    Write-Host "开始执行负载测试..." -ForegroundColor Green
    Write-Host "测试参数: $($args -join ' ')" -ForegroundColor Cyan
    
    $startTime = Get-Date
    dotnet run --configuration Release -- $args
    $endTime = Get-Date
    $duration = $endTime - $startTime

    if ($LASTEXITCODE -eq 0) {
        Write-Host "负载测试完成!" -ForegroundColor Green
        Write-Host "测试耗时: $($duration.ToString('hh\:mm\:ss'))" -ForegroundColor Cyan
        Write-Host "测试报告位置: $reportDir" -ForegroundColor Cyan
        
        # 显示报告文件
        $reportFiles = Get-ChildItem -Path $reportDir -Filter "*.html" | Sort-Object LastWriteTime -Descending
        if ($reportFiles.Count -gt 0) {
            $latestReport = $reportFiles[0]
            Write-Host "最新HTML报告: $($latestReport.FullName)" -ForegroundColor Green
            
            # 询问是否打开报告
            $openReport = Read-Host "是否打开测试报告? (y/N)"
            if ($openReport -eq "y" -or $openReport -eq "Y") {
                Start-Process $latestReport.FullName
            }
        }
    } else {
        throw "负载测试执行失败"
    }

    # 清理测试数据
    if ($CleanupAfter) {
        Write-Host "正在清理测试数据..." -ForegroundColor Yellow
        & "$scriptDir\cleanup-test-data.ps1" -Environment $Environment
    }

} catch {
    Write-Error "负载测试执行过程中发生错误: $($_.Exception.Message)"
    exit 1
} finally {
    # 恢复原始目录
    Pop-Location -ErrorAction SilentlyContinue
}

Write-Host "负载测试脚本执行完成!" -ForegroundColor Green
