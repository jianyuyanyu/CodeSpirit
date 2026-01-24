# 验证迁移文件是否使用了正确的 DbContext
# 使用方式: .\validate-migration.ps1 -MigrationFile "Data/Migrations/MySql/20260123_AddProduct.cs"

param(
    [Parameter(Mandatory=$true)]
    [string]$MigrationFile
)

if (-not (Test-Path $MigrationFile)) {
    Write-Host "错误: 迁移文件不存在: $MigrationFile" -ForegroundColor Red
    exit 1
}

$content = Get-Content $MigrationFile -Raw

# 检查是否使用了数据库特定的 DbContext
$isMySql = $MigrationFile -match "MySql"
$isSqlServer = $MigrationFile -match "SqlServer"

if ($isMySql) {
    if ($content -match "MySql\w+DbContext") {
        Write-Host "✓ MySQL 迁移文件使用了正确的 DbContext" -ForegroundColor Green
    } else {
        Write-Host "✗ 错误: MySQL 迁移文件应使用 MySql{Service}DbContext" -ForegroundColor Red
        exit 1
    }
} elseif ($isSqlServer) {
    if ($content -match "SqlServer\w+DbContext") {
        Write-Host "✓ SQL Server 迁移文件使用了正确的 DbContext" -ForegroundColor Green
    } else {
        Write-Host "✗ 错误: SQL Server 迁移文件应使用 SqlServer{Service}DbContext" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "警告: 无法确定数据库类型（文件路径应包含 MySql 或 SqlServer）" -ForegroundColor Yellow
}

# 检查是否配置了 ValueGeneratedNever（如果适用）
if ($content -match "\.Id\s*=\s*") {
    if ($content -match "ValueGeneratedNever") {
        Write-Host "✓ 已配置 ValueGeneratedNever()" -ForegroundColor Green
    } else {
        Write-Host "警告: 检测到 ID 字段，但未找到 ValueGeneratedNever() 配置" -ForegroundColor Yellow
        Write-Host "  如果使用雪花 ID，请在实体配置中添加: builder.Property(x => x.Id).ValueGeneratedNever();" -ForegroundColor Yellow
    }
}

Write-Host "`n验证完成" -ForegroundColor Green
