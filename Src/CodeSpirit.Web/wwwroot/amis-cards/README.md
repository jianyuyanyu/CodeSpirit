# CodeSpirit Amis Cards V2.0

## 概述

基于 Amis Page 组件的统一表格卡片系统，与现有 `cards-sdk` 并行存在，提供更现代化、配置化的解决方案。

📋 **开发前必读**: [PRINCIPLES.md](./PRINCIPLES.md) - 项目基本原则和开发准则，确保开发过程不偏离核心理念。

## 项目结构

```
Src/CodeSpirit.Web/wwwroot/amis-cards/
├── README.md                     # 项目文档
├── PRINCIPLES.md                 # 基本原则
├── core/                         # 核心SDK
│   ├── amis-cards-core.js        # 核心SDK
│   ├── data-service.js           # 数据服务
│   └── utils.js                  # 工具函数
├── renderers/                    # 卡片渲染器
│   ├── base-renderer.js          # 基础渲染器
│   ├── stat-renderer.js          # 统计卡片
│   ├── chart-renderer.js         # 图表卡片
│   ├── table-renderer.js         # 表格卡片
│   ├── info-renderer.js          # 信息卡片
│   └── info-grid-renderer.js     # 信息网格卡片
├── configs/                      # 配置文件
│   ├── card-configs.js           # 卡片配置
│   └── theme-configs.js          # 主题配置
├── styles/                       # 样式文件
│   ├── amis-cards.css            # 主样式文件
│   └── themes/                   # 主题样式
│       ├── default.css           # 默认主题
│       └── dark.css              # 暗色主题
├── docs/                         # 详细文档
│   ├── card-usage-guide.md       # 卡片使用总指南
│   ├── stat-card-guide.md        # 统计卡片指南
│   ├── chart-card-guide.md       # 图表卡片指南
│   ├── table-card-guide.md       # 表格卡片指南
│   ├── info-card-guide.md        # 信息卡片指南
│   └── info-grid-guide.md        # 信息网格卡片指南
├── tests/                        # 测试文件
│   └── info-grid-test.html       # InfoGrid测试页面
└── demo/                         # 演示页面
    ├── index.html                # 演示首页
    ├── monitor-dashboard.html    # 监控仪表板演示
    ├── mock-data.js              # 模拟数据
    └── assets/                   # 演示资源
```

## 核心特性

### 1. 统一架构
- 基于 Amis Page 组件的完整页面架构
- 配置化开发，减少手写代码
- 标准化的组件库和样式系统

### 2. 原生能力
- 利用 Amis 的轮询、下拉刷新功能
- 内置的筛选、排序、分页功能
- 响应式布局和移动端适配

### 3. 模块化设计
- 核心SDK与业务配置分离
- 可插拔的渲染器系统
- 独立的样式和主题系统

### 4. 演示系统
- 完整的静态HTML演示页面
- 独立的模拟API服务
- 丰富的示例和文档

## 技术栈

### 前端技术
- **Amis**: 基础UI框架和组件库
- **ES6+**: 现代JavaScript语法
- **CSS3**: 现代样式和动画
- **Responsive Design**: 响应式设计

### 依赖关系
- **必需依赖**:
  - Amis SDK (6.12.0+)
  - TokenManager (现有)

## 支持的卡片类型

### 1. 统计卡片 (stat) 📊
用于展示数值统计信息，支持趋势显示、图标配置和实时更新。

**主要特性**：
- 数值格式化（整数、货币、百分比、文件大小）
- 图标配置（位置、尺寸、颜色、背景）
- 趋势显示（上升/下降/稳定）
- 进度条显示

**详细文档**：[统计卡片使用指南](./docs/stat-card-guide.md)

### 2. 图表卡片 (chart) 📈
基于ECharts的图表展示卡片，支持多种图表类型。

**主要特性**：
- 多种图表类型（折线图、柱状图、饼图、散点图等）
- 数据源配置（静态数据、API数据）
- 图表交互和动画
- 响应式图表尺寸

**详细文档**：[图表卡片使用指南](./docs/chart-card-guide.md)

### 3. 表格卡片 (table) 📋
功能完整的表格展示卡片，支持搜索、排序、分页等功能。

**主要特性**：
- 多种列类型（文本、数字、日期、状态、映射等）
- 搜索功能（基本搜索、高级搜索）
- 操作功能（行操作、批量操作、工具栏）
- 分页和排序

**详细文档**：[表格卡片使用指南](./docs/table-card-guide.md)

### 4. 信息卡片 (info) 📄
用于展示静态信息内容，支持富文本和多种信息类型。

**主要特性**：
- 多种信息类型（文本、HTML、模板、列表、属性等）
- 富文本支持
- 自定义布局
- 操作按钮集成

**详细文档**：[信息卡片使用指南](./docs/info-card-guide.md)

### 5. 信息网格卡片 (info-grid) 🔲
专门用于展示网格化信息项，适用于监控大屏和系统概览。

**主要特性**：
- 灵活的网格布局（自适应、固定列数、自定义CSS）
- 丰富的图标支持
- 响应式设计
- 条件样式和高亮

