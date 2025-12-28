# CodeSpirit 文档站

这是 CodeSpirit 项目的 Docusaurus 文档站点。

## 🚀 快速开始

### 安装依赖

```bash
cd website
npm install
```

### 本地开发

```bash
npm start
```

这个命令会启动一个本地开发服务器并打开浏览器。大多数更改都会实时热更新，无需重启服务器。

### 构建

```bash
npm run build
```

这个命令会生成静态内容到 `build` 目录，可以使用任何静态内容托管服务进行部署。

## 📁 文档结构

文档源文件位于 `../Docs/` 目录，按以下结构组织：

```
Docs/
├── codespirit-ai-features-zh-CN.md          # AI 特色功能
├── codespirit-framework-highlights-zh-CN.md  # 框架核心亮点
├── 01-core-docs/                             # 核心文档
├── 02-ui-generation/                         # 界面生成引擎
├── 03-core-components/                       # 核心组件
├── 04-identity-auth/                         # 身份认证与权限
├── 05-multi-tenancy/                         # 多租户架构
├── 06-infrastructure/                        # 基础设施与运维
├── 07-api-communication/                     # API与通信
├── 08-project-management/                    # 项目管理
├── 09-exam-system/                           # 考试系统
├── 09-survey-system/                         # 问卷调查系统
└── 10-pathfinder-project/                    # Pathfinder项目
```

## 🌐 国际化

本文档站支持中英文双语：

- 默认语言：简体中文 (`zh-CN`)
- 支持语言：English (`en-US`)

文档命名规范：
- 中文文档：`filename-zh-cn.md`
- 英文文档：`filename-en-us.md`

## 📝 添加新文档

1. 在 `../Docs/` 对应目录下创建 Markdown 文件
2. 按照命名规范命名文件（使用小写+短横线格式）
3. 在 `sidebars.ts` 中添加文档引用
4. 提交更改，GitHub Actions 会自动部署

## 🔍 搜索功能

本项目使用 Algolia DocSearch 提供搜索功能。如需启用：

1. 访问 https://docsearch.algolia.com/apply/
2. 填写表单申请
3. 获得 App ID 和 API Key 后，更新 `docusaurus.config.ts` 中的配置

## 🚀 部署

### GitHub Pages

推送到 `main` 分支后，GitHub Actions 会自动构建并部署到 GitHub Pages。

### Gitee Pages

1. 构建静态文件：`npm run build`
2. 将 `build` 目录内容推送到 Gitee 仓库
3. 在 Gitee 仓库设置中启用 Gitee Pages

### 手动部署

```bash
npm run build
# 将 build 目录部署到任何静态托管服务
```

## 📚 参考资源

- [Docusaurus 官方文档](https://docusaurus.io/)
- [Markdown 语法](https://www.markdownguide.org/)
- [MDX 文档](https://mdxjs.com/)
