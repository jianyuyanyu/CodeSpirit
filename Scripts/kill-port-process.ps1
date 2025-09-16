# CodeSpirit 端口占用检测和释放脚本
# 用于解决 Aspire 应用程序端口冲突问题

param(
    [Parameter(Mandatory=$false)]
    [int]$Port,
    
    [Parameter(Mandatory=$false)]
    [switch]$KillAll,
    
    [Parameter(Mandatory=$false)]
    [switch]$ShowOnly,
    
    [Parameter(Mandatory=$false)]
    [switch]$Help
)

# 显示帮助信息
function Show-Help {
    Write-Host "CodeSpirit 端口占用检测和释放脚本" -ForegroundColor Green
    Write-Host ""
    Write-Host "用法:" -ForegroundColor Yellow
    Write-Host "  .\kill-port-process.ps1 -Port 3306                # 释放指定端口"
    Write-Host "  .\kill-port-process.ps1 -ShowOnly -Port 3306      # 仅显示端口占用情况"
    Write-Host "  .\kill-port-process.ps1 -KillAll                  # 释放所有 Aspire 相关端口"
    Write-Host "  .\kill-port-process.ps1 -Help                     # 显示帮助信息"
    Write-Host ""
    Write-Host "参数说明:" -ForegroundColor Yellow
    Write-Host "  -Port      指定要检查/释放的端口号"
    Write-Host "  -KillAll   释放所有常用的 Aspire 服务端口"
    Write-Host "  -ShowOnly  仅显示端口占用情况，不执行释放操作"
    Write-Host "  -Help      显示此帮助信息"
    Write-Host ""
    Write-Host "常用端口:" -ForegroundColor Cyan
    Write-Host "  3306  - MySQL"
    Write-Host "  1433  - SQL Server"
    Write-Host "  6380  - Redis"
    Write-Host "  5672  - RabbitMQ"
    Write-Host "  9200  - Elasticsearch"
    Write-Host "  5341  - Seq"
}

# 检查端口占用情况
function Get-PortProcess {
    param([int]$PortNumber)
    
    try {
        $connections = Get-NetTCPConnection -LocalPort $PortNumber -ErrorAction SilentlyContinue
        if ($connections) {
            foreach ($conn in $connections) {
                $process = Get-Process -Id $conn.OwningProcess -ErrorAction SilentlyContinue
                if ($process) {
                    return @{
                        Port = $PortNumber
                        ProcessId = $process.Id
                        ProcessName = $process.ProcessName
                        State = $conn.State
                    }
                }
            }
        }
    }
    catch {
        # 端口未被占用或其他错误
    }
    
    return $null
}

# 释放端口进程
function Kill-PortProcess {
    param([int]$PortNumber, [bool]$ShowOnlyFlag = $false)
    
    $portInfo = Get-PortProcess -PortNumber $PortNumber
    
    if ($portInfo) {
        Write-Host "端口 $PortNumber 被占用:" -ForegroundColor Red
        Write-Host "  进程ID: $($portInfo.ProcessId)" -ForegroundColor Yellow
        Write-Host "  进程名: $($portInfo.ProcessName)" -ForegroundColor Yellow
        Write-Host "  状态: $($portInfo.State)" -ForegroundColor Yellow
        
        if (-not $ShowOnlyFlag) {
            try {
                Write-Host "正在终止进程 $($portInfo.ProcessId)..." -ForegroundColor Yellow
                Stop-Process -Id $portInfo.ProcessId -Force -ErrorAction Stop
                Write-Host "✓ 端口 $PortNumber 已释放" -ForegroundColor Green
                
                # 等待一秒后再次检查
                Start-Sleep -Seconds 1
                $checkInfo = Get-PortProcess -PortNumber $PortNumber
                if ($checkInfo) {
                    Write-Host "⚠ 警告: 端口 $PortNumber 仍被占用，可能需要管理员权限" -ForegroundColor Yellow
                } else {
                    Write-Host "✓ 确认端口 $PortNumber 已完全释放" -ForegroundColor Green
                }
            }
            catch {
                Write-Host "✗ 无法终止进程 $($portInfo.ProcessId): $($_.Exception.Message)" -ForegroundColor Red
                Write-Host "  提示: 请尝试以管理员身份运行此脚本" -ForegroundColor Yellow
            }
        }
    }
    else {
        Write-Host "✓ 端口 $PortNumber 未被占用" -ForegroundColor Green
    }
}

# 主程序逻辑
if ($Help) {
    Show-Help
    exit 0
}

Write-Host "CodeSpirit 端口占用检测和释放工具" -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan
Write-Host ""

# Aspire 常用端口列表
$AspirePorts = @(3306, 1433, 6380, 5672, 9200, 5341, 15672, 9300)

if ($KillAll) {
    Write-Host "检查所有 Aspire 服务端口..." -ForegroundColor Yellow
    Write-Host ""
    
    foreach ($portNum in $AspirePorts) {
        Kill-PortProcess -PortNumber $portNum -ShowOnlyFlag $ShowOnly
        Write-Host ""
    }
}
elseif ($Port) {
    Write-Host "检查端口 $Port..." -ForegroundColor Yellow
    Write-Host ""
    Kill-PortProcess -PortNumber $Port -ShowOnlyFlag $ShowOnly
}
else {
    Write-Host "请指定端口号或使用 -KillAll 参数" -ForegroundColor Red
    Write-Host "使用 -Help 参数查看详细用法" -ForegroundColor Yellow
    Write-Host ""
    
    # 显示当前占用的 Aspire 端口
    Write-Host "当前 Aspire 服务端口占用情况:" -ForegroundColor Cyan
    Write-Host ""
    
    $hasOccupiedPorts = $false
    foreach ($portNum in $AspirePorts) {
        $portInfo = Get-PortProcess -PortNumber $portNum
        if ($portInfo) {
            $hasOccupiedPorts = $true
            Write-Host "端口 $portNum : $($portInfo.ProcessName) (PID: $($portInfo.ProcessId))" -ForegroundColor Red
        }
    }
    
    if (-not $hasOccupiedPorts) {
        Write-Host "✓ 所有 Aspire 服务端口均未被占用" -ForegroundColor Green
    }
    
    Write-Host ""
    Write-Host "提示: 使用 '.\kill-port-process.ps1 -Help' 查看详细用法" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "操作完成!" -ForegroundColor Green