**详细文档**：[信息网格卡片使用指南](./docs/info-grid-guide.md)

## 📚 文档导航

### 📖 使用指南
- [**卡片使用总指南**](./docs/card-usage-guide.md) - 快速开始和总体概览
- [统计卡片详细指南](./docs/stat-card-guide.md) - 数值统计和趋势展示
- [图表卡片详细指南](./docs/chart-card-guide.md) - 数据可视化和图表配置
- [表格卡片详细指南](./docs/table-card-guide.md) - 表格数据展示和操作
- [信息卡片详细指南](./docs/info-card-guide.md) - 信息内容展示和布局
- [信息网格卡片详细指南](./docs/info-grid-guide.md) - 网格化信息展示

### 🎯 快速链接
- [演示页面](./demo/index.html) - 在线演示和示例
- [监控仪表板演示](./demo/monitor-dashboard.html) - 实际应用场景
- [InfoGrid测试页面](./tests/info-grid-test.html) - 功能测试

## 🚀 快速开始

### 1. 引入SDK

```html
<!DOCTYPE html>
<html lang="zh-CN">
<head>
    <!-- Amis CSS 资源 -->
    <link rel="stylesheet" href="/sdk/6.12.0/antd.css">
    <link rel="stylesheet" href="/sdk/6.12.0/helper.css">
    <link rel="stylesheet" href="/sdk/6.12.0/iconfont.css">
    
    <!-- Amis Cards 样式 -->
    <link rel="stylesheet" href="/amis-cards/styles/amis-cards.css">
</head>
<body>
    <div id="app-container"></div>
    
    <!-- Amis SDK -->
    <script src="/sdk/6.12.0/sdk.js"></script>
    
    <!-- Amis Cards Core -->
    <script src="/amis-cards/core/amis-cards-core.js"></script>
    <script src="/amis-cards/core/data-service.js"></script>
    
    <!-- 配置文件 -->
    <script src="/amis-cards/configs/card-configs.js"></script>
</body>
</html>
```

### 2. 基本使用

```javascript
// 创建Amis Cards实例
const amisCards = new AmisCards.Core({
    namespace: 'MyApp',
    baseUrl: '/api',
    theme: 'antd',
    debug: true
});

// 渲染仪表板页面
const dashboardConfig = {
    type: 'page',
    title: '智慧管理平台',
    body: [
        {
            type: 'grid',
            columns: [
                {
                    body: {
                        type: 'stat',
                        title: '总用户数',
                        data: { value: 1248, unit: '人' },
                        style: { theme: 'primary' }
                    }
                },
                {
                    body: {
                        type: 'stat',
                        title: '今日活跃',
                        data: { value: 856, unit: '人' },
                        style: { theme: 'success' }
                    }
                }
            ]
        }
    ]
};

await amisCards.renderPage('#app-container', dashboardConfig);
```

## 🎨 主题配置

支持多种内置主题：

- `default` - 默认主题
- `primary` - 主色调主题
- `success` - 成功主题（绿色）
- `warning` - 警告主题（橙色）
- `danger` - 危险主题（红色）
- `info` - 信息主题（蓝色）
- `dark` - 深色主题

```javascript
// 创建实例时指定主题
const cards = AmisCards.create({
    theme: 'dark'
});

// 动态切换主题
await cards.setTheme('primary');
```

## 🔧 API 参考

### 核心方法

| 方法 | 参数 | 返回值 | 描述 |
|------|------|--------|------|
| `renderPage` | (containerId, pageConfig, options?) | Promise&lt;Instance&gt; | 渲染Amis页面 |
| `destroyPage` | (instanceId) | void | 销毁页面实例 |
| `getDataService` | () | DataService | 获取数据服务实例 |

### 配置选项

| 参数 | 类型 | 默认值 | 描述 |
|------|------|--------|------|
| `namespace` | string | 'AmisCards' | 命名空间，避免冲突 |
| `baseUrl` | string | '/api' | API基础路径 |
| `theme` | string | 'antd' | Amis主题 |
| `debug` | boolean | false | 调试模式 |

## 🌐 浏览器支持

- Chrome 80+
- Firefox 75+
- Safari 13+
- Edge 80+
- 移动端浏览器

## 🔄 更新日志

### v2.0.0
- 🎉 新增InfoGrid信息网格卡片
- 🎨 支持多种图标位置和尺寸
- 🔧 优化图标样式和动画效果
- 📱 改进移动端图标显示
- 🌙 完善深色主题图标适配
- 📚 完善文档系统

### v1.0.0
- 🚀 初始版本发布
- 📊 支持统计、图表、表格、信息卡片
- 🎨 多主题支持
- 📱 响应式设计

## 🤝 贡献指南

欢迎提交 Issue 和 Pull Request！参与贡献前请阅读[贡献指南](docs/contributing.md)。

---

**版本信息**
- 项目版本：v2.0.0
- 适用于：CodeSpirit Amis Cards V2.0
- 作者：CodeSpirit 开发团队