#!/usr/bin/env pwsh
<#
.SYNOPSIS
    一键更新 Amis SDK 到最新版本

.DESCRIPTION
    此脚本会自动下载最新版本的 Amis SDK 文件并更新到 wwwroot/sdk 目录。
    支持从官方 CDN 或 GitHub 仓库下载最新版本。

.PARAMETER Version
    指定要下载的 Amis 版本，如果不指定则下载最新版本

.PARAMETER Source
    指定下载源：'cdn' 或 'github'，默认为 'cdn'

.EXAMPLE
    .\UpdateAmisSDK.ps1
    下载最新版本的 Amis SDK

.EXAMPLE
    .\UpdateAmisSDK.ps1 -Version "6.12.0" -Source "cdn"
    下载指定版本的 Amis SDK

.NOTES
    作者: CodeSpirit Team
    版本: 1.2.0
    创建日期: 2025-01-27
    更新日期: 2025-05-27
#>

param(
    [string]$Version = "latest",
    [ValidateSet("cdn", "github")]
    [string]$Source = "cdn"
)

# 设置错误处理
$ErrorActionPreference = "Stop"

# 获取脚本所在目录和项目根目录
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$WebProjectRoot = Split-Path -Parent $ScriptDir
$SdkBaseDir = Join-Path $WebProjectRoot "wwwroot\sdk"

# SDK目录将在获取版本后动态设置
$SdkDir = ""

# 颜色输出函数
function Write-ColorOutput {
    param(
        [string]$Message,
        [string]$Color = "White"
    )
    Write-Host $Message -ForegroundColor $Color
}

# 获取最新版本号
function Get-LatestAmisVersion {
    try {
        Write-ColorOutput "正在获取 Amis 最新版本信息..." "Yellow"
        
        # 从 GitHub API 获取最新版本
        $apiUrl = "https://api.github.com/repos/baidu/amis/releases/latest"
        $response = Invoke-RestMethod -Uri $apiUrl -Headers @{
            "User-Agent" = "CodeSpirit-AmisUpdater/1.0"
        }
        
        $latestVersion = $response.tag_name -replace '^v', ''
        Write-ColorOutput "发现最新版本: $latestVersion" "Green"
        return $latestVersion
    }
    catch {
        Write-ColorOutput "无法获取最新版本信息，使用默认版本 6.12.0" "Yellow"
        return "6.12.0"
    }
}

# 下载文件函数
function Download-File {
    param(
        [string]$Url,
        [string]$OutputPath,
        [string]$FileName
    )
    
    try {
        Write-ColorOutput "正在下载: $FileName" "Cyan"
        
        # 确保目录存在
        $dir = Split-Path -Parent $OutputPath
        if (!(Test-Path $dir)) {
            New-Item -ItemType Directory -Path $dir -Force | Out-Null
        }
        
        # 下载文件
        Invoke-WebRequest -Uri $Url -OutFile $OutputPath -UseBasicParsing
        
        if (Test-Path $OutputPath) {
            $fileSize = (Get-Item $OutputPath).Length
            Write-ColorOutput "✓ 下载完成: $FileName ($([math]::Round($fileSize/1KB, 2)) KB)" "Green"
            return $true
        }
        else {
            Write-ColorOutput "✗ 下载失败: $FileName" "Red"
            return $false
        }
    }
    catch {
        Write-ColorOutput "✗ 下载失败: $FileName - $($_.Exception.Message)" "Red"
        return $false
    }
}

