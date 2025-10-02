#!/usr/bin/env pwsh
<#
.SYNOPSIS
    使用 Aspire exec 运行数据库迁移
.DESCRIPTION
    利用 Aspire 9.5 的 aspire exec 命令，在应用环境上下文中执行 EF Core 迁移
.PARAMETER Service
    要运行迁移的服务名称（identity, exam, config, messaging, file, survey, approval）
.PARAMETER DatabaseType
    数据库类型（MySql 或 SqlServer）
.EXAMPLE
    .\run-migrations.ps1 -Service identity -DatabaseType MySql
#>

param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("identity", "exam", "config", "messaging", "file", "survey", "approval")]
    [string]$Service,

    [Parameter(Mandatory = $false)]
    [ValidateSet("MySql", "SqlServer")]
    [string]$DatabaseType = "MySql"
)

$ErrorActionPreference = "Stop"

Write-Host "🔄 准备运行 $Service 服务的数据库迁移（数据库类型: $DatabaseType）..." -ForegroundColor Cyan

# 服务名称映射
$serviceMap = @{
    "identity"  = "CodeSpirit.IdentityApi"
    "exam"      = "CodeSpirit.ExamApi"
    "config"    = "CodeSpirit.ConfigCenter"
    "messaging" = "CodeSpirit.MessagingApi"
    "file"      = "CodeSpirit.FileStorageApi"
    "survey"    = "CodeSpirit.SurveyApi"
    "approval"  = "CodeSpirit.ApprovalApi"
}

$projectName = $serviceMap[$Service]
$projectPath = Join-Path $PSScriptRoot "..\Src\ApiServices\$projectName"

if (-not (Test-Path $projectPath)) {
    Write-Error "❌ 项目路径不存在: $projectPath"
    exit 1
}

Write-Host "📁 项目路径: $projectPath" -ForegroundColor Green

# 使用 aspire exec 在应用环境上下文中运行迁移
# 这样可以继承所有环境变量和连接字符串
Write-Host "🚀 执行迁移命令..." -ForegroundColor Yellow

try {
    # 注意：需要先启动 AppHost，然后在另一个终端运行此脚本
    # aspire exec 会自动从运行中的 AppHost 获取环境变量和配置
    
    $migrationPath = "Migrations\$DatabaseType"
    
    aspire exec --resource $Service --workdir $projectPath -- `
        dotnet ef database update --context ApplicationDbContext --migrations-assembly $migrationPath
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ $Service 服务迁移成功完成！" -ForegroundColor Green
    }
    else {
        Write-Error "❌ 迁移失败，退出代码: $LASTEXITCODE"
    }
}
catch {
    Write-Error "❌ 执行迁移时发生错误: $_"
    exit 1
}

Write-Host "`n✨ 迁移操作完成" -ForegroundColor Cyan

