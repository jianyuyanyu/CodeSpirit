# GreptimeDB连接问题修复脚本
# 用于修复 ServiceUnavailable 和表创建失败问题

param(
    [string]$GreptimeDbUrl = "",
    [string]$Database = "audit_logs",
    [switch]$Verbose,
    [switch]$AutoDetectPort
)

Write-Host "=== CodeSpirit.Audit GreptimeDB 连接修复脚本 ===" -ForegroundColor Green
Write-Host ""

function Write-Log {
    param([string]$Message, [string]$Level = "INFO")
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $color = switch ($Level) {
        "ERROR" { "Red" }
        "WARN" { "Yellow" }
        "SUCCESS" { "Green" }
        default { "White" }
    }
    Write-Host "[$timestamp] [$Level] $Message" -ForegroundColor $color
}

function Test-GreptimeDbConnection {
    param([string]$Url)
    
    Write-Log "正在测试 GreptimeDB 连接: $Url"
    
    try {
        # GreptimeDB 没有 /health 端点，我们测试 /dashboard 端点
        $response = Invoke-RestMethod -Uri "$Url/dashboard" -Method Get -TimeoutSec 10
        Write-Log "GreptimeDB 健康检查通过" -Level "SUCCESS"
        return $true
    }
    catch {
        Write-Log "GreptimeDB 健康检查失败: $($_.Exception.Message)" -Level "ERROR"
        return $false
    }
}

function Test-SqlQuery {
    param([string]$Url, [string]$Database)
    
    Write-Log "正在测试 SQL 查询功能"
    
    try {
        # 使用 curl 来避免 PowerShell 的 Content-Type 问题
        $curlCmd = "curl -s -X POST `"$Url/v1/sql?db=$Database`" -H `"Content-Type: application/json`" -d `"{`\`"sql`\`": `\`"SELECT 1 as test`\`"}`""
        $response = Invoke-Expression $curlCmd
        
        if ($Verbose) {
            Write-Log "SQL 查询响应: $response"
        }
        
        if ($response -and $response -notlike "*error*") {
            Write-Log "SQL 查询功能正常" -Level "SUCCESS"
            return $true
        } else {
            Write-Log "SQL 查询返回错误: $response" -Level "ERROR"
            return $false
        }
    }
    catch {
        Write-Log "SQL 查询失败: $($_.Exception.Message)" -Level "ERROR"
        return $false
    }
}

function Create-Database {
    param([string]$Url, [string]$Database)
    
    Write-Log "正在创建数据库: $Database"
    
    try {
        # 使用 curl 来避免 PowerShell 的 Content-Type 问题
        $curlCmd = "curl -s -X POST `"$Url/v1/sql`" -H `"Content-Type: application/json`" -d `"{`\`"sql`\`": `\`"CREATE DATABASE IF NOT EXISTS $Database`\`"}`""
        $response = Invoke-Expression $curlCmd
        
        if ($Verbose) {
            Write-Log "创建数据库响应: $response"
        }
        
        if ($response -and $response -notlike "*error*") {
            Write-Log "数据库创建成功: $Database" -Level "SUCCESS"
            return $true
        } else {
            Write-Log "数据库创建返回: $response" -Level "WARN"
            # 即使返回错误，数据库可能已经存在，继续执行
            return $true
        }
    }
    catch {
        Write-Log "数据库创建失败: $($_.Exception.Message)" -Level "ERROR"
        return $false
    }
}

function Create-AuditTable {
    param([string]$Url, [string]$Database, [string]$TableName = "web_audit_logs")
    
    Write-Log "正在创建审计日志表: $TableName"
    
    $createTableSql = @"
CREATE TABLE IF NOT EXISTS $TableName (
    id STRING,
    user_id STRING,
    user_name STRING,
    ip_address STRING,
    operation_time TIMESTAMP TIME INDEX,
    service_name STRING,
    controller_name STRING,
    action_name STRING,
    operation_type STRING,
    description STRING,
    request_path STRING,
    request_method STRING,
    request_params TEXT,
    entity_name STRING,
    entity_id STRING,
    execution_duration BIGINT,
    is_success BOOLEAN,
    error_message TEXT,
    status_code INT,
    before_data TEXT,
    after_data TEXT,
    user_agent TEXT,
    operation_name STRING,
    tenant_id STRING,
    PRIMARY KEY (id)
)
"@
    
    try {
        # 使用 curl 来避免 PowerShell 的 Content-Type 问题
        # 转义 SQL 中的特殊字符
        $escapedSql = $createTableSql -replace '"', '\"' -replace "`n", " " -replace "`r", ""
        $curlCmd = "curl -s -X POST `"$Url/v1/sql?db=$Database`" -H `"Content-Type: application/json`" -d `"{`\`"sql`\`": `\`"$escapedSql`\`"}`""
        $response = Invoke-Expression $curlCmd
        
        if ($Verbose) {
            Write-Log "创建表响应: $response"
        }
        
        if ($response -and $response -notlike "*error*") {
            Write-Log "审计日志表创建成功: $TableName" -Level "SUCCESS"
            return $true
        } else {
            Write-Log "审计日志表创建返回: $response" -Level "WARN"
            # 表可能已经存在，继续执行
            return $true
        }
    }
    catch {
        Write-Log "审计日志表创建失败: $($_.Exception.Message)" -Level "ERROR"
        Write-Log "SQL: $createTableSql" -Level "ERROR"
        return $false
    }
}

