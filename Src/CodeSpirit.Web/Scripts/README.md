# CodeSpirit Amis SDK 一键更新工具

## 概述

本工具用于自动更新 CodeSpirit 项目中的 Amis SDK 文件到最新版本，支持从官方 CDN 下载最新的 SDK 文件并自动替换现有文件。

## 功能特性

- ✅ **npm 包下载**：直接下载 npm 官方包，确保文件完整性（v1.2.0 新增）
- ✅ **自动获取最新版本**：从 GitHub API 获取 Amis 最新版本信息（当前最新：6.12.0）
- ✅ **完整文件支持**：下载所有核心和扩展 SDK 文件（109 个文件）
- ✅ **版本管理**：按版本号分目录存储，支持多版本共存
- ✅ **当前版本链接**：自动创建 current 目录指向最新版本
- ✅ **版本验证**：下载后自动验证文件完整性
- ✅ **配置更新**：自动更新 libman.json 配置文件
- ✅ **版本追踪**：生成版本信息文件便于追踪
- ✅ **多平台支持**：支持 Windows、Linux、macOS
- ✅ **多种使用方式**：支持命令行和图形界面操作
- ✅ **智能下载策略**：优先使用 npm 包，备用 CDN 逐个文件下载

## 文件说明

| 文件名 | 说明 |
|--------|------|
| `UpdateAmisSDK.ps1` | PowerShell 主脚本，包含所有更新逻辑 |
| `UpdateAmisSDK.bat` | Windows 批处理文件，提供友好的图形界面 |
| `README.md` | 本说明文档 |

## 使用方法

### 方法一：使用批处理文件（推荐）

1. 双击运行 `UpdateAmisSDK.bat`
2. 根据菜单提示选择操作：
   - **选项 1**：更新到最新版本（推荐）
   - **选项 2**：更新到指定版本
   - **选项 3**：查看当前版本信息
   - **选项 4**：退出

### 方法二：直接使用 PowerShell 脚本

#### 更新到最新版本
```powershell
.\UpdateAmisSDK.ps1
```

#### 更新到指定版本
```powershell
.\UpdateAmisSDK.ps1 -Version "6.12.0"
```

#### 指定下载源
```powershell
.\UpdateAmisSDK.ps1 -Source "cdn"
```



## 参数说明

### PowerShell 脚本参数

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `-Version` | String | "latest" | 指定要下载的 Amis 版本号 |
| `-Source` | String | "cdn" | 下载源，支持 "cdn" 或 "github" |

## 更新的文件列表

### 核心文件（必需）
- `sdk.js` - Amis 核心 JavaScript 文件（~2MB）
- `sdk.css` - Amis 核心样式文件（~2.5MB）
- `sdk-ie11.css` - IE11 兼容样式文件（~2MB）

### 主题文件
- `cxd.css` / `cxd-ie11.css` - CXD 主题（默认主题）
- `dark.css` / `dark-ie11.css` - 暗色主题
- `antd.css` / `antd-ie11.css` - Ant Design 主题
- `ang.css` / `ang-ie11.css` - Angular 主题
- `helper.css` - 辅助样式文件

### 字体文件
- `iconfont.css` - 图标字体样式
- `iconfont.eot` / `iconfont.svg` / `iconfont.ttf` / `iconfont.woff` - 图标字体文件

### 扩展功能文件
- `charts.js` - 图表组件（~2.3MB）
- `rich-text.js` - 富文本编辑器
- `rest.js` - REST API 支持（~2.8MB）
- `office-viewer.js` - Office 文档查看器
- `pdf-viewer.js` - PDF 查看器
- `json-view.js` - JSON 查看器
- `markdown.js` - Markdown 支持
- `codemirror.js` - 代码编辑器
- `color-picker.js` - 颜色选择器
- `cropperjs.js` - 图片裁剪
- `barcode.js` - 条形码生成
- `exceljs.js` - Excel 处理
- `fomula-doc.js` - 公式文档
- `papaparse.js` - CSV 解析
- `tinymce.js` - TinyMCE 编辑器（~1.4MB）
- `xlsx.js` - Excel 文件处理

### 扩展目录（完整支持）
- `thirds/` - 第三方库目录（73 个文件）
  - `@fortawesome/` - FontAwesome 图标库（8 个文件）
  - `hls.js/` - HLS 视频流播放器（1 个文件）
  - `markdown-it/` - Markdown 解析器（1 个文件）
  - `moment-timezone/` - 时区处理库（1 个文件）
  - `monaco-editor/` - 代码编辑器（59 个文件，支持多种编程语言）
  - `mpegts.js/` - MPEG-TS 视频流（1 个文件）
  - `pdfjs-dist/` - PDF 查看器（2 个文件）
