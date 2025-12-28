# CodeSpirit 文档站快速设置脚本

Write-Host "==================================" -ForegroundColor Cyan
Write-Host "  CodeSpirit 文档站快速设置" -ForegroundColor Cyan
Write-Host "==================================" -ForegroundColor Cyan
Write-Host ""

# 1. 安装依赖
Write-Host "[1/4] 安装依赖..." -ForegroundColor Yellow
Set-Location website
npm install
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ 依赖安装失败！" -ForegroundColor Red
    exit 1
}
Write-Host "✅ 依赖安装完成！" -ForegroundColor Green
Write-Host ""

# 2. 创建文档符号链接
Write-Host "[2/4] 创建文档链接..." -ForegroundColor Yellow
if (Test-Path "docs") {
    Write-Host "⚠️  docs 目录已存在，跳过..." -ForegroundColor Yellow
} else {
    try {
        New-Item -ItemType SymbolicLink -Path "docs" -Target "..\Docs" -ErrorAction Stop
        Write-Host "✅ 文档链接创建成功！" -ForegroundColor Green
    } catch {
        Write-Host "⚠️  无法创建符号链接（需要管理员权限），改为复制文件..." -ForegroundColor Yellow
        Copy-Item -Path "..\Docs" -Destination "docs" -Recurse
        Write-Host "✅ 文档复制完成！" -ForegroundColor Green
    }
}
Write-Host ""

# 3. 复制图片资源
Write-Host "[3/4] 复制图片资源..." -ForegroundColor Yellow
if (Test-Path "..\Res") {
    Copy-Item -Path "..\Res\*" -Destination "static\img\" -Recurse -Force
    Write-Host "✅ 图片资源复制完成！" -ForegroundColor Green
} else {
    Write-Host "⚠️  Res 目录不存在，跳过..." -ForegroundColor Yellow
}
Write-Host ""

# 4. 创建 GitHub Actions 配置
Write-Host "[4/4] 创建 GitHub Actions 配置..." -ForegroundColor Yellow
$workflowDir = "..\.github\workflows"
if (-not (Test-Path $workflowDir)) {
    New-Item -ItemType Directory -Path $workflowDir -Force | Out-Null
}

$workflowContent = @"
name: Deploy Documentation

on:
  push:
    branches:
      - main
    paths:
      - 'Docs/**'
      - 'website/**'
      - '.github/workflows/deploy-docs.yml'
  workflow_dispatch:

permissions:
  contents: read
  pages: write
  id-token: write

concurrency:
  group: "pages"
  cancel-in-progress: false

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - name: Checkout
        uses: actions/checkout@v4
        
      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: '20'
          cache: 'npm'
          cache-dependency-path: website/package-lock.json
          
      - name: Install dependencies
        working-directory: ./website
        run: npm ci
        
      - name: Build website
        working-directory: ./website
        run: npm run build
        
      - name: Upload artifact
        uses: actions/upload-pages-artifact@v3
        with:
          path: ./website/build

  deploy:
    environment:
      name: github-pages
      url: `${{ steps.deployment.outputs.page_url }}
    runs-on: ubuntu-latest
    needs: build
    steps:
      - name: Deploy to GitHub Pages
        id: deployment
        uses: actions/deploy-pages@v4
"@

Set-Content -Path "$workflowDir\deploy-docs.yml" -Value $workflowContent
Write-Host "✅ GitHub Actions 配置创建完成！" -ForegroundColor Green
Write-Host ""

# 完成
Write-Host "==================================" -ForegroundColor Cyan
Write-Host "  ✅ 设置完成！" -ForegroundColor Green
Write-Host "==================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "下一步操作：" -ForegroundColor Yellow
Write-Host ""
Write-Host "1. 启动本地开发服务器：" -ForegroundColor White
Write-Host "   cd website" -ForegroundColor Gray
Write-Host "   npm start" -ForegroundColor Gray
Write-Host ""
Write-Host "2. 构建生产版本：" -ForegroundColor White
Write-Host "   npm run build" -ForegroundColor Gray
Write-Host ""
Write-Host "3. 预览生产版本：" -ForegroundColor White
Write-Host "   npm run serve" -ForegroundColor Gray
Write-Host ""
Write-Host "4. 部署到 GitHub Pages：" -ForegroundColor White
Write-Host "   - 推送代码到 main 分支" -ForegroundColor Gray
Write-Host "   - GitHub Actions 会自动构建和部署" -ForegroundColor Gray
Write-Host "   - 在仓库设置中启用 GitHub Pages (Settings > Pages)" -ForegroundColor Gray
Write-Host ""
Write-Host "📚 查看详细文档：website/README.md" -ForegroundColor Cyan
Write-Host "📚 迁移指南：website/MIGRATION-GUIDE.md" -ForegroundColor Cyan
Write-Host ""

