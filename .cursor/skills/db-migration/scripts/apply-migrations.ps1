# 一键应用 MySQL 和 SQL Server 迁移
# 使用方式: .\apply-migrations.ps1 -ServiceName "ExamApi"

param(
    [Parameter(Mandatory=$true)]
    [string]$ServiceName,
    
    [string]$ProjectPath = "Src/ApiServices/CodeSpirit.$ServiceName"
)

$projectPath = Join-Path $PSScriptRoot "..\..\..\.." $ProjectPath

if (-not (Test-Path $projectPath)) {
    Write-Host "错误: 项目路径不存在: $projectPath" -ForegroundColor Red
    exit 1
}

Push-Location $projectPath

try {
    Write-Host "开始应用迁移..." -ForegroundColor Cyan
    
    # 应用 MySQL 迁移
    Write-Host "`n应用 MySQL 迁移..." -ForegroundColor Yellow
    $mySqlContext = "MySql$($ServiceName.Replace('Api', ''))DbContext"
    dotnet ef database update --context $mySqlContext
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "✗ MySQL 迁移失败" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "✓ MySQL 迁移成功" -ForegroundColor Green
    
    # 应用 SQL Server 迁移
    Write-Host "`n应用 SQL Server 迁移..." -ForegroundColor Yellow
    $sqlServerContext = "SqlServer$($ServiceName.Replace('Api', ''))DbContext"
    dotnet ef database update --context $sqlServerContext
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "✗ SQL Server 迁移失败" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "✓ SQL Server 迁移成功" -ForegroundColor Green
    
    Write-Host "`n所有迁移已成功应用" -ForegroundColor Green
} finally {
    Pop-Location
}