# 创建版本目录
function Create-VersionDirectory {
    param([string]$Version)
    
    # 设置版本特定的SDK目录
    $script:SdkDir = Join-Path $SdkBaseDir $Version
    
    Write-ColorOutput "正在创建版本目录: $SdkDir" "Yellow"
    
    try {
        # 如果版本目录已存在，先清理
        if (Test-Path $SdkDir) {
            Remove-Item -Path "$SdkDir\*" -Recurse -Force
            Write-ColorOutput "✓ 清理现有版本目录" "Green"
        }
        else {
            # 创建版本目录
            New-Item -ItemType Directory -Path $SdkDir -Force | Out-Null
            Write-ColorOutput "✓ 创建版本目录完成" "Green"
        }
        
        # 确保基础SDK目录存在
        if (!(Test-Path $SdkBaseDir)) {
            New-Item -ItemType Directory -Path $SdkBaseDir -Force | Out-Null
        }
        
        return $true
    }
    catch {
        Write-ColorOutput "✗ 创建版本目录失败: $($_.Exception.Message)" "Red"
        throw
    }
}

# 递归下载目录
function Download-Directory {
    param(
        [string]$BaseUrl,
        [string]$RelativePath,
        [string]$LocalPath,
        [string]$Version
    )
    
    try {
        Write-ColorOutput "正在下载目录: $RelativePath" "Cyan"
        
        $downloadCount = 0
        
        # 根据目录路径使用预定义的文件结构
        switch ($RelativePath) {
            "thirds" {
                # thirds 目录的子目录
                $thirdsSubDirs = @(
                    "@fortawesome",
                    "hls.js", 
                    "markdown-it",
                    "moment-timezone",
                    "monaco-editor",
                    "mpegts.js",
                    "pdfjs-dist"
                )
                
                foreach ($subDir in $thirdsSubDirs) {
                    $localSubDir = Join-Path $LocalPath $subDir
                    New-Item -ItemType Directory -Path $localSubDir -Force | Out-Null
                    Write-ColorOutput "正在下载子目录: $subDir" "Cyan"
                    
                    $subPath = "$RelativePath/$subDir"
                    $subCount = Download-Directory -BaseUrl $BaseUrl -RelativePath $subPath -LocalPath $localSubDir -Version $Version
                    $downloadCount += $subCount
                }
            }
            
            "locale" {
                # locale 目录的文件
                $localeFiles = @("de-DE.js")
                
                foreach ($file in $localeFiles) {
                    $fileUrl = "$BaseUrl/$RelativePath/$file"
                    $localFilePath = Join-Path $LocalPath $file
                    
                    Write-ColorOutput "  下载文件: $file" "Gray"
                    if (Download-File -Url $fileUrl -OutputPath $localFilePath -FileName $file) {
                        $downloadCount++
                    }
                }
            }
            
            "thirds/hls.js" {
                $files = @("hls.js", "hls.min.js")
                foreach ($file in $files) {
                    $fileUrl = "$BaseUrl/$RelativePath/$file"
                    $localFilePath = Join-Path $LocalPath $file
                    
                    Write-ColorOutput "  下载文件: $file" "Gray"
                    if (Download-File -Url $fileUrl -OutputPath $localFilePath -FileName $file) {
                        $downloadCount++
                    }
                }
            }
            
            "thirds/markdown-it" {
                $files = @("markdown-it.js", "markdown-it.min.js")
                foreach ($file in $files) {
                    $fileUrl = "$BaseUrl/$RelativePath/$file"
                    $localFilePath = Join-Path $LocalPath $file
                    
                    Write-ColorOutput "  下载文件: $file" "Gray"
                    if (Download-File -Url $fileUrl -OutputPath $localFilePath -FileName $file) {
                        $downloadCount++
                    }
                }
            }
            
            "thirds/moment-timezone" {
                $files = @("moment-timezone-with-data.js", "moment-timezone-with-data.min.js")
                foreach ($file in $files) {
                    $fileUrl = "$BaseUrl/$RelativePath/$file"
                    $localFilePath = Join-Path $LocalPath $file
                    
                    Write-ColorOutput "  下载文件: $file" "Gray"
                    if (Download-File -Url $fileUrl -OutputPath $localFilePath -FileName $file) {
                        $downloadCount++
                    }
                }
            }
            
            "thirds/mpegts.js" {
                $files = @("mpegts.js", "mpegts.min.js")
                foreach ($file in $files) {
                    $fileUrl = "$BaseUrl/$RelativePath/$file"
                    $localFilePath = Join-Path $LocalPath $file
                    
                    Write-ColorOutput "  下载文件: $file" "Gray"
                    if (Download-File -Url $fileUrl -OutputPath $localFilePath -FileName $file) {
                        $downloadCount++
                    }
                }
            }
            
            "thirds/@fortawesome" {
                # @fortawesome 包含 fontawesome-free 子目录
                $subDir = "fontawesome-free"
                $localSubDir = Join-Path $LocalPath $subDir
                New-Item -ItemType Directory -Path $localSubDir -Force | Out-Null
                
                $subPath = "$RelativePath/$subDir"
                $subCount = Download-Directory -BaseUrl $BaseUrl -RelativePath $subPath -LocalPath $localSubDir -Version $Version
                $downloadCount += $subCount
            }
            
            "thirds/@fortawesome/fontawesome-free" {
                # fontawesome-free 包含多个子目录
                $subDirs = @("css", "js", "webfonts")
                foreach ($subDir in $subDirs) {
                    $localSubDir = Join-Path $LocalPath $subDir
                    New-Item -ItemType Directory -Path $localSubDir -Force | Out-Null
                    
                    $subPath = "$RelativePath/$subDir"
                    $subCount = Download-Directory -BaseUrl $BaseUrl -RelativePath $subPath -LocalPath $localSubDir -Version $Version
                    $downloadCount += $subCount
                }
            }
            
            "thirds/@fortawesome/fontawesome-free/css" {
                $files = @("all.css", "all.min.css")
                foreach ($file in $files) {
                    $fileUrl = "$BaseUrl/$RelativePath/$file"
                    $localFilePath = Join-Path $LocalPath $file
                    
                    Write-ColorOutput "  下载文件: $file" "Gray"
                    if (Download-File -Url $fileUrl -OutputPath $localFilePath -FileName $file) {
                        $downloadCount++
                    }
                }
            }
            
            "thirds/@fortawesome/fontawesome-free/js" {
                $files = @("all.js", "all.min.js")
                foreach ($file in $files) {
                    $fileUrl = "$BaseUrl/$RelativePath/$file"
                    $localFilePath = Join-Path $LocalPath $file
                    
                    Write-ColorOutput "  下载文件: $file" "Gray"
                    if (Download-File -Url $fileUrl -OutputPath $localFilePath -FileName $file) {
                        $downloadCount++
                    }
                }
            }
            
            "thirds/@fortawesome/fontawesome-free/webfonts" {
                # webfonts 目录包含字体文件，下载主要的几个
                $files = @("fa-solid-900.woff2", "fa-regular-400.woff2", "fa-brands-400.woff2")
                foreach ($file in $files) {
                    $fileUrl = "$BaseUrl/$RelativePath/$file"
                    $localFilePath = Join-Path $LocalPath $file
                    
                    Write-ColorOutput "  下载文件: $file" "Gray"
                    if (Download-File -Url $fileUrl -OutputPath $localFilePath -FileName $file) {
                        $downloadCount++
                    }
                }
            }
            
            "thirds/monaco-editor" {
                # monaco-editor 包含 min 子目录
                $subDir = "min"
                $localSubDir = Join-Path $LocalPath $subDir
                New-Item -ItemType Directory -Path $localSubDir -Force | Out-Null
                
                $subPath = "$RelativePath/$subDir"
                $subCount = Download-Directory -BaseUrl $BaseUrl -RelativePath $subPath -LocalPath $localSubDir -Version $Version
                $downloadCount += $subCount
            }
            
            "thirds/monaco-editor/min" {
                # monaco-editor/min 包含核心文件
                $files = @("vs/loader.js", "vs/editor/editor.main.js", "vs/editor/editor.main.css")
                
                # 创建 vs 和 vs/editor 目录
                $vsDir = Join-Path $LocalPath "vs"
                $editorDir = Join-Path $vsDir "editor"
                New-Item -ItemType Directory -Path $vsDir -Force | Out-Null
                New-Item -ItemType Directory -Path $editorDir -Force | Out-Null
                
                foreach ($file in $files) {
                    $fileUrl = "$BaseUrl/$RelativePath/$file"
                    $localFilePath = Join-Path $LocalPath $file
                    
                    Write-ColorOutput "  下载文件: $file" "Gray"
                    if (Download-File -Url $fileUrl -OutputPath $localFilePath -FileName $file) {
                        $downloadCount++
                    }
                }
            }
            
            "thirds/pdfjs-dist" {
                # pdfjs-dist 包含 build 子目录
                $subDir = "build"
                $localSubDir = Join-Path $LocalPath $subDir
                New-Item -ItemType Directory -Path $localSubDir -Force | Out-Null
                
                $subPath = "$RelativePath/$subDir"
                $subCount = Download-Directory -BaseUrl $BaseUrl -RelativePath $subPath -LocalPath $localSubDir -Version $Version
                $downloadCount += $subCount
            }
            
            "thirds/pdfjs-dist/build" {
                $files = @("pdf.js", "pdf.min.js", "pdf.worker.js", "pdf.worker.min.js")
                foreach ($file in $files) {
                    $fileUrl = "$BaseUrl/$RelativePath/$file"
                    $localFilePath = Join-Path $LocalPath $file
                    
                    Write-ColorOutput "  下载文件: $file" "Gray"
                    if (Download-File -Url $fileUrl -OutputPath $localFilePath -FileName $file) {
                        $downloadCount++
                    }
                }
            }
            
            default {
                Write-ColorOutput "跳过未知目录: $RelativePath" "Yellow"
            }
        }
        
        Write-ColorOutput "目录 $RelativePath 下载完成: $downloadCount 个文件" "Green"
        return $downloadCount
    }
    catch {
        Write-ColorOutput "下载目录失败: $RelativePath - $($_.Exception.Message)" "Red"
        return 0
    }
}

