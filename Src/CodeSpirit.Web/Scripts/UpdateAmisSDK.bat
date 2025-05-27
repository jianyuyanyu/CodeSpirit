@echo off
chcp 65001 >nul
setlocal enabledelayedexpansion

:: CodeSpirit Amis SDK 更新工具
:: 作者: CodeSpirit Team
:: 版本: 1.0.0

echo.
echo ===============================================
echo    CodeSpirit Amis SDK 一键更新工具
echo ===============================================
echo.

:: 检查 PowerShell 是否可用
where pwsh >nul 2>&1
if %errorlevel% neq 0 (
    where powershell >nul 2>&1
    if %errorlevel% neq 0 (
        echo [错误] 未找到 PowerShell，请确保已安装 PowerShell
        pause
        exit /b 1
    ) else (
        set PS_CMD=powershell
    )
) else (
    set PS_CMD=pwsh
)

:: 获取脚本所在目录
set SCRIPT_DIR=%~dp0
set PS_SCRIPT=%SCRIPT_DIR%UpdateAmisSDK.ps1

:: 检查 PowerShell 脚本是否存在
if not exist "%PS_SCRIPT%" (
    echo [错误] 未找到 PowerShell 脚本: %PS_SCRIPT%
    pause
    exit /b 1
)

:: 显示菜单
:menu
echo 请选择操作：
echo.
echo 1. 更新到最新版本 (推荐)
echo 2. 更新到指定版本
echo 3. 查看当前版本信息
echo 4. 退出
echo.
set /p choice="请输入选项 (1-4): "

if "%choice%"=="1" goto update_latest
if "%choice%"=="2" goto update_version
if "%choice%"=="3" goto show_version
if "%choice%"=="4" goto exit
echo [错误] 无效选项，请重新选择
echo.
goto menu

:update_latest
echo.
echo 正在更新到最新版本...
echo.
%PS_CMD% -ExecutionPolicy Bypass -File "%PS_SCRIPT%"
goto end

:update_version
echo.
set /p version="请输入要更新的版本号 (例如: 3.6.0): "
if "%version%"=="" (
    echo [错误] 版本号不能为空
    echo.
    goto menu
)
echo.
echo 正在更新到版本 %version%...
echo.
%PS_CMD% -ExecutionPolicy Bypass -File "%PS_SCRIPT%" -Version "%version%"
goto end

:show_version
echo.
echo 正在查看当前版本信息...
echo.

:: 查找版本信息文件
set VERSION_FILE=%SCRIPT_DIR%..\wwwroot\sdk\version.json
if exist "%VERSION_FILE%" (
    echo 当前版本信息：
    type "%VERSION_FILE%"
) else (
    echo 未找到版本信息文件，可能尚未使用此工具更新过 SDK
)

echo.
echo 当前 SDK 目录文件列表：
set SDK_DIR=%SCRIPT_DIR%..\wwwroot\sdk
if exist "%SDK_DIR%" (
    dir "%SDK_DIR%" /b
) else (
    echo SDK 目录不存在
)

echo.
pause
goto menu

:end
echo.
if %errorlevel% equ 0 (
    echo [成功] SDK 更新完成！
) else (
    echo [失败] SDK 更新失败，错误代码: %errorlevel%
)
echo.

:exit
echo 感谢使用 CodeSpirit Amis SDK 更新工具！
pause 