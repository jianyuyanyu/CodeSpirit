# 快速修复 MySQL 端口占用问题
# 专门用于解决 CodeSpirit Aspire 应用中的 MySQL 端口冲突

Write-Host "CodeSpirit MySQL 端口修复工具" -ForegroundColor Cyan
Write-Host "=============================" -ForegroundColor Cyan

$MySqlPort = 3306

# 检查端口占用
try {
    $connections = Get-NetTCPConnection -LocalPort $MySqlPort -ErrorAction SilentlyContinue
    
    if ($connections) {
        Write-Host "发现端口 $MySqlPort 被占用，正在释放..." -ForegroundColor Yellow
        
        foreach ($conn in $connections) {
            $process = Get-Process -Id $conn.OwningProcess -ErrorAction SilentlyContinue
            if ($process) {
                Write-Host "终止进程: $($process.ProcessName) (PID: $($process.Id))" -ForegroundColor Yellow
                try {
                    Stop-Process -Id $process.Id -Force
                    Write-Host "✓ 进程已终止" -ForegroundColor Green
                }
                catch {
                    Write-Host "✗ 无法终止进程，请以管理员身份运行" -ForegroundColor Red
                }
            }
        }
        
        # 等待并再次检查
        Start-Sleep -Seconds 2
        $checkConnections = Get-NetTCPConnection -LocalPort $MySqlPort -ErrorAction SilentlyContinue
        if (-not $checkConnections) {
            Write-Host "✓ 端口 $MySqlPort 已成功释放" -ForegroundColor Green
        } else {
            Write-Host "⚠ 端口可能仍被占用，建议重启计算机" -ForegroundColor Yellow
        }
    }
    else {
        Write-Host "✓ 端口 $MySqlPort 未被占用，可以正常启动" -ForegroundColor Green
    }
}
catch {
    Write-Host "检查端口时出错: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "现在可以重新启动 CodeSpirit.AppHost 项目" -ForegroundColor Cyan