# 下载并解压 npm 包
function Download-FromNpmPackage {
    param([string]$Version)
    
    Write-ColorOutput "正在下载 Amis npm 包 v$Version..." "Yellow"
    
    # npm 包下载 URL
    $packageUrl = "https://registry.npmjs.org/amis/-/amis-$Version.tgz"
    $tempDir = Join-Path $env:TEMP "amis-download-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
    $packageFile = Join-Path $tempDir "amis-$Version.tgz"
    
    try {
        # 创建临时目录
        New-Item -ItemType Directory -Path $tempDir -Force | Out-Null
        Write-ColorOutput "临时目录: $tempDir" "Gray"
        
        # 下载 npm 包
        Write-ColorOutput "正在下载 npm 包..." "Cyan"
        Invoke-WebRequest -Uri $packageUrl -OutFile $packageFile -UseBasicParsing
        
        $fileSize = (Get-Item $packageFile).Length
        Write-ColorOutput "✓ 包下载完成: $([math]::Round($fileSize/1MB, 2)) MB" "Green"
        
        # 解压 tar.gz 文件
        Write-ColorOutput "正在解压包..." "Cyan"
        
        # 使用 PowerShell 5.0+ 的 Expand-Archive 或 tar 命令
        $extractDir = Join-Path $tempDir "extracted"
        New-Item -ItemType Directory -Path $extractDir -Force | Out-Null
        
        # 尝试使用 tar 命令（Windows 10 1903+ 内置）
        try {
            $tarResult = & tar -xzf $packageFile -C $extractDir 2>&1
            if ($LASTEXITCODE -ne 0) {
                throw "tar 命令失败: $tarResult"
            }
            Write-ColorOutput "✓ 使用 tar 命令解压成功" "Green"
        }
        catch {
            Write-ColorOutput "tar 命令不可用，尝试使用 7-Zip..." "Yellow"
            
            # 尝试使用 7-Zip
            $sevenZipPaths = @(
                "${env:ProgramFiles}\7-Zip\7z.exe",
                "${env:ProgramFiles(x86)}\7-Zip\7z.exe",
                "7z.exe"
            )
            
            $sevenZipFound = $false
            foreach ($path in $sevenZipPaths) {
                if (Get-Command $path -ErrorAction SilentlyContinue) {
                    & $path x $packageFile "-o$extractDir" -y | Out-Null
                    if ($LASTEXITCODE -eq 0) {
                        Write-ColorOutput "✓ 使用 7-Zip 解压成功" "Green"
                        $sevenZipFound = $true
                        break
                    }
                }
            }
            
            if (!$sevenZipFound) {
                throw "无法找到解压工具，请安装 7-Zip 或确保系统支持 tar 命令"
            }
        }
        
        # 查找 SDK 目录
        $packageDir = Join-Path $extractDir "package"
        $sdkSourceDir = Join-Path $packageDir "sdk"
        
        if (!(Test-Path $sdkSourceDir)) {
            throw "在解压的包中找不到 sdk 目录"
        }
        
        Write-ColorOutput "✓ 找到 SDK 目录: $sdkSourceDir" "Green"
        
        # 复制 SDK 文件到目标目录
        Write-ColorOutput "正在复制 SDK 文件..." "Cyan"
        Copy-Item -Path "$sdkSourceDir\*" -Destination $SdkDir -Recurse -Force
        
        # 统计复制的文件数量
        $copiedFiles = Get-ChildItem -Path $SdkDir -Recurse -File
        $fileCount = $copiedFiles.Count
        
        Write-ColorOutput "✓ 复制完成: $fileCount 个文件" "Green"
        
        return $fileCount -gt 0
    }
    catch {
        Write-ColorOutput "✗ npm 包下载失败: $($_.Exception.Message)" "Red"
        return $false
    }
    finally {
        # 清理临时文件
        if (Test-Path $tempDir) {
            try {
                Remove-Item -Path $tempDir -Recurse -Force
                Write-ColorOutput "✓ 清理临时文件完成" "Gray"
            }
            catch {
                Write-ColorOutput "清理临时文件失败: $($_.Exception.Message)" "Yellow"
            }
        }
    }
}

