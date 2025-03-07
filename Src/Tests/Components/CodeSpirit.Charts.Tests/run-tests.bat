@echo off
REM 批处理脚本：自动运行单元测试并生成报告
REM 使用方法：run-tests.bat [/nobuild] [/noreport]

setlocal EnableDelayedExpansion

set NoBuild=0
set NoReport=0

REM 解析命令行参数
:ParseParams
if "%~1"=="/nobuild" set NoBuild=1 & shift & goto ParseParams
if "%~1"=="/noreport" set NoReport=1 & shift & goto ParseParams

set ReportDir=.\TestResults
set ProjectPath=.\Src\Tests\Components\CodeSpirit.Charts.Tests

REM 显示标题
echo =====================================================
echo    CodeSpirit.Charts 组件单元测试自动化执行脚本
echo =====================================================
echo.

REM 确保报告目录存在
if not exist %ReportDir% (
    mkdir %ReportDir%
    echo 创建测试报告目录: %ReportDir%
)

REM 执行构建步骤
if %NoBuild%==0 (
    echo 正在构建项目...
    dotnet build %ProjectPath% -c Release
    
    if errorlevel 1 (
        echo 构建失败，错误代码: !errorlevel!
        exit /b !errorlevel!
    )
    
    echo 项目构建成功!
    echo.
)

REM 运行测试
echo 正在运行单元测试...
if %NoReport%==1 (
    REM 简单模式，不生成报告
    dotnet test %ProjectPath% --no-build -c Release
) else (
    REM 生成详细覆盖率报告
    dotnet test %ProjectPath% ^
        --no-build ^
        -c Release ^
        --logger "trx;LogFileName=TestResults.trx" ^
        /p:CollectCoverage=true ^
        /p:CoverletOutputFormat=cobertura ^
        /p:CoverletOutput="%ReportDir%/coverage.cobertura.xml"
        
    if errorlevel 1 (
        echo 测试失败，错误代码: !errorlevel!
    ) else (
        echo 所有测试通过!
        
        REM 生成HTML报告
        echo 正在生成HTML覆盖率报告...
        
        REM 检查是否安装了reportgenerator
        where reportgenerator >nul 2>&1
        if errorlevel 1 (
            echo 未找到reportgenerator工具，正在安装...
            dotnet tool install -g dotnet-reportgenerator-globaltool
            if errorlevel 1 (
                echo 安装reportgenerator失败
                exit /b !errorlevel!
            )
        )
        
        reportgenerator ^
            "-reports:%ReportDir%/coverage.cobertura.xml" ^
            "-targetdir:%ReportDir%/html" ^
            "-reporttypes:Html"
            
        echo HTML覆盖率报告已生成: %ReportDir%/html/index.html
    )
)

echo.
echo =====================================================
echo    测试执行完成
echo =====================================================

endlocal 