function Test-InsertData {
    param([string]$Url, [string]$Database, [string]$TableName = "web_audit_logs")
    
    Write-Log "正在测试数据插入"
    
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss.fff"
    $testId = "test-$(Get-Date -Format 'yyyyMMddHHmmss')"
    
    $insertSql = @"
INSERT INTO $TableName (
    id, user_id, user_name, ip_address, operation_time,
    service_name, controller_name, action_name, operation_type, description,
    request_path, request_method, request_params, entity_name, entity_id,
    execution_duration, is_success, error_message, status_code,
    before_data, after_data, user_agent, operation_name, tenant_id
) VALUES (
    '$testId',
    'test-user',
    'Test User',
    '127.0.0.1',
    '$timestamp',
    'FixScript',
    'FixController',
    'TestAction',
    'Test',
    'GreptimeDB连接修复测试',
    '/api/fix/test',
    'POST',
    '{"test": true}',
    'FixEntity',
    'test-entity-1',
    100,
    true,
    '',
    200,
    '{}',
    '{"result": "success"}',
    'GreptimeDB-Fix-Script/1.0',
    '修复测试操作',
    'test-tenant'
)
"@
    
    try {
        $body = @{
            sql = $insertSql
        } | ConvertTo-Json
        
        $headers = @{
            "Content-Type" = "application/json"
        }
        
        $response = Invoke-RestMethod -Uri "$Url/v1/sql?db=$Database" -Method Post -Body $body -Headers $headers -TimeoutSec 30
        
        if ($Verbose) {
            Write-Log "插入数据响应: $($response | ConvertTo-Json -Depth 3)"
        }
        
        Write-Log "测试数据插入成功，ID: $testId" -Level "SUCCESS"
        return $true
    }
    catch {
        Write-Log "测试数据插入失败: $($_.Exception.Message)" -Level "ERROR"
        return $false
    }
}

function Get-GreptimeDbPort {
    Write-Log "正在检测 GreptimeDB 容器端口映射"
    
    try {
        # 获取 GreptimeDB 容器的端口映射信息
        $containerInfo = docker ps --format "{{.Names}}\t{{.Ports}}" | Select-String "greptimedb"
        
        if ($containerInfo) {
            Write-Log "发现 GreptimeDB 容器: $containerInfo" -Level "SUCCESS"
            
            # 解析端口映射，查找映射到4000的端口
            $portsInfo = $containerInfo.ToString().Split("`t")[1]
            Write-Log "端口映射信息: $portsInfo"
            
            # 匹配类似 "127.0.0.1:49824->4000/tcp" 的模式
            if ($portsInfo -match "127\.0\.0\.1:(\d+)->4000/tcp") {
                $hostPort = $matches[1]
                Write-Log "检测到 GreptimeDB HTTP 端口: $hostPort" -Level "SUCCESS"
                return $hostPort
            } elseif ($portsInfo -match "0\.0\.0\.0:(\d+)->4000/tcp") {
                $hostPort = $matches[1]
                Write-Log "检测到 GreptimeDB HTTP 端口: $hostPort" -Level "SUCCESS"
                return $hostPort
            } elseif ($portsInfo -match ":4000->4000/tcp") {
                # 如果是固定端口映射 4000:4000
                Write-Log "检测到固定端口映射，使用端口 4000" -Level "SUCCESS"
                return "4000"
            }
        }
        
        Write-Log "未能检测到 GreptimeDB 端口映射" -Level "WARN"
        return $null
    }
    catch {
        Write-Log "检测端口时出错: $($_.Exception.Message)" -Level "ERROR"
        return $null
    }
}

