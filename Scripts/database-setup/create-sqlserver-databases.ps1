# =============================================
# CodeSpirit SQL Server 数据库创建 PowerShell 脚本
# 执行 SQL 脚本创建数据库和用户
# =============================================

param(
    [Parameter(Mandatory=$false)]
    [string]$ServerInstance = "10.0.1.15",
    
    [Parameter(Mandatory=$false)]
    [string]$AdminUser = "sa",
    
    [Parameter(Mandatory=$false)]
    [string]$AdminPassword,
    
    [Parameter(Mandatory=$false)]
    [switch]$UseWindowsAuth = $false
)

# 脚本路径
$ScriptPath = Split-Path -Parent $MyInvocation.MyCommand.Definition
$SqlScriptPath = Join-Path $ScriptPath "create-sqlserver-databases.sql"

Write-Host "CodeSpirit SQL Server 数据库创建脚本" -ForegroundColor Green
Write-Host "=================================" -ForegroundColor Green
Write-Host ""

# 检查 SQL 脚本文件是否存在
if (-not (Test-Path $SqlScriptPath)) {
    Write-Error "SQL 脚本文件不存在: $SqlScriptPath"
    exit 1
}

# 检查是否安装了 SqlServer 模块
if (-not (Get-Module -ListAvailable -Name SqlServer)) {
    Write-Host "正在安装 SqlServer PowerShell 模块..." -ForegroundColor Yellow
    try {
        Install-Module -Name SqlServer -Force -AllowClobber -Scope CurrentUser
        Write-Host "SqlServer 模块安装成功" -ForegroundColor Green
    }
    catch {
        Write-Error "无法安装 SqlServer 模块: $_"
        Write-Host "请手动安装: Install-Module -Name SqlServer -Force" -ForegroundColor Yellow
        exit 1
    }
}

# 导入 SqlServer 模块
Import-Module SqlServer -Force

# 构建连接参数
$ConnectionParams = @{
    ServerInstance = $ServerInstance
    Database = "master"
}

if ($UseWindowsAuth) {
    Write-Host "使用 Windows 身份验证连接到 SQL Server..." -ForegroundColor Cyan
}
else {
    if (-not $AdminPassword) {
        $SecurePassword = Read-Host "请输入 SQL Server 管理员密码 ($AdminUser)" -AsSecureString
        $AdminPassword = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($SecurePassword))
    }
    
    $ConnectionParams.Username = $AdminUser
    $ConnectionParams.Password = $AdminPassword
    
    Write-Host "使用 SQL Server 身份验证连接..." -ForegroundColor Cyan
}

Write-Host "服务器: $ServerInstance" -ForegroundColor Cyan
Write-Host ""

try {
    # 测试连接
    Write-Host "测试数据库连接..." -ForegroundColor Yellow
    $TestQuery = "SELECT @@VERSION as Version, @@SERVERNAME as ServerName"
    $TestResult = Invoke-Sqlcmd @ConnectionParams -Query $TestQuery -ErrorAction Stop
    
    Write-Host "连接成功!" -ForegroundColor Green
    Write-Host "服务器版本: $($TestResult.Version)" -ForegroundColor Cyan
    Write-Host "服务器名称: $($TestResult.ServerName)" -ForegroundColor Cyan
    Write-Host ""
    
    # 执行创建脚本
    Write-Host "执行数据库创建脚本..." -ForegroundColor Yellow
    Write-Host "脚本路径: $SqlScriptPath" -ForegroundColor Cyan
    Write-Host ""
    
    $Result = Invoke-Sqlcmd @ConnectionParams -InputFile $SqlScriptPath -Verbose -ErrorAction Stop
    
    Write-Host ""
    Write-Host "数据库创建脚本执行完成!" -ForegroundColor Green
    Write-Host ""
    
    # 验证创建的数据库
    Write-Host "验证创建的数据库..." -ForegroundColor Yellow
    $DatabaseList = @(
        'codespirit-identity',
        'codespirit-exam', 
        'codespirit-messaging',
        'codespirit-config',
        'codespirit-settings',
        'codespirit-file',
        'codespirit-survey'
    )
    
    foreach ($DbName in $DatabaseList) {
        $CheckQuery = "SELECT name FROM sys.databases WHERE name = '$DbName'"
        $DbExists = Invoke-Sqlcmd @ConnectionParams -Query $CheckQuery
        
        if ($DbExists) {
            Write-Host "✓ 数据库 '$DbName' 创建成功" -ForegroundColor Green
        }
        else {
            Write-Host "✗ 数据库 '$DbName' 创建失败" -ForegroundColor Red
        }
    }
    
    Write-Host ""
    Write-Host "连接字符串示例:" -ForegroundColor Cyan
    Write-Host "Server=$ServerInstance;Database={数据库名};User ID=codespirit;Password=123456abcD..;TrustServerCertificate=True;" -ForegroundColor Yellow
    
}
catch {
    Write-Error "执行失败: $_"
    Write-Host ""
    Write-Host "可能的解决方案:" -ForegroundColor Yellow
    Write-Host "1. 检查 SQL Server 服务是否正在运行" -ForegroundColor White
    Write-Host "2. 检查服务器地址和端口是否正确" -ForegroundColor White
    Write-Host "3. 检查管理员用户名和密码是否正确" -ForegroundColor White
    Write-Host "4. 检查防火墙设置" -ForegroundColor White
    Write-Host "5. 确保有足够的权限创建数据库" -ForegroundColor White
    exit 1
}

Write-Host ""
Write-Host "脚本执行完成!" -ForegroundColor Green
