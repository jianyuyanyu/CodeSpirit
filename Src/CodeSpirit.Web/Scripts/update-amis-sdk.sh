#!/bin/bash

# CodeSpirit Amis SDK 更新工具 (Linux/macOS 版本)
# 作者: CodeSpirit Team
# 版本: 1.0.0
# 创建日期: 2025-01-27

set -e  # 遇到错误时退出

# 颜色定义
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
MAGENTA='\033[0;35m'
CYAN='\033[0;36m'
GRAY='\033[0;37m'
NC='\033[0m' # No Color

# 默认参数
VERSION="latest"
SOURCE="cdn"

# 获取脚本所在目录
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
WEB_PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
SDK_BASE_DIR="$WEB_PROJECT_ROOT/wwwroot/sdk"

# SDK目录将在获取版本后动态设置
SDK_DIR=""

# 颜色输出函数
print_color() {
    local color=$1
    local message=$2
    echo -e "${color}${message}${NC}"
}

# 显示帮助信息
show_help() {
    echo "CodeSpirit Amis SDK 更新工具"
    echo ""
    echo "用法: $0 [选项]"
    echo ""
    echo "选项:"
    echo "  -v, --version VERSION    指定要下载的 Amis 版本 (默认: latest)"
    echo "  -s, --source SOURCE      指定下载源 cdn|github (默认: cdn)"
    echo "  -h, --help               显示此帮助信息"
    echo ""
    echo "示例:"
    echo "  $0                       # 更新到最新版本"
    echo "  $0 -v 6.12.0            # 更新到指定版本"
    echo ""
}

# 解析命令行参数
parse_args() {
    while [[ $# -gt 0 ]]; do
        case $1 in
            -v|--version)
                VERSION="$2"
                shift 2
                ;;
            -s|--source)
                SOURCE="$2"
                shift 2
                ;;
            -h|--help)
                show_help
                exit 0
                ;;
            *)
                print_color $RED "未知参数: $1"
                show_help
                exit 1
                ;;
        esac
    done
}

# 检查依赖
check_dependencies() {
    if ! command -v curl &> /dev/null; then
        print_color $RED "错误: 未找到 curl 命令，请先安装 curl"
        exit 1
    fi

    if ! command -v jq &> /dev/null; then
        print_color $YELLOW "警告: 未找到 jq 命令，将使用简单的文本处理方式获取版本信息"
    fi
}

# 获取最新版本号
get_latest_version() {
    print_color $YELLOW "正在获取 Amis 最新版本信息..."
    
    local api_url="https://api.github.com/repos/baidu/amis/releases/latest"
    
    if command -v jq &> /dev/null; then
        # 使用 jq 解析 JSON
        local response=$(curl -s -H "User-Agent: CodeSpirit-AmisUpdater/1.0" "$api_url")
        local latest_version=$(echo "$response" | jq -r '.tag_name' | sed 's/^v//')
    else
        # 使用简单的文本处理
        local response=$(curl -s -H "User-Agent: CodeSpirit-AmisUpdater/1.0" "$api_url")
        local latest_version=$(echo "$response" | grep -o '"tag_name":"[^"]*"' | cut -d'"' -f4 | sed 's/^v//')
    fi
    
    if [[ -n "$latest_version" && "$latest_version" != "null" ]]; then
        print_color $GREEN "发现最新版本: $latest_version"
        echo "$latest_version"
    else
        print_color $YELLOW "无法获取最新版本信息，使用默认版本 6.12.0"
        echo "6.12.0"
    fi
}

# 下载文件
download_file() {
    local url=$1
    local output_path=$2
    local filename=$3
    
    print_color $CYAN "正在下载: $filename"
    
    # 确保目录存在
    mkdir -p "$(dirname "$output_path")"
    
    if curl -L -s -o "$output_path" "$url"; then
        if [[ -f "$output_path" ]]; then
            local file_size=$(stat -f%z "$output_path" 2>/dev/null || stat -c%s "$output_path" 2>/dev/null || echo "0")
            local file_size_kb=$((file_size / 1024))
            print_color $GREEN "✓ 下载完成: $filename ($file_size_kb KB)"
            return 0
        else
            print_color $RED "✗ 下载失败: $filename"
            return 1
        fi
    else
        print_color $RED "✗ 下载失败: $filename"
        return 1
    fi
}



