# PowerShell脚本：自动运行单元测试并生成报告
# 使用方法：.\run-tests.ps1 [选项]
# 选项：
#   -NoBuild: 跳过构建步骤
#   -NoReport: 跳过报告生成

param (
    [switch]$NoBuild,
    [switch]$NoReport
)

$ErrorActionPreference = "Stop"
$ReportDir = "./TestResults"
$ProjectPath = "./Src/Tests/Components/CodeSpirit.Charts.Tests"

# 显示测试标题
Write-Host "=====================================================" -ForegroundColor Cyan
Write-Host "   CodeSpirit.Charts 组件单元测试自动化执行脚本" -ForegroundColor Cyan
Write-Host "=====================================================" -ForegroundColor Cyan
Write-Host ""

# 确保报告目录存在
if (-not (Test-Path $ReportDir)) {
    New-Item -ItemType Directory -Path $ReportDir | Out-Null
    Write-Host "创建测试报告目录: $ReportDir" -ForegroundColor Gray
}

# 执行构建步骤
if (-not $NoBuild) {
    Write-Host "正在构建项目..." -ForegroundColor Yellow
    dotnet build $ProjectPath -c Release
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "构建失败，错误代码: $LASTEXITCODE" -ForegroundColor Red
        exit $LASTEXITCODE
    }
    
    Write-Host "项目构建成功!" -ForegroundColor Green
    Write-Host ""
}

# 运行测试
Write-Host "正在运行单元测试..." -ForegroundColor Yellow
if ($NoReport) {
    # 简单模式，不生成报告
    dotnet test $ProjectPath --no-build -c Release
} else {
    # 生成详细覆盖率报告
    dotnet test $ProjectPath `
        --no-build `
        -c Release `
        --logger "trx;LogFileName=TestResults.trx" `
        /p:CollectCoverage=true `
        /p:CoverletOutputFormat=cobertura `
        /p:CoverletOutput="$ReportDir/coverage.cobertura.xml"
        
    if ($LASTEXITCODE -ne 0) {
        Write-Host "测试失败，错误代码: $LASTEXITCODE" -ForegroundColor Red
    } else {
        Write-Host "所有测试通过!" -ForegroundColor Green
        
        # 生成 HTML 报告
        Write-Host "正在生成HTML覆盖率报告..." -ForegroundColor Yellow
        
        # 检查是否安装了 reportgenerator
        $reportGenInstalled = Get-Command reportgenerator -ErrorAction SilentlyContinue
        
        if ($null -eq $reportGenInstalled) {
            Write-Host "未找到reportgenerator工具，正在安装..." -ForegroundColor Yellow
            dotnet tool install -g dotnet-reportgenerator-globaltool
        }
        
        reportgenerator `
            "-reports:$ReportDir/coverage.cobertura.xml" `
            "-targetdir:$ReportDir/html" `
            "-reporttypes:Html"
            
        Write-Host "HTML覆盖率报告已生成: $ReportDir/html/index.html" -ForegroundColor Green
    }
}

Write-Host ""
Write-Host "=====================================================" -ForegroundColor Cyan
Write-Host "   测试执行完成" -ForegroundColor Cyan
Write-Host "=====================================================" -ForegroundColor Cyan 