#!/usr/bin/env pwsh

<#
.SYNOPSIS
    管理多数据库迁移的PowerShell脚本
    
.DESCRIPTION
    此脚本用于管理支持MySQL和SQL Server的API项目的数据库迁移。
    支持添加、删除、列出和更新迁移操作。
    
.PARAMETER ApiProject
    API项目名称 (ExamApi, ConfigCenter, FileStorageApi, SurveyApi)
    
.PARAMETER DatabaseType
    数据库类型 (MySql, SqlServer)
    
.PARAMETER Action
    要执行的操作 (Add, Remove, List, Update)
    
.PARAMETER MigrationName
    迁移名称（仅在Add和Remove操作时需要）
    
.PARAMETER TargetMigration
    目标迁移（仅在Update操作时可选）
    
.EXAMPLE
    .\Scripts\manage-multi-database-migrations.ps1 -ApiProject ExamApi -DatabaseType MySql -Action Add -MigrationName "InitialCreate"
    
.EXAMPLE
    .\Scripts\manage-multi-database-migrations.ps1 -ApiProject ConfigCenter -DatabaseType SqlServer -Action List
    
.EXAMPLE
    .\Scripts\manage-multi-database-migrations.ps1 -ApiProject SurveyApi -DatabaseType MySql -Action Update
#>

param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("ExamApi", "ConfigCenter", "FileStorageApi", "SurveyApi", "Settings", "Messaging")]
    [string]$ApiProject,
    
    [Parameter(Mandatory = $true)]
    [ValidateSet("MySql", "SqlServer")]
    [string]$DatabaseType,
    
    [Parameter(Mandatory = $true)]
    [ValidateSet("Add", "Remove", "List", "Update")]
    [string]$Action,
    
    [Parameter(Mandatory = $false)]
    [string]$MigrationName,
    
    [Parameter(Mandatory = $false)]
    [string]$TargetMigration
)

# 项目路径映射
$ProjectPaths = @{
    "ExamApi" = "Src/ApiServices/CodeSpirit.ExamApi"
    "ConfigCenter" = "Src/ApiServices/CodeSpirit.ConfigCenter"
    "FileStorageApi" = "Src/ApiServices/CodeSpirit.FileStorageApi"
    "SurveyApi" = "Src/ApiServices/CodeSpirit.SurveyApi"
    "Settings" = "Src/Components/CodeSpirit.Settings"
    "Messaging" = "Src/Components/CodeSpirit.Messaging"
}

# DbContext类名映射
$DbContextMappings = @{
    "ExamApi" = @{
        "MySql" = "MySqlExamDbContext"
        "SqlServer" = "SqlServerExamDbContext"
    }
    "ConfigCenter" = @{
        "MySql" = "MySqlConfigDbContext"
        "SqlServer" = "SqlServerConfigDbContext"
    }
    "FileStorageApi" = @{
        "MySql" = "MySqlFileStorageDbContext"
        "SqlServer" = "SqlServerFileStorageDbContext"
    }
    "SurveyApi" = @{
        "MySql" = "MySqlSurveyDbContext"
        "SqlServer" = "SqlServerSurveyDbContext"
    }
    "Settings" = @{
        "MySql" = "MySqlSettingsDbContext"
        "SqlServer" = "SqlServerSettingsDbContext"
    }
    "Messaging" = @{
        "MySql" = "MySqlMessagingDbContext"
        "SqlServer" = "SqlServerMessagingDbContext"
    }
}

# 获取项目路径
$ProjectPath = $ProjectPaths[$ApiProject]
if (-not $ProjectPath) {
    Write-Error "未找到项目 $ApiProject 的路径配置"
    exit 1
}

# 检查项目是否存在
if (-not (Test-Path $ProjectPath)) {
    Write-Error "项目路径不存在: $ProjectPath"
    exit 1
}

# 获取DbContext类名
$DbContextName = $DbContextMappings[$ApiProject][$DatabaseType]
if (-not $DbContextName) {
    Write-Error "未找到 $ApiProject 项目的 $DatabaseType 数据库DbContext配置"
    exit 1
}