# 从 CDN 下载 Amis SDK（备用方法）
function Download-FromCDN {
    param([string]$Version)
    
    Write-ColorOutput "正在从 CDN 下载 Amis SDK v$Version..." "Yellow"
    
    # CDN 基础 URL
    $cdnBase = "https://unpkg.com/amis@$Version/sdk"
    
    # 定义需要下载的文件列表
    $files = @(
        @{ Name = "sdk.js"; Path = "sdk.js" },
        @{ Name = "sdk.css"; Path = "sdk.css" },
        @{ Name = "sdk-ie11.css"; Path = "sdk-ie11.css" },
        @{ Name = "cxd.css"; Path = "cxd.css" },
        @{ Name = "cxd-ie11.css"; Path = "cxd-ie11.css" },
        @{ Name = "dark.css"; Path = "dark.css" },
        @{ Name = "dark-ie11.css"; Path = "dark-ie11.css" },
        @{ Name = "antd.css"; Path = "antd.css" },
        @{ Name = "antd-ie11.css"; Path = "antd-ie11.css" },
        @{ Name = "ang.css"; Path = "ang.css" },
        @{ Name = "ang-ie11.css"; Path = "ang-ie11.css" },
        @{ Name = "helper.css"; Path = "helper.css" },
        @{ Name = "iconfont.css"; Path = "iconfont.css" },
        @{ Name = "iconfont.eot"; Path = "iconfont.eot" },
        @{ Name = "iconfont.svg"; Path = "iconfont.svg" },
        @{ Name = "iconfont.ttf"; Path = "iconfont.ttf" },
        @{ Name = "iconfont.woff"; Path = "iconfont.woff" },
        @{ Name = "ie11-patch.css"; Path = "ie11-patch.css" }
    )
    
    # 可选的扩展文件
    $optionalFiles = @(
        @{ Name = "charts.js"; Path = "charts.js" },
        @{ Name = "rich-text.js"; Path = "rich-text.js" },
        @{ Name = "rest.js"; Path = "rest.js" },
        @{ Name = "office-viewer.js"; Path = "office-viewer.js" },
        @{ Name = "pdf-viewer.js"; Path = "pdf-viewer.js" },
        @{ Name = "json-view.js"; Path = "json-view.js" },
        @{ Name = "markdown.js"; Path = "markdown.js" },
        @{ Name = "codemirror.js"; Path = "codemirror.js" },
        @{ Name = "color-picker.js"; Path = "color-picker.js" },
        @{ Name = "cropperjs.js"; Path = "cropperjs.js" },
        @{ Name = "barcode.js"; Path = "barcode.js" },
        @{ Name = "exceljs.js"; Path = "exceljs.js" },
        @{ Name = "fomula-doc.js"; Path = "fomula-doc.js" },
        @{ Name = "papaparse.js"; Path = "papaparse.js" },
        @{ Name = "tinymce.js"; Path = "tinymce.js" },
        @{ Name = "xlsx.js"; Path = "xlsx.js" }
    )
    
    $successCount = 0
    $totalFiles = $files.Count + $optionalFiles.Count
    
    # 下载核心文件
    foreach ($file in $files) {
        $url = "$cdnBase/$($file.Path)"
        $outputPath = Join-Path $SdkDir $file.Name
        
        if (Download-File -Url $url -OutputPath $outputPath -FileName $file.Name) {
            $successCount++
        }
    }
    
    # 下载可选文件（失败不影响整体流程）
    foreach ($file in $optionalFiles) {
        $url = "$cdnBase/$($file.Path)"
        $outputPath = Join-Path $SdkDir $file.Name
        
        if (Download-File -Url $url -OutputPath $outputPath -FileName $file.Name) {
            $successCount++
        }
    }
    
    # 尝试下载 thirds 目录（如果存在）
    try {
        Write-ColorOutput "正在检查 thirds 目录..." "Yellow"
        # 使用 unpkg browse API 来检查目录
        $thirdsApiUrl = "https://unpkg.com/browse/amis@$Version/sdk/thirds/"
        $thirdsResponse = Invoke-WebRequest -Uri $thirdsApiUrl -UseBasicParsing
        
        if ($thirdsResponse.StatusCode -eq 200) {
            Write-ColorOutput "正在下载 thirds 目录..." "Yellow"
            $thirdsPath = Join-Path $SdkDir "thirds"
            New-Item -ItemType Directory -Path $thirdsPath -Force | Out-Null
            $thirdsCount = Download-Directory -BaseUrl $cdnBase -RelativePath "thirds" -LocalPath $thirdsPath -Version $Version
            $successCount += $thirdsCount
            Write-ColorOutput "✓ thirds 目录下载完成: $thirdsCount 个文件" "Green"
        }
    }
    catch {
        Write-ColorOutput "thirds 目录不存在或无法访问，跳过下载" "Yellow"
        # 删除空的 thirds 目录
        $thirdsPath = Join-Path $SdkDir "thirds"
        if (Test-Path $thirdsPath) {
            Remove-Item -Path $thirdsPath -Force -ErrorAction SilentlyContinue
        }
    }
    
    # 尝试下载 locale 目录（如果存在）
    try {
        Write-ColorOutput "正在检查 locale 目录..." "Yellow"
        # 使用 unpkg browse API 来检查目录
        $localeApiUrl = "https://unpkg.com/browse/amis@$Version/sdk/locale/"
        $localeResponse = Invoke-WebRequest -Uri $localeApiUrl -UseBasicParsing
        
        if ($localeResponse.StatusCode -eq 200) {
            Write-ColorOutput "正在下载 locale 目录..." "Yellow"
            $localePath = Join-Path $SdkDir "locale"
            New-Item -ItemType Directory -Path $localePath -Force | Out-Null
            $localeCount = Download-Directory -BaseUrl $cdnBase -RelativePath "locale" -LocalPath $localePath -Version $Version
            $successCount += $localeCount
            Write-ColorOutput "✓ locale 目录下载完成: $localeCount 个文件" "Green"
        }
    }
    catch {
        Write-ColorOutput "locale 目录不存在或无法访问，跳过下载" "Yellow"
        # 删除空的 locale 目录
        $localePath = Join-Path $SdkDir "locale"
        if (Test-Path $localePath) {
            Remove-Item -Path $localePath -Force -ErrorAction SilentlyContinue
        }
    }
    
    Write-ColorOutput "下载完成: $successCount 个文件" "Green"
    return $successCount -gt 0
}