- `locale/` - 国际化语言包（1 个文件）
  - `de-DE.js` - 德语本地化文件

> **v1.2.0 更新**：现在通过 npm 包下载，确保获取到完整的 109 个文件，包括所有第三方库和本地化文件。

## 目录结构

```
Src/CodeSpirit.Web/
├── Scripts/
│   ├── UpdateAmisSDK.ps1      # PowerShell 主脚本
│   ├── UpdateAmisSDK.bat      # Windows 批处理文件
│   ├── update-amis-sdk.sh     # Linux/macOS 脚本
│   └── README.md              # 说明文档
└── wwwroot/
    └── sdk/                   # Amis SDK 文件目录
        ├── 6.12.0/            # 版本 6.12.0
        │   ├── sdk.js
        │   ├── sdk.css
        │   ├── version.json
        │   └── ...            # 其他 SDK 文件
        ├── 6.11.0/            # 版本 6.11.0（如果存在）
        └── ...                # 其他版本目录
```

## 版本信息文件

更新完成后，工具会在 SDK 目录下生成 `version.json` 文件，包含以下信息：

```json
{
  "version": "6.12.0",
  "updateTime": "2025-05-27 17:33:51",
  "source": "cdn",
  "updatedBy": "UpdateAmisSDK.ps1"
}
```

## 版本管理

- 每个版本的 SDK 文件都存储在独立的版本目录中（如 `6.12.0/`、`6.11.0/` 等）
- 支持多版本共存，可以随时切换使用不同版本
- 旧版本文件会自动保留，无需额外备份

## 错误处理

### 常见问题及解决方案

1. **PowerShell 执行策略限制**
   ```powershell
   Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
   ```

2. **网络连接问题**
   - 检查网络连接
   - 尝试使用代理或 VPN
   - 手动下载文件后放置到 SDK 目录

3. **权限不足**
   - 以管理员身份运行脚本
   - 检查文件夹写入权限

4. **文件被占用**
   - 停止 Web 应用程序
   - 关闭可能占用文件的进程

## 注意事项

1. **版本兼容性**
   - 新版本 SDK 可能包含破坏性更改
   - 建议在测试环境中先验证兼容性
   - 可以保留多个版本进行对比测试

2. **自定义修改**
   - 如果对 SDK 文件有自定义修改，更新后需要重新应用
   - 建议将自定义修改保存为单独的文件

3. **生产环境使用**
   - 生产环境更新前请充分测试
   - 建议在维护窗口期间进行更新
   - 可以快速回退到之前的版本目录

4. **磁盘空间**
   - 由于保留多个版本，请确保有足够的磁盘空间
   - 可以定期清理不需要的旧版本目录

## 手动更新方法

如果自动更新工具无法使用，可以手动更新：

1. 访问 [Amis 官方网站](https://aisuda.bce.baidu.com/amis/) 或 [unpkg CDN](https://unpkg.com/amis@latest/sdk/)
2. 下载所需的 SDK 文件
3. 备份现有的 `wwwroot/sdk` 目录
4. 将新文件复制到 `wwwroot/sdk` 目录
5. 重启应用程序

## 技术支持

如果在使用过程中遇到问题，请：

1. 查看脚本输出的错误信息
2. 检查网络连接和权限设置
3. 查看备份文件是否完整
4. 联系 CodeSpirit 开发团队获取支持

## 更新日志

### v1.2.0 (2025-05-27)
- 🚀 **重大改进**：新增 npm 包下载功能
- 📦 **完整性保证**：直接下载 npm 官方包，确保所有文件完整
- 📈 **文件数量提升**：从 44 个文件增加到 109 个文件
- 🔧 **智能下载策略**：优先使用 npm 包，备用 CDN 逐个文件下载
- 📁 **完整目录支持**：包含完整的 thirds 和 locale 目录
- ⚡ **性能优化**：一次下载整个压缩包，速度更快
- 🛠️ **解压工具支持**：支持 Windows 内置 tar 命令和 7-Zip

### v1.1.0 (2025-05-27)
- 移除备份逻辑，改用版本目录管理
- 支持多版本共存，每个版本独立存储
- 添加 current 目录指向最新版本
- 优化错误处理和用户体验
- 更新到最新版本 6.12.0

### v1.0.0 (2025-05-27)
- 初始版本发布
- 支持从 CDN 自动下载最新版本
- 提供 PowerShell 和批处理两种使用方式
- 添加版本验证和配置更新功能 