# 创建版本目录
create_version_directory() {
    local version=$1
    
    # 设置版本特定的SDK目录
    SDK_DIR="$SDK_BASE_DIR/$version"
    
    print_color $YELLOW "正在创建版本目录: $SDK_DIR"
    
    # 如果版本目录已存在，先清理
    if [[ -d "$SDK_DIR" ]]; then
        rm -rf "$SDK_DIR"/*
        print_color $GREEN "✓ 清理现有版本目录"
    else
        # 创建版本目录
        mkdir -p "$SDK_DIR"
        print_color $GREEN "✓ 创建版本目录完成"
    fi
    
    # 确保基础SDK目录存在
    if [[ ! -d "$SDK_BASE_DIR" ]]; then
        mkdir -p "$SDK_BASE_DIR"
    fi
}

# 从 CDN 下载 Amis SDK
download_from_cdn() {
    local version=$1
    
    print_color $YELLOW "正在从 CDN 下载 Amis SDK v$version..."
    
    # CDN 基础 URL
    local cdn_base="https://unpkg.com/amis@$version/sdk"
    
    # 定义需要下载的文件列表
    local files=(
        "sdk.js"
        "sdk.css"
        "sdk-ie11.css"
        "cxd.css"
        "cxd-ie11.css"
        "dark.css"
        "dark-ie11.css"
        "antd.css"
        "antd-ie11.css"
        "ang.css"
        "ang-ie11.css"
        "helper.css"
        "iconfont.css"
        "iconfont.eot"
        "iconfont.svg"
        "iconfont.ttf"
        "iconfont.woff"
        "ie11-patch.css"
    )
    
    # 可选的扩展文件
    local optional_files=(
        "charts.js"
        "rich-text.js"
        "rest.js"
        "office-viewer.js"
        "pdf-viewer.js"
        "json-view.js"
        "markdown.js"
        "codemirror.js"
        "color-picker.js"
        "cropperjs.js"
        "barcode.js"
        "exceljs.js"
        "fomula-doc.js"
        "papaparse.js"
        "tinymce.js"
        "xlsx.js"
    )
    
    local success_count=0
    local total_files=$((${#files[@]} + ${#optional_files[@]}))
    
    # 下载核心文件
    for file in "${files[@]}"; do
        local url="$cdn_base/$file"
        local output_path="$SDK_DIR/$file"
        
        if download_file "$url" "$output_path" "$file"; then
            ((success_count++))
        fi
    done
    
    # 下载可选文件（失败不影响整体流程）
    for file in "${optional_files[@]}"; do
        local url="$cdn_base/$file"
        local output_path="$SDK_DIR/$file"
        
        if download_file "$url" "$output_path" "$file"; then
            ((success_count++))
        fi
    done
    
    print_color $GREEN "下载完成: $success_count/$total_files 个文件"
    [[ $success_count -gt 0 ]]
}

# 验证下载的文件
validate_downloaded_files() {
    print_color $YELLOW "正在验证下载的文件..."
    
    local required_files=("sdk.js" "sdk.css" "cxd.css")
    local missing_files=()
    
    for file in "${required_files[@]}"; do
        local file_path="$SDK_DIR/$file"
        if [[ ! -f "$file_path" ]]; then
            missing_files+=("$file")
        else
            local file_size=$(stat -f%z "$file_path" 2>/dev/null || stat -c%s "$file_path" 2>/dev/null || echo "0")
            if [[ $file_size -lt 1024 ]]; then
                missing_files+=("$file (文件过小)")
            fi
        fi
    done
    
    if [[ ${#missing_files[@]} -eq 0 ]]; then
        print_color $GREEN "✓ 文件验证通过"
        return 0
    else
        print_color $RED "✗ 文件验证失败，缺少文件: ${missing_files[*]}"
        return 1
    fi
}

# 生成版本信息文件
generate_version_info() {
    local version=$1
    local version_info_path="$SDK_DIR/version.json"
    
    cat > "$version_info_path" << EOF
{
  "version": "$version",
  "updateTime": "$(date '+%Y-%m-%d %H:%M:%S')",
  "source": "$SOURCE",
  "updatedBy": "update-amis-sdk.sh"
}
EOF
    
    if [[ $? -eq 0 ]]; then
        print_color $GREEN "✓ 版本信息已保存: $version_info_path"
    else
        print_color $YELLOW "保存版本信息失败"
    fi
}

# 创建当前版本链接
create_current_version_link() {
    local version=$1
    local current_link_path="$SDK_BASE_DIR/current"
    
    # 删除现有的链接或目录
    if [[ -e "$current_link_path" ]]; then
        rm -rf "$current_link_path"
    fi
    
    # 尝试创建符号链接
    if ln -s "$SDK_DIR" "$current_link_path" 2>/dev/null; then
        print_color $GREEN "✓ 创建符号链接: current -> $version"
    else
        # 如果符号链接失败，则复制文件
        cp -r "$SDK_DIR" "$current_link_path"
        print_color $YELLOW "✓ 复制文件到 current 目录（符号链接失败）"
    fi
}

# 主函数
main() {
    print_color $MAGENTA "=== CodeSpirit Amis SDK 更新工具 ==="
    print_color $GRAY "Web项目路径: $WEB_PROJECT_ROOT"
    print_color $GRAY "SDK 基础目录: $SDK_BASE_DIR"
    echo ""
    
    # 检查依赖
    check_dependencies
    
    # 获取版本信息
    if [[ "$VERSION" == "latest" ]]; then
        VERSION=$(get_latest_version)
    fi
    
    print_color $CYAN "目标版本: $VERSION"
    print_color $CYAN "下载源: $SOURCE"
    
    # 创建版本目录
    create_version_directory "$VERSION"
    
    print_color $GRAY "版本目录: $SDK_DIR"
    echo ""
    
    # 下载新文件
    local download_success=false
    
    if [[ "$SOURCE" == "cdn" ]]; then
        if download_from_cdn "$VERSION"; then
            download_success=true
        fi
    else
        print_color $YELLOW "GitHub 下载源暂未实现，切换到 CDN 下载"
        if download_from_cdn "$VERSION"; then
            download_success=true
        fi
    fi
    
    if [[ "$download_success" != true ]]; then
        print_color $RED "下载失败"
        exit 1
    fi
    
    # 验证文件
    if ! validate_downloaded_files; then
        print_color $RED "文件验证失败"
        exit 1
    fi
    
    # 生成版本信息
    generate_version_info "$VERSION"
    
    # 创建当前版本链接
    create_current_version_link "$VERSION"
    
    echo ""
    print_color $GREEN "=== 更新完成 ==="
    print_color $GREEN "Amis SDK 已成功更新到版本 $VERSION"
    print_color $GRAY "文件保存位置: $SDK_DIR"
    
    echo ""
    print_color $YELLOW "请重新启动应用程序以使用新版本的 SDK"
}

# 错误处理
trap 'print_color $RED "脚本执行失败，错误发生在第 $LINENO 行"' ERR

# 解析参数并执行主函数
parse_args "$@"
main 