# 验证下载的文件
function Validate-DownloadedFiles {
    Write-ColorOutput "正在验证下载的文件..." "Yellow"
    
    $requiredFiles = @("sdk.js", "sdk.css", "cxd.css")
    $missingFiles = @()
    
    foreach ($file in $requiredFiles) {
        $filePath = Join-Path $SdkDir $file
        if (!(Test-Path $filePath)) {
            $missingFiles += $file
        }
        else {
            $fileSize = (Get-Item $filePath).Length
            if ($fileSize -lt 1KB) {
                $missingFiles += "$file (文件过小)"
            }
        }
    }
    
    if ($missingFiles.Count -eq 0) {
        Write-ColorOutput "✓ 文件验证通过" "Green"
        return $true
    }
    else {
        Write-ColorOutput "✗ 文件验证失败，缺少文件: $($missingFiles -join ', ')" "Red"
        return $false
    }
}

# 更新 libman.json 配置
function Update-LibmanConfig {
    $libmanPath = Join-Path $WebProjectRoot "libman.json"
    
    if (Test-Path $libmanPath) {
        try {
            Write-ColorOutput "正在更新 libman.json 配置..." "Yellow"
            
            $libmanContent = Get-Content $libmanPath -Raw | ConvertFrom-Json
            
            # 检查是否已存在 amis 配置
            $amisLibrary = $libmanContent.libraries | Where-Object { $_.library -like "*amis*" }
            
            if (!$amisLibrary) {
                # 添加 amis 配置
                $newLibrary = @{
                    library = "amis@$Version"
                    destination = "wwwroot/sdk/"
                    files = @(
                        "sdk/sdk.js",
                        "sdk/sdk.css",
                        "sdk/cxd.css",
                        "sdk/dark.css",
                        "sdk/antd.css",
                        "sdk/ang.css"
                    )
                }
                
                $libmanContent.libraries += $newLibrary
                
                # 保存更新后的配置
                $libmanContent | ConvertTo-Json -Depth 10 | Set-Content $libmanPath -Encoding UTF8
                
                Write-ColorOutput "✓ libman.json 配置已更新" "Green"
            }
            else {
                Write-ColorOutput "libman.json 中已存在 amis 配置，跳过更新" "Yellow"
            }
        }
        catch {
            Write-ColorOutput "更新 libman.json 失败: $($_.Exception.Message)" "Yellow"
        }
    }
}

