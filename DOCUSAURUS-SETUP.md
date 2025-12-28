# Docusaurus 文档站配置完成

## ✅ 配置状态

Docusaurus 文档站已成功配置并启动！

### 访问地址

- **本地开发**: http://localhost:3000/CodeSpirit/
- **GitHub Pages** (部署后): https://[your-username].github.io/code-spirit/

## 📁 项目结构

```
website/
├── docs/                    # 文档内容（从 Docs/ 复制）
├── src/
│   ├── css/
│   │   └── custom.css      # 自定义样式
│   └── pages/
│       ├── index.tsx       # 首页
│       └── index.module.css
├── static/
│   └── img/                # 静态图片资源
├── docusaurus.config.ts    # 主配置文件
├── sidebars.ts             # 侧边栏配置
├── package.json            # 依赖管理
├── tsconfig.json           # TypeScript 配置
├── babel.config.js         # Babel 配置
├── README.md               # 项目说明
└── MIGRATION-GUIDE.md      # 迁移指南
```

## 🚀 快速开始

### 1. 启动开发服务器

```bash
cd website
npm start
```

服务器将在 http://localhost:3000/CodeSpirit/ 启动。

### 2. 构建生产版本

```bash
cd website
npm run build
```

构建产物将生成在 `build/` 目录。

### 3. 本地预览生产版本

```bash
cd website
npm run serve
```

## 🌐 部署到 GitHub Pages

### 方式一：自动部署（推荐）

1. 在 GitHub 仓库中启用 Actions
2. 推送代码到 `main` 分支
3. GitHub Actions 将自动构建并部署到 `gh-pages` 分支

### 方式二：手动部署

```bash
cd website
npm run deploy
```

**注意**: 需要先在 `docusaurus.config.ts` 中配置：
- `organizationName`: 你的 GitHub 用户名
- `projectName`: 仓库名称

## 📝 文档管理

### 添加新文档

1. 在 `Docs/` 目录中添加 Markdown 文件
2. 复制到 `website/docs/` 目录
3. 在 `website/sidebars.ts` 中添加引用

### 更新文档

1. 修改 `Docs/` 目录中的文件
2. 重新复制到 `website/docs/`
3. 开发服务器会自动重新加载

### 文档命名规范

- 中文文档: `文件名-zh-CN.md`
- 英文文档: `文件名-en-US.md`
- 文档 ID: `目录名/文件名-语言代码`（不含 `.md` 扩展名）

## 🎨 自定义

### 修改主题颜色

编辑 `website/src/css/custom.css`:

```css
:root {
  --ifm-color-primary: #2e8555;
  --ifm-color-primary-dark: #29784c;
  /* ... */
}
```

### 修改导航栏

编辑 `website/docusaurus.config.ts` 中的 `themeConfig.navbar`:

```typescript
navbar: {
  title: 'CodeSpirit',
  logo: {
    alt: 'CodeSpirit Logo',
    src: 'img/logo.svg',
  },
  items: [
    // 添加导航项...
  ],
}
```

### 修改侧边栏

编辑 `website/sidebars.ts`:

```typescript
const sidebars: SidebarsConfig = {
  docs: [
    {
      type: 'category',
      label: '分类名称',
      items: [
        {
          type: 'doc',
          id: '文档ID',
          label: '显示名称',
        },
      ],
    },
  ],
};
```

## 🔧 配置说明

### 国际化 (i18n)

已配置中英文双语支持：

- 默认语言: 中文 (`zh-CN`)
- 支持语言: 英文 (`en-US`)

切换语言：点击导航栏右上角的语言切换按钮。

### 搜索功能

已配置 Algolia DocSearch（需要申请）。

临时方案：使用浏览器内置搜索（Ctrl+F / Cmd+F）。

## ⚠️ 已知问题

### 1. 文档内部链接警告

部分文档中的内部链接使用了旧的文件名，需要更新为新的文件名。

**解决方案**: 参考 `MIGRATION-GUIDE.md` 更新链接。

### 2. 缺失的文档

部分文档引用了不存在的文件，需要创建或移除引用。

### 3. 图片路径

部分图片使用了相对路径，可能需要调整为绝对路径或复制到 `static/img/`。

## 📚 相关文档

- [Docusaurus 官方文档](https://docusaurus.io/)
- [MIGRATION-GUIDE.md](./website/MIGRATION-GUIDE.md) - 详细的迁移指南
- [README.md](./website/README.md) - 项目说明

## 🎯 下一步

1. ✅ 配置完成
2. ✅ 启动开发服务器
3. ⏳ 更新文档内部链接
4. ⏳ 配置 GitHub Pages 部署
5. ⏳ 申请 Algolia DocSearch
6. ⏳ 自定义主题和样式

## 💡 提示

- 开发时修改配置文件需要重启服务器
- 修改文档内容会自动热重载
- 构建前建议先运行 `npm run clear` 清理缓存
- 使用 `npm run swizzle` 可以自定义组件

---

**配置完成时间**: 2025-12-28  
**Docusaurus 版本**: 3.9.2  
**Node 版本**: v22.14.0
