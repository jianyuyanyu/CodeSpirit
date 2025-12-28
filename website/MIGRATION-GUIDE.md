# 文档迁移指南

本指南说明如何将现有文档迁移到 Docusaurus 文档站。

## 📋 迁移步骤

### 方案一：创建符号链接（推荐）

这种方式不需要复制文件，直接使用现有的 `Docs` 目录。

#### Windows (需要管理员权限)

```powershell
# 在 website 目录下创建符号链接
cd website
New-Item -ItemType SymbolicLink -Path "docs" -Target "..\Docs"
```

#### Linux/macOS

```bash
cd website
ln -s ../Docs docs
```

### 方案二：复制文档文件

如果无法创建符号链接，可以复制文件：

```bash
# 复制整个 Docs 目录
cp -r Docs website/docs

# 或使用 PowerShell
Copy-Item -Path "Docs" -Destination "website\docs" -Recurse
```

## 🔄 自动同步脚本

如果使用复制方式，可以使用以下脚本保持同步：

### sync-docs.ps1 (Windows)

```powershell
# 同步文档脚本
$source = ".\Docs"
$destination = ".\website\docs"

Write-Host "同步文档中..." -ForegroundColor Green
Copy-Item -Path $source\* -Destination $destination -Recurse -Force
Write-Host "同步完成！" -ForegroundColor Green
```

### sync-docs.sh (Linux/macOS)

```bash
#!/bin/bash
# 同步文档脚本

SOURCE="./Docs"
DEST="./website/docs"

echo "同步文档中..."
rsync -av --delete "$SOURCE/" "$DEST/"
echo "同步完成！"
```

## 📝 文档格式调整

大多数 Markdown 文件可以直接使用，但需要注意以下几点：

### 1. 添加 Frontmatter（可选但推荐）

在每个文档顶部添加元数据：

```markdown
---
sidebar_position: 1
title: 文档标题
description: 文档描述
---

# 文档内容开始...
```

### 2. 更新内部链接

Docusaurus 使用不同的链接格式：

```markdown
<!-- 原格式 -->
[链接](../other-doc.md)

<!-- Docusaurus 格式 -->
[链接](./other-doc)
或
[链接](/docs/category/other-doc)
```

### 3. 图片路径

图片应放在 `website/static/img/` 目录：

```markdown
<!-- 使用绝对路径 -->
![图片](/img/screenshot.png)
```

## 🔧 自动化工具

### 批量添加 Frontmatter

创建脚本 `add-frontmatter.js`：

```javascript
const fs = require('fs');
const path = require('path');

function addFrontmatter(filePath) {
  const content = fs.readFileSync(filePath, 'utf-8');
  
  // 如果已有 frontmatter，跳过
  if (content.startsWith('---')) {
    return;
  }
  
  const filename = path.basename(filePath, '.md');
  const title = filename
    .replace(/-/g, ' ')
    .replace(/\b\w/g, l => l.toUpperCase());
  
  const frontmatter = `---
title: ${title}
---

`;
  
  fs.writeFileSync(filePath, frontmatter + content);
  console.log(`已处理: ${filePath}`);
}

// 递归处理目录
function processDirectory(dir) {
  const files = fs.readdirSync(dir);
  
  files.forEach(file => {
    const filePath = path.join(dir, file);
    const stat = fs.statSync(filePath);
    
    if (stat.isDirectory()) {
      processDirectory(filePath);
    } else if (file.endsWith('.md')) {
      addFrontmatter(filePath);
    }
  });
}

processDirectory('./docs');
```

运行：
```bash
node add-frontmatter.js
```

### 更新链接格式

创建脚本 `update-links.js`：

```javascript
const fs = require('fs');
const path = require('path');

function updateLinks(filePath) {
  let content = fs.readFileSync(filePath, 'utf-8');
  
  // 更新 .md 链接
  content = content.replace(
    /\[([^\]]+)\]\(([^)]+)\.md\)/g,
    '[$1]($2)'
  );
  
  fs.writeFileSync(filePath, content);
  console.log(`已更新: ${filePath}`);
}

function processDirectory(dir) {
  const files = fs.readdirSync(dir);
  
  files.forEach(file => {
    const filePath = path.join(dir, file);
    const stat = fs.statSync(filePath);
    
    if (stat.isDirectory()) {
      processDirectory(filePath);
    } else if (file.endsWith('.md')) {
      updateLinks(filePath);
    }
  });
}

processDirectory('./docs');
```

## ✅ 验证检查清单

迁移完成后，检查以下项目：

- [ ] 所有文档文件都在 `website/docs` 目录下
- [ ] `sidebars.ts` 已配置所有文档
- [ ] 文档内部链接正常工作
- [ ] 图片正确显示
- [ ] 代码块语法高亮正常
- [ ] 运行 `npm start` 能正常预览
- [ ] 运行 `npm run build` 构建成功

## 🐛 常见问题

### 问题1：链接失效

**原因**：Docusaurus 不需要 `.md` 扩展名

**解决**：移除链接中的 `.md` 扩展名

### 问题2：图片不显示

**原因**：图片路径不正确

**解决**：
1. 将图片移动到 `website/static/img/`
2. 使用 `/img/` 开头的绝对路径

### 问题3：侧边栏不显示

**原因**：文档 ID 在 `sidebars.ts` 中配置不正确

**解决**：确保文档 ID 与文件路径匹配（不包含 `.md`）

## 📞 获取帮助

如有问题，请查看：
- [Docusaurus 官方文档](https://docusaurus.io/)
- [GitHub Issues](https://github.com/xin-lai/CodeSpirit/issues)