# 生成版本信息文件
function Generate-VersionInfo {
    param([string]$Version)
    
    $versionInfoPath = Join-Path $SdkDir "version.json"
    $versionInfo = @{
        version = $Version
        updateTime = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")
        source = $Source
        updatedBy = "UpdateAmisSDK.ps1"
    }
    
    try {
        $versionInfo | ConvertTo-Json -Depth 2 | Set-Content $versionInfoPath -Encoding UTF8
        Write-ColorOutput "✓ 版本信息已保存: $versionInfoPath" "Green"
    }
    catch {
        Write-ColorOutput "保存版本信息失败: $($_.Exception.Message)" "Yellow"
    }
}

# 创建当前版本链接
function Create-CurrentVersionLink {
    param([string]$Version)
    
    $currentLinkPath = Join-Path $SdkBaseDir "current"
    
    try {
        # 删除现有的链接或目录
        if (Test-Path $currentLinkPath) {
            Remove-Item -Path $currentLinkPath -Recurse -Force
        }
        
        # 在Windows上创建目录链接（需要管理员权限）或复制文件
        try {
            # 尝试创建符号链接
            New-Item -ItemType SymbolicLink -Path $currentLinkPath -Target $SdkDir -Force | Out-Null
            Write-ColorOutput "✓ 创建符号链接: current -> $Version" "Green"
        }
        catch {
            # 如果符号链接失败，则复制文件
            Copy-Item -Path $SdkDir -Destination $currentLinkPath -Recurse -Force
            Write-ColorOutput "✓ 复制文件到 current 目录（符号链接需要管理员权限）" "Yellow"
        }
    }
    catch {
        Write-ColorOutput "创建当前版本链接失败: $($_.Exception.Message)" "Yellow"
    }
}

