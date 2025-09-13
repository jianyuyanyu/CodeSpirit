#!/usr/bin/env pwsh

<#
.SYNOPSIS
    修复多数据库迁移冲突的PowerShell脚本
    
.DESCRIPTION
    当出现"There is already an object named 'XXX' in the database"错误时，
    此脚本可以帮助清理和重置迁移状态。
    
.PARAMETER ApiProject
    API项目名称 (ExamApi, ConfigCenter, FileStorageApi, SurveyApi)
    
.PARAMETER DatabaseType
    数据库类型 (MySql, SqlServer)
    
.PARAMETER Action
    要执行的操作 (CheckStatus, ResetMigrations, ManualMarkApplied)
    
.PARAMETER MigrationName
    要标记为已应用的迁移名称（仅在ManualMarkApplied操作时需要）
    
.EXAMPLE
    .\Scripts\fix-migration-conflicts.ps1 -ApiProject ExamApi -DatabaseType SqlServer -Action CheckStatus
    
.EXAMPLE
    .\Scripts\fix-migration-conflicts.ps1 -ApiProject ExamApi -DatabaseType SqlServer -Action ResetMigrations
#>

param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("ExamApi", "ConfigCenter", "FileStorageApi", "SurveyApi", "Settings", "Messaging")]
    [string]$ApiProject,
    
    [Parameter(Mandatory = $true)]
    [ValidateSet("MySql", "SqlServer")]
    [string]$DatabaseType,
    
    [Parameter(Mandatory = $true)]
    [ValidateSet("CheckStatus", "ResetMigrations", "ManualMarkApplied")]
    [string]$Action,
    
    [Parameter(Mandatory = $false)]
    [string]$MigrationName
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

# 获取项目路径和DbContext类名
$ProjectPath = $ProjectPaths[$ApiProject]
$DbContextName = $DbContextMappings[$ApiProject][$DatabaseType]
$MigrationsDir = "Migrations/$DatabaseType"

if (-not $ProjectPath) {
    Write-Error "未找到项目 $ApiProject 的路径配置"
    exit 1
}

if (-not $DbContextName) {
    Write-Error "未找到 $ApiProject 项目的 $DatabaseType 数据库DbContext配置"
    exit 1
}

if (-not (Test-Path $ProjectPath)) {
    Write-Error "项目路径不存在: $ProjectPath"
    exit 1
}

Write-Host "========================================" -ForegroundColor Green
Write-Host "迁移冲突修复工具" -ForegroundColor Green
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
    # 设置环境变量指定数据库类型
    $env:DatabaseType = $DatabaseType
    
    switch ($Action) {
        "CheckStatus" {
            Write-Host "正在检查迁移状态..." -ForegroundColor Cyan
            
            # 检查应用的迁移
            Write-Host "`n已应用的迁移:" -ForegroundColor Yellow
            $appliedCommand = "dotnet ef migrations list --context $DbContextName"
            Write-Host "执行命令: $appliedCommand" -ForegroundColor Gray
            Invoke-Expression $appliedCommand
            
            # 检查待应用的迁移
            Write-Host "`n待应用的迁移:" -ForegroundColor Yellow
            $pendingCommand = "dotnet ef migrations list --context $DbContextName --pending"
            Write-Host "执行命令: $pendingCommand" -ForegroundColor Gray
            Invoke-Expression $pendingCommand
        }
        
        "ResetMigrations" {
            Write-Host "警告: 此操作将删除所有迁移文件并重新创建！" -ForegroundColor Red
            $confirm = Read-Host "确定要继续吗？(y/N)"
            
            if ($confirm -eq 'y' -or $confirm -eq 'Y') {
                # 删除迁移文件夹
                $migrationsPath = Join-Path $PWD $MigrationsDir
                if (Test-Path $migrationsPath) {
                    Write-Host "删除迁移文件夹: $migrationsPath" -ForegroundColor Yellow
                    Remove-Item $migrationsPath -Recurse -Force
                }
                
                # 删除数据库中的迁移历史表（需要手动执行SQL）
                Write-Host "`n请手动执行以下SQL命令来清理数据库中的迁移历史:" -ForegroundColor Yellow
                Write-Host "DELETE FROM __EFMigrationsHistory WHERE MigrationId LIKE '%$ApiProject%';" -ForegroundColor Cyan
                Write-Host "-- 或者完全删除迁移历史表（谨慎使用）:" -ForegroundColor Red
                Write-Host "-- DROP TABLE __EFMigrationsHistory;" -ForegroundColor Red
                
                # 重新创建初始迁移
                Write-Host "`n重新创建初始迁移..." -ForegroundColor Cyan
                $createCommand = "dotnet ef migrations add `"InitialCreate`" --context $DbContextName --output-dir `"$MigrationsDir`""
                Write-Host "执行命令: $createCommand" -ForegroundColor Gray
                Invoke-Expression $createCommand
                
                if ($LASTEXITCODE -eq 0) {
                    Write-Host "迁移重置成功！" -ForegroundColor Green
                    Write-Host "请执行上述SQL命令清理数据库，然后重新运行应用程序。" -ForegroundColor Yellow
                } else {
                    Write-Host "迁移重置失败！" -ForegroundColor Red
                }
            } else {
                Write-Host "操作已取消。" -ForegroundColor Yellow
            }
        }
        
        "ManualMarkApplied" {
            if (-not $MigrationName) {
                Write-Error "ManualMarkApplied操作需要指定MigrationName参数"
                exit 1
            }
            
            Write-Host "手动标记迁移为已应用: $MigrationName" -ForegroundColor Cyan
            Write-Host "请手动执行以下SQL命令:" -ForegroundColor Yellow
            
            # 生成SQL命令来标记迁移为已应用
            $migrationId = (Get-ChildItem "$MigrationsDir/*.cs" | Where-Object { $_.Name -like "*$MigrationName*" } | Select-Object -First 1).BaseName
            if ($migrationId) {
                $insertSql = "INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('$migrationId', '9.0.0');"
                Write-Host $insertSql -ForegroundColor Cyan
            } else {
                Write-Host "未找到迁移文件，请检查迁移名称是否正确。" -ForegroundColor Red
            }
        }
    }
}
catch {
    Write-Error "操作失败: $_"
    exit 1
}
finally {
    Pop-Location
    Remove-Item Env:\DatabaseType -ErrorAction SilentlyContinue
}

Write-Host "`n操作完成！" -ForegroundColor Green