# 设置迁移输出目录
$MigrationsDir = "Migrations/$DatabaseType"

Write-Host "========================================" -ForegroundColor Green
Write-Host "多数据库迁移管理工具" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host "项目: $ApiProject" -ForegroundColor Yellow
Write-Host "数据库类型: $DatabaseType" -ForegroundColor Yellow
Write-Host "DbContext: $DbContextName" -ForegroundColor Yellow
Write-Host "迁移目录: $MigrationsDir" -ForegroundColor Yellow
Write-Host "操作: $Action" -ForegroundColor Yellow
Write-Host "========================================" -ForegroundColor Green

# 切换到项目目录
Push-Location $ProjectPath

try {
    switch ($Action) {
        "Add" {
            if (-not $MigrationName) {
                Write-Error "添加迁移时必须指定迁移名称"
                exit 1
            }
            
            Write-Host "正在添加迁移: $MigrationName" -ForegroundColor Cyan
            
            # 设置环境变量指定数据库类型
            $env:DatabaseType = $DatabaseType
            
            # 执行迁移添加命令
            $command = "dotnet ef migrations add `"$MigrationName`" --context $DbContextName --output-dir `"$MigrationsDir`""
            Write-Host "执行命令: $command" -ForegroundColor Gray
            
            Invoke-Expression $command
            
            if ($LASTEXITCODE -eq 0) {
                Write-Host "✅ 迁移添加成功!" -ForegroundColor Green
            } else {
                Write-Error "❌ 迁移添加失败!"
                exit 1
            }
        }
        
        "Remove" {
            if ($MigrationName) {
                Write-Host "正在删除指定迁移: $MigrationName" -ForegroundColor Cyan
                $command = "dotnet ef migrations remove --context $DbContextName --force"
            } else {
                Write-Host "正在删除最后一个迁移" -ForegroundColor Cyan
                $command = "dotnet ef migrations remove --context $DbContextName"
            }
            
            # 设置环境变量指定数据库类型
            $env:DatabaseType = $DatabaseType
            
            Write-Host "执行命令: $command" -ForegroundColor Gray
            Invoke-Expression $command
            
            if ($LASTEXITCODE -eq 0) {
                Write-Host "✅ 迁移删除成功!" -ForegroundColor Green
            } else {
                Write-Error "❌ 迁移删除失败!"
                exit 1
            }
        }
        
        "List" {
            Write-Host "正在列出迁移历史..." -ForegroundColor Cyan
            
            # 设置环境变量指定数据库类型
            $env:DatabaseType = $DatabaseType
            
            $command = "dotnet ef migrations list --context $DbContextName"
            Write-Host "执行命令: $command" -ForegroundColor Gray
            
            Invoke-Expression $command
        }
        
        "Update" {
            if ($TargetMigration) {
                Write-Host "正在更新数据库到指定迁移: $TargetMigration" -ForegroundColor Cyan
                $command = "dotnet ef database update `"$TargetMigration`" --context $DbContextName"
            } else {
                Write-Host "正在更新数据库到最新迁移..." -ForegroundColor Cyan
                $command = "dotnet ef database update --context $DbContextName"
            }
            
            # 设置环境变量指定数据库类型
            $env:DatabaseType = $DatabaseType
            
            Write-Host "执行命令: $command" -ForegroundColor Gray
            Invoke-Expression $command
            
            if ($LASTEXITCODE -eq 0) {
                Write-Host "✅ 数据库更新成功!" -ForegroundColor Green
            } else {
                Write-Error "❌ 数据库更新失败!"
                exit 1
            }
        }
    }
} finally {
    # 清理环境变量
    Remove-Item Env:DatabaseType -ErrorAction SilentlyContinue
    
    # 返回原目录
    Pop-Location
}

Write-Host "========================================" -ForegroundColor Green
Write-Host "操作完成!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