# 主函数
function Main {
    Write-ColorOutput "=== CodeSpirit Amis SDK 更新工具 ===" "Magenta"
    Write-ColorOutput "Web项目路径: $WebProjectRoot" "Gray"
    Write-ColorOutput "SDK 基础目录: $SdkBaseDir" "Gray"
    Write-ColorOutput ""
    
    try {
        # 获取版本信息
        if ($Version -eq "latest") {
            $Version = Get-LatestAmisVersion
        }
        
        Write-ColorOutput "目标版本: $Version" "Cyan"
        Write-ColorOutput "下载源: $Source" "Cyan"
        
        # 创建版本目录
        Create-VersionDirectory -Version $Version
        
        Write-ColorOutput "版本目录: $SdkDir" "Gray"
        Write-ColorOutput ""
        
        # 下载新文件
        $downloadSuccess = $false
        
        if ($Source -eq "cdn") {
            # 优先尝试 npm 包下载
            Write-ColorOutput "尝试从 npm 包下载..." "Cyan"
            $downloadSuccess = Download-FromNpmPackage -Version $Version
            
            if (!$downloadSuccess) {
                Write-ColorOutput "npm 包下载失败，切换到 CDN 逐个文件下载..." "Yellow"
                $downloadSuccess = Download-FromCDN -Version $Version
            }
        }
        else {
            Write-ColorOutput "GitHub 下载源暂未实现，使用 npm 包下载" "Yellow"
            $downloadSuccess = Download-FromNpmPackage -Version $Version
            
            if (!$downloadSuccess) {
                Write-ColorOutput "npm 包下载失败，切换到 CDN 逐个文件下载..." "Yellow"
                $downloadSuccess = Download-FromCDN -Version $Version
            }
        }
        
        if (!$downloadSuccess) {
            throw "下载失败"
        }
        
        # 验证文件
        if (!(Validate-DownloadedFiles)) {
            throw "文件验证失败"
        }
        
        # 更新配置文件
        Update-LibmanConfig
        
        # 生成版本信息
        Generate-VersionInfo -Version $Version
        
        # 创建当前版本链接
        Create-CurrentVersionLink -Version $Version
        
        Write-ColorOutput ""
        Write-ColorOutput "=== 更新完成 ===" "Green"
        Write-ColorOutput "Amis SDK 已成功更新到版本 $Version" "Green"
        Write-ColorOutput "文件保存位置: $SdkDir" "Gray"
        
        Write-ColorOutput ""
        Write-ColorOutput "请重新启动应用程序以使用新版本的 SDK" "Yellow"
        
    }
    catch {
        Write-ColorOutput ""
        Write-ColorOutput "=== 更新失败 ===" "Red"
        Write-ColorOutput "错误信息: $($_.Exception.Message)" "Red"
        Write-ColorOutput "提示: 由于使用版本目录管理，旧版本文件仍然保留在其他版本目录中" "Yellow"
        
        exit 1
    }
}

# 执行主函数
Main 