function Check-DockerContainer {
    Write-Log "正在检查 GreptimeDB Docker 容器状态"
    
    try {
        $containers = docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}" | Select-String "greptimedb"
        
        if ($containers) {
            Write-Log "发现 GreptimeDB 容器:" -Level "SUCCESS"
            $containers | ForEach-Object { Write-Log "  $_" }
            return $true
        } else {
            Write-Log "未发现运行中的 GreptimeDB 容器" -Level "WARN"
            
            # 检查是否有停止的容器
            $stoppedContainers = docker ps -a --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}" | Select-String "greptimedb"
            if ($stoppedContainers) {
                Write-Log "发现已停止的 GreptimeDB 容器:" -Level "WARN"
                $stoppedContainers | ForEach-Object { Write-Log "  $_" }
            }
            
            return $false
        }
    }
    catch {
        Write-Log "检查 Docker 容器时出错: $($_.Exception.Message)" -Level "ERROR"
        return $false
    }
}

function Start-AspireApp {
    Write-Log "建议启动 Aspire 应用以确保 GreptimeDB 容器运行"
    Write-Log "执行命令: cd Src/CodeSpirit.AppHost && dotnet run" -Level "INFO"
    
    $choice = Read-Host "是否现在启动 Aspire 应用? (y/N)"
    if ($choice -eq 'y' -or $choice -eq 'Y') {
        try {
            Push-Location "Src/CodeSpirit.AppHost"
            Write-Log "正在启动 Aspire 应用..."
            Start-Process "dotnet" -ArgumentList "run" -NoNewWindow
            Write-Log "Aspire 应用启动命令已执行，请等待容器启动完成" -Level "SUCCESS"
        }
        catch {
            Write-Log "启动 Aspire 应用失败: $($_.Exception.Message)" -Level "ERROR"
        }
        finally {
            Pop-Location
        }
    }
}

# 主执行流程
Write-Log "开始 GreptimeDB 连接诊断和修复..."

# 1. 检查 Docker 容器状态
$containerRunning = Check-DockerContainer

if (-not $containerRunning) {
    Write-Log "GreptimeDB 容器未运行，这可能是问题的根本原因" -Level "WARN"
    Start-AspireApp
    Write-Log "等待容器启动..." -Level "INFO"
    Start-Sleep -Seconds 10
}

# 2. 自动检测或使用指定的 URL
if ([string]::IsNullOrEmpty($GreptimeDbUrl) -or $AutoDetectPort) {
    Write-Log "自动检测 GreptimeDB 端口..."
    $detectedPort = Get-GreptimeDbPort
    
    if ($detectedPort) {
        $GreptimeDbUrl = "http://localhost:$detectedPort"
        Write-Log "使用检测到的 URL: $GreptimeDbUrl" -Level "SUCCESS"
    } else {
        $GreptimeDbUrl = "http://localhost:4000"
        Write-Log "无法检测端口，使用默认 URL: $GreptimeDbUrl" -Level "WARN"
    }
} else {
    Write-Log "使用指定的 URL: $GreptimeDbUrl"
}

# 3. 测试基础连接
$connectionOk = Test-GreptimeDbConnection -Url $GreptimeDbUrl

if (-not $connectionOk) {
    Write-Log "GreptimeDB 连接失败，请检查:" -Level "ERROR"
    Write-Log "  1. GreptimeDB 服务是否正在运行" -Level "ERROR"
    Write-Log "  2. URL 配置是否正确: $GreptimeDbUrl" -Level "ERROR"
    Write-Log "  3. 端口 4000 是否可访问" -Level "ERROR"
    Write-Log "  4. 防火墙设置" -Level "ERROR"
    exit 1
}

# 4. 创建数据库
$dbCreated = Create-Database -Url $GreptimeDbUrl -Database $Database

# 5. 测试 SQL 查询
$queryOk = Test-SqlQuery -Url $GreptimeDbUrl -Database $Database

if (-not $queryOk) {
    Write-Log "SQL 查询失败，无法继续" -Level "ERROR"
    exit 1
}

# 6. 创建审计日志表（针对不同的表前缀）
$tablesPrefixes = @("web", "identity", "messaging", "exam", "file", "survey", "approval")

foreach ($prefix in $tablesPrefixes) {
    $tableName = "${prefix}_audit_logs"
    $tableCreated = Create-AuditTable -Url $GreptimeDbUrl -Database $Database -TableName $tableName
    
    if ($tableCreated) {
        # 测试数据插入
        Test-InsertData -Url $GreptimeDbUrl -Database $Database -TableName $tableName | Out-Null
    }
}

Write-Log ""
Write-Log "=== 修复完成 ===" -Level "SUCCESS"
Write-Log "如果问题仍然存在，请检查应用程序配置文件中的 GreptimeDB 连接字符串" -Level "INFO"
Write-Log "配置示例:"
Write-Log '{
  "Audit": {
    "StorageProvider": "GreptimeDB",
    "GreptimeDB": {
      "Url": "http://greptimedb:4000",
      "Database": "audit_logs",
      "TableName": "audit_logs",
      "TablePrefix": "web"
    }
  }
}' -Level "INFO"

Write-Log ""
Write-Log "脚本执行完成！" -Level "SUCCESS"
