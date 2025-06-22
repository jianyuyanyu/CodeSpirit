# CodeSpirit Amis Cards V2.0 架构方案

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
│   └── info-renderer.js          # 信息卡片
├── configs/                      # 配置文件
│   ├── card-configs.js           # 卡片配置
│   └── theme-configs.js          # 主题配置
├── styles/                       # 样式文件
│   ├── amis-cards.css            # 主样式文件
│   └── themes/                   # 主题样式
│       ├── default.css           # 默认主题
│       └── dark.css              # 暗色主题
└── demo/                         # 演示页面
    ├── index.html                # 演示首页
    ├── mock-data.js              # 模拟数据
    └── assets/                   # 演示资源
        ├── demo.css              # 演示样式
        └── demo.js               # 演示脚本
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

### 1. 统计卡片 (stat)

用于展示数值统计信息，支持趋势显示和实时更新。

```javascript
{
    id: 'exam-count',
    type: 'stat',
    title: '今日考试',
    subtitle: '实时统计数据',
    size: 'medium', // small | medium | large
    style: { theme: 'primary' }, // primary | success | warning | danger | info
    data: {
        value: 156,
        label: '场次',
        unit: '个',
        trend: {
            direction: 'up', // up | down | stable
            value: 12,
            period: '较昨日'
        }
    },
    autoRefresh: true,
    refreshInterval: 30000
}
```

### 2. 图表卡片 (chart)
```javascript
{
    id: 'trend-chart',
    type: 'chart',
    title: '考试趋势图',
    subtitle: '最近6个月数据',
    size: 'large',
    style: { height: 400, theme: 'info' },
    data: {
        chartType: 'line', // line | bar | pie | area | scatter
        chartTitle: '月度考试趋势',
        api: '/api/charts/exam-trend', // 支持API数据源
        config: {
            xAxis: {
                type: 'category',
                data: ['1月', '2月', '3月', '4月', '5月', '6月']
            },
            yAxis: {
                type: 'value'
            },
            series: [{
                name: '考试场次',
                type: 'line',
                data: [120, 132, 101, 134, 90, 230],
                smooth: true
            }]
        }
    }
}
```

### 3. 信息卡片 (info)

用于展示静态信息内容，支持富文本和自定义布局。

```javascript
{
    id: 'system-info',
    type: 'info',
    title: '系统通知',
    subtitle: '重要信息公告',
    size: 'medium',
    style: { theme: 'warning' },
    data: {
        content: `
            <div class="info-content">
                <h5>系统维护通知</h5>
                <p>系统将于今晚23:00-01:00进行维护升级，期间可能影响正常使用。</p>
                <p><strong>维护时间：</strong>2024年1月15日 23:00-01:00</p>
                <p><strong>影响范围：</strong>考试系统、用户管理</p>
            </div>
        `,
        actions: [
            {
                label: '查看详情',
                type: 'button',
                level: 'info',
                actionType: 'dialog'
            }
        ]
    }
}
```

### 4. 操作卡片 (action)

提供快速操作按钮组，支持各种Amis交互动作。

```javascript
{
    id: 'quick-actions',
    type: 'action',
    title: '快速操作',
    subtitle: '常用功能入口',
    size: 'medium',
    style: { theme: 'success' },
    data: {
        layout: 'grid', // grid | list | inline
        actions: [
            {
                label: '创建考试',
                icon: 'fa fa-plus',
                level: 'primary',
                actionType: 'drawer',
                drawer: {
                    title: '创建新考试',
                    size: 'lg',
                    body: {
                        type: 'form',
                        api: 'post:/api/exams',
                        body: [/* 表单配置 */]
                    }
                }
            },
            {
                label: '导入学生',
                icon: 'fa fa-upload',
                level: 'info',
                actionType: 'dialog',
                dialog: {
                    title: '批量导入学生',
                    body: {
                        type: 'form',
                        body: [
                            {
                                type: 'input-file',
                                name: 'file',
                                label: '选择Excel文件',
                                accept: '.xlsx,.xls'
                            }
                        ]
                    }
                }
            },
            {
                label: '系统设置',
                icon: 'fa fa-cogs',
                level: 'default',
                actionType: 'url',
                url: '/admin/settings'
            }
        ]
    }
}
```

### 5. 表单卡片 (form)

基于Amis Form组件的表单卡片，支持复杂表单逻辑。

```javascript
{
    id: 'search-form',
    type: 'form',
    title: '查询条件',
    size: 'large',
    style: { theme: 'default' },
    data: {
        mode: 'horizontal',
        wrapWithPanel: false,
        submitText: '查询',
        actions: [
            {
                type: 'submit',
                label: '查询',
                level: 'primary'
            },
            {
                type: 'reset',
                label: '重置'
            }
        ],
        body: [
            {
                type: 'input-text',
                name: 'keyword',
                label: '关键词',
                placeholder: '请输入搜索关键词'
            },
            {
                type: 'select',
                name: 'category',
                label: '分类',
                options: [
                    { label: '全部', value: '' },
                    { label: '学生', value: 'student' },
                    { label: '考试', value: 'exam' }
                ]
            }
        ]
    }
}
```

## 使用场景

### 1. 统计仪表板
- **统计卡片**: 展示关键指标（考试数量、学生人数、通过率等）
- **图表卡片**: 展示趋势分析、数据对比
- **信息卡片**: 显示系统通知、重要公告
- **操作卡片**: 提供快速操作入口

### 2. 数据管理界面
- **表格展示**: 学生列表、考试记录、统计报表
- **表单处理**: 数据录入、查询条件、批量操作
- **图表分析**: 成绩分布、趋势分析、对比报告

### 3. 监控管理
- **实时监控**: 考试状态、系统状态、用户行为
- **告警通知**: 异常提醒、系统消息、操作反馈
- **快速响应**: 紧急处理、批量操作、状态切换

### 4. 移动端适配
- **响应式布局**: 自动适配桌面、平板、手机
- **触控友好**: 优化触控交互体验
- **性能优化**: 移动端性能优化

## 🎨 主题样式

支持多种主题配色方案：

- `default` - 默认蓝色主题 (#007bff)
- `primary` - 主要蓝色 (#007bff)
- `success` - 成功绿色 (#28a745)
- `warning` - 警告橙色 (#ffc107)
- `danger` - 危险红色 (#dc3545)
- `info` - 信息青色 (#17a2b8)
- `dark` - 暗色主题 (#343a40)
- `light` - 浅色主题 (#f8f9fa)

### 主题使用示例

```javascript
// 统计卡片使用成功主题
{
    id: 'pass-rate',
    type: 'stat',
    style: { theme: 'success' },
    data: {
        value: 98.5,
        label: '通过率',
        unit: '%'
    }
}

// 警告信息卡片
{
    id: 'warning-notice',
    type: 'info',
    style: { theme: 'warning' },
    title: '系统维护通知'
}
```

## 📱 响应式设计

SDK 采用移动优先的响应式设计，自动适配各种设备：

### 断点设置
- **Extra Small (xs)**: < 576px (手机竖屏)
- **Small (sm)**: ≥ 576px (手机横屏)
- **Medium (md)**: ≥ 768px (平板)
- **Large (lg)**: ≥ 992px (桌面)
- **Extra Large (xl)**: ≥ 1200px (大屏幕)

### 布局适配
```css
/* 桌面端：4列网格 */
@media (min-width: 992px) {
    .amis-cards-grid {
        grid-template-columns: repeat(4, 1fr);
    }
}

/* 平板端：2列网格 */
@media (min-width: 768px) and (max-width: 991.98px) {
    .amis-cards-grid {
        grid-template-columns: repeat(2, 1fr);
    }
}

/* 手机端：单列布局 */
@media (max-width: 767.98px) {
    .amis-cards-grid {
        grid-template-columns: 1fr;
    }
}
```

### 卡片尺寸适配
```javascript
{
    id: 'responsive-card',
    type: 'stat',
    size: 'large', // 基础尺寸
    responsive: {
        xs: 'small',  // 手机端使用小尺寸
        sm: 'medium', // 平板端使用中等尺寸
        lg: 'large'   // 桌面端使用大尺寸
    }
}
```

## 配置示例

### 混合卡片布局
```javascript
const dashboardConfig = {
    type: 'page',
    title: '智慧考试管理平台',
    className: 'amis-cards-dashboard',
    body: [
        // 统计卡片区域
        {
            type: 'grid',
            className: 'stats-grid',
            columns: [
                {
                    body: {
                        id: 'total-exams',
                        type: 'stat',
                        title: '总考试数',
                        style: { theme: 'primary' },
                        data: {
                            value: 1248,
                            unit: '场',
                            trend: { direction: 'up', value: 8.2, period: '本月' }
                        }
                    }
                },
                {
                    body: {
                        id: 'active-students',
                        type: 'stat',
                        title: '活跃学生',
                        style: { theme: 'success' },
                        data: {
                            value: 3672,
                            unit: '人',
                            trend: { direction: 'up', value: 12.5, period: '本周' }
                        }
                    }
                },
                {
                    body: {
                        id: 'pass-rate',
                        type: 'stat',
                        title: '通过率',
                        style: { theme: 'info' },
                        data: {
                            value: 94.8,
                            unit: '%',
                            trend: { direction: 'stable', value: 0.3, period: '较上月' }
                        }
                    }
                },
                {
                    body: {
                        id: 'system-alerts',
                        type: 'stat',
                        title: '系统告警',
                        style: { theme: 'warning' },
                        data: {
                            value: 3,
                            unit: '条',
                            trend: { direction: 'down', value: 2, period: '今日' }
                        }
                    }
                }
            ]
        },
        
        // 图表和操作区域
        {
            type: 'grid',
            className: 'charts-actions-grid',
            columns: [
                {
                    md: 8,
                    body: {
                        id: 'exam-trend',
                        type: 'chart',
                        title: '考试趋势分析',
                        style: { height: 350, theme: 'default' },
                        data: {
                            api: '/api/charts/exam-trend'
                        }
                    }
                },
                {
                    md: 4,
                    body: [
                        {
                            id: 'quick-actions',
                            type: 'action',
                            title: '快速操作',
                            style: { theme: 'success' },
                            data: {
                                layout: 'list',
                                actions: [
                                    {
                                        label: '创建考试',
                                        icon: 'fa fa-plus',
                                        level: 'primary'
                                    },
                                    {
                                        label: '导入学生',
                                        icon: 'fa fa-upload',
                                        level: 'info'
                                    },
                                    {
                                        label: '生成报告',
                                        icon: 'fa fa-file-alt',
                                        level: 'success'
                                    }
                                ]
                            }
                        },
                        {
                            id: 'system-notices',
                            type: 'info',
                            title: '系统通知',
                            style: { theme: 'warning' },
                            data: {
                                content: '<p>系统将于今晚进行例行维护...</p>'
                            }
                        }
                    ]
                }
            ]
        },
        
        // 表格区域
        {
            type: 'crud',
            title: '最近考试记录',
            api: '/api/exams/recent',
            interval: 30000,
            headerToolbar: ['filter-toggler', 'reload'],
            columns: [
                { name: 'name', label: '考试名称', type: 'text' },
                { name: 'subject', label: '科目', type: 'text' },
                { name: 'participants', label: '参与人数', type: 'number' },
                { name: 'status', label: '状态', type: 'mapping' },
                { name: 'createTime', label: '创建时间', type: 'datetime' }
            ]
        }
    ]
};
```

### 移动端优化配置
```javascript
const mobileOptimizedConfig = {
    type: 'page',
    className: 'mobile-optimized-dashboard',
    body: [
        // 移动端紧凑统计卡片
        {
            type: 'grid',
            columns: [
                {
                    xs: 6, sm: 3,
                    body: {
                        type: 'stat',
                        title: '考试',
                        size: 'small',
                        data: { value: 156, unit: '场' }
                    }
                },
                {
                    xs: 6, sm: 3,
                    body: {
                        type: 'stat',
                        title: '学生',
                        size: 'small',
                        data: { value: 1248, unit: '人' }
                    }
                }
            ]
        },
        
        // 移动端滑动操作卡片
        {
            type: 'action',
            className: 'mobile-actions',
            data: {
                layout: 'inline',
                actions: [
                    { label: '开始考试', level: 'primary', size: 'sm' },
                    { label: '查看成绩', level: 'info', size: 'sm' },
                    { label: '设置', level: 'default', size: 'sm' }
                ]
            }
        }
    ]
};
```

## 迁移策略

### 阶段1: 并行部署
- 部署新架构，与现有系统并存
- 创建新的演示页面
- 完善文档和示例

### 阶段2: 功能验证
- 验证核心功能的完整性
- 性能测试和优化
- 用户反馈收集

### 阶段3: 逐步迁移
- 新功能优先使用新架构
- 逐步迁移现有功能
- 保持向后兼容

### 阶段4: 统一维护
- 完成迁移后统一维护
- 清理旧代码和文档
- 建立新的开发规范

## 开发规范

### 1. 代码规范
- 使用 ES6+ 语法
- 统一的命名规范
- 完善的注释文档

### 2. 配置规范
- 标准化的配置结构
- 类型定义和验证
- 默认值和可选项

### 3. 样式规范
- BEM 命名方法
- 模块化的样式结构
- 响应式设计原则

### 4. 文档规范
- 完整的API文档
- 丰富的示例代码
- 清晰的使用指南

## 性能优化

### 1. 代码分割
- 按功能模块分割代码
- 延迟加载非核心功能
- 减少初始加载时间

### 2. 缓存策略
- 合理的缓存配置
- 版本化的资源管理
- CDN 加速支持

### 3. 数据优化
- 分页和虚拟滚动
- 防抖和节流处理
- 智能的数据刷新

## 安全考虑

### 1. 认证授权
- 集成现有 TokenManager
- 请求拦截和权限控制
- 敏感数据保护

### 2. 数据安全
- XSS 防护
- CSRF 防护
- 数据验证和过滤

### 3. 接口安全
- API 签名验证
- 请求频率限制
- 错误信息脱敏

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
    <script src="/amis-cards/core/mock-data.js"></script>
    
    <!-- 配置文件 -->
    <script src="/amis-cards/configs/card-configs.js"></script>
    <script src="/amis-cards/configs/page-configs.js"></script>
</body>
</html>
```

### 2. 初始化SDK

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
        // 统计卡片网格
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

### 3. 混合卡片布局

```javascript
const mixedLayoutConfig = {
    type: 'page',
    title: '综合仪表板',
    body: [
        // 第一行：统计卡片
        {
            type: 'grid',
            className: 'stats-row',
            columns: [
                { body: { type: 'stat', /* 统计配置 */ } },
                { body: { type: 'stat', /* 统计配置 */ } },
                { body: { type: 'stat', /* 统计配置 */ } },
                { body: { type: 'stat', /* 统计配置 */ } }
            ]
        },
        
        // 第二行：图表和操作
        {
            type: 'grid',
            columns: [
                {
                    md: 8,
                    body: {
                        type: 'chart',
                        title: '趋势分析',
                        data: { api: '/api/charts/trend' }
                    }
                },
                {
                    md: 4,
                    body: [
                        {
                            type: 'action',
                            title: '快速操作',
                            data: { actions: [/* 操作配置 */] }
                        },
                        {
                            type: 'info',
                            title: '系统通知',
                            data: { content: '最新公告...' }
                        }
                    ]
                }
            ]
        },
        
        // 第三行：数据表格
        {
            type: 'crud',
            title: '数据列表',
            api: '/api/data/list',
            columns: [/* 表格列配置 */]
        }
    ]
};
```

## 🔧 API 参考

### Core SDK 配置选项

| 参数 | 类型 | 默认值 | 描述 |
|------|------|--------|------|
| namespace | string | 'AmisCards' | 命名空间，避免冲突 |
| baseUrl | string | '/api' | API基础路径 |
| theme | string | 'antd' | Amis主题 |
| debug | boolean | false | 调试模式 |

### 核心方法

| 方法 | 参数 | 返回值 | 描述 |
|------|------|--------|------|
| renderPage | (containerId, pageConfig, options?) | Promise&lt;Instance&gt; | 渲染Amis页面 |
| destroyPage | (instanceId) | void | 销毁页面实例 |
| getDataService | () | DataService | 获取数据服务实例 |

### 卡片配置接口

#### 统计卡片 (StatCard)
```typescript
interface StatCardConfig {
    id: string;
    type: 'stat';
    title: string;
    subtitle?: string;
    size?: 'small' | 'medium' | 'large';
    style?: {
        theme?: 'primary' | 'success' | 'warning' | 'danger' | 'info';
    };
    data: {
        value: number | string;
        unit?: string;
        label?: string;
        trend?: {
            direction: 'up' | 'down' | 'stable';
            value: number;
            period: string;
        };
        api?: string;
    };
    autoRefresh?: boolean;
    refreshInterval?: number;
}
```

#### 图表卡片 (ChartCard)
```typescript
interface ChartCardConfig {
    id: string;
    type: 'chart';
    title: string;
    subtitle?: string;
    style?: {
        height?: number;
        theme?: string;
    };
    data: {
        chartType: 'line' | 'bar' | 'pie' | 'area' | 'scatter';
        api?: string;
        config?: EChartsOption;
    };
}
```

#### 信息卡片 (InfoCard)
```typescript
interface InfoCardConfig {
    id: string;
    type: 'info';
    title: string;
    subtitle?: string;
    style?: {
        theme?: string;
    };
    data: {
        content: string;
        api?: string;
        actions?: Array<{
            label: string;
            type: string;
            actionType: string;
            [key: string]: any;
        }>;
    };
}
```

#### 操作卡片 (ActionCard)
```typescript
interface ActionCardConfig {
    id: string;
    type: 'action';
    title: string;
    subtitle?: string;
    style?: {
        theme?: string;
    };
    data: {
        layout: 'grid' | 'list' | 'inline';
        actions: Array<{
            label: string;
            icon?: string;
            level?: string;
            actionType: string;
            [key: string]: any;
        }>;
    };
}
```

## 🔌 扩展开发

### 自定义卡片渲染器

```javascript
// 创建自定义渲染器
class CustomCardRenderer {
    async render(config) {
        // 自定义渲染逻辑
        return element;
    }
    
    async update(element, config) {
        // 自定义更新逻辑
    }
}

// 注册自定义渲染器
amisCards.registerRenderer('custom', new CustomCardRenderer());

// 使用自定义卡片
const customCardConfig = {
    type: 'custom',
    // 自定义配置
};
```

### 数据更新和事件

```javascript
// 监听卡片事件
amisCards.eventBus.on('card-rendered', (data) => {
    console.log('卡片渲染完成:', data);
});

// 手动更新数据
await amisCards.updateCard('card-id', { value: 999 });

// 批量刷新
await amisCards.refreshAll();
```

## 🌐 浏览器支持

- Chrome 80+
- Firefox 75+
- Safari 13+
- Edge 80+
- 移动端浏览器

## 📖 示例和演示

访问演示页面：
- [基础演示](/amis-cards/demo/index.html)
- [表格卡片演示](/amis-cards/demo/table-cards-demo.html)
- [图表演示](/amis-cards/demo/chart-demo.html)
- [表单演示](/amis-cards/demo/form-demo.html)


## 🤝 贡献指南

欢迎提交 Issue 和 Pull Request！参与贡献前请阅读[贡献指南](docs/contributing.md)。 

## 主要特性

- **统一架构**：基于 Amis Page 组件，与现有系统完美融合
- **多种卡片类型**：支持统计卡片、图表卡片、表格卡片、信息卡片
- **智能主题**：支持多种主题和深色模式
- **响应式设计**：完美适配各种屏幕尺寸
- **图标支持**：丰富的图标配置选项，支持 FontAwesome 和自定义图标
- **高性能**：优化的渲染机制和缓存策略
- **易于扩展**：模块化架构，支持自定义渲染器

## 快速开始

### 基本使用

```javascript
// 创建 AmisCards 实例
const cards = AmisCards.create({
    container: '#cards-container',
    theme: 'default'
});

// 渲染卡片
await cards.render([
    {
        id: 'user-stats',
        type: 'stat',
        title: '用户统计',
        data: {
            value: 1234,
            label: '总用户数',
            icon: 'users',
            iconColor: '#007bff'
        }
    }
]);
```

## 卡片类型

### 统计卡片 (stat)

统计卡片用于展示数值统计信息，支持趋势显示、进度条和图标。

#### 基本配置

```javascript
{
    id: 'basic-stat',
    type: 'stat',
    title: '基础统计',
    subtitle: '统计说明',
    theme: 'primary',
    data: {
        value: 1234,
        label: '统计标签',
        unit: '个',
        formatter: 'integer'
    }
}
```

#### 图标配置

统计卡片支持丰富的图标配置选项：

```javascript
{
    id: 'icon-stat',
    type: 'stat',
    title: '带图标统计',
    data: {
        value: 1234,
        label: '用户数',
        
        // 图标基本配置
        icon: 'users',                    // 图标名称（FontAwesome）
        iconColor: '#007bff',             // 图标颜色
        iconSize: 'lg',                   // 图标尺寸：xs, sm, md, lg, xl
        iconPosition: 'left',             // 图标位置：left, right, top, bottom
        
        // 图标样式配置
        iconBackground: 'rgba(0, 123, 255, 0.1)',  // 图标背景色
        iconBorder: true,                 // 是否显示边框
    }
}
```

#### 图标位置选项

- `left`：图标在左侧（默认）
- `right`：图标在右侧
- `top`：图标在上方，居中对齐
- `bottom`：图标在下方，居中对齐

#### 图标尺寸选项

- `xs`：24x24px，字体大小 12px
- `sm`：32x32px，字体大小 16px
- `md`：48x48px，字体大小 24px（默认）
- `lg`：64x64px，字体大小 32px
- `xl`：80x80px，字体大小 40px

#### 图标类型支持

1. **FontAwesome 图标**
   ```javascript
   icon: 'users'           // 简写形式
   icon: 'fa-users'        // 标准形式
   icon: 'fa fa-users'     // 完整形式
   ```

2. **URL 图标**
   ```javascript
   icon: 'https://example.com/icon.svg'    // HTTP URL
   icon: '//example.com/icon.png'          // 协议相对 URL
   icon: 'data:image/svg+xml;base64,...'   // Data URL
   ```

#### 趋势显示

```javascript
{
    data: {
        value: 1234,
        trend: {
            direction: 'up',      // up, down, stable
            value: 12.5,          // 趋势值
            period: '较昨日',     // 时间周期
            percentage: true      // 是否为百分比
        }
    }
}
```

#### 进度条显示

```javascript
{
    data: {
        value: 750,
        target: 1000,           // 目标值
        showProgress: true,     // 显示进度条
        description: '进度说明'
    }
}
```

### 图表卡片 (chart)

用于展示各种图表数据。

```javascript
{
    id: 'chart-example',
    type: 'chart',
    title: '销售趋势',
    chartType: 'line',
    series: [{
        name: '销售额',
        data: [120, 132, 101, 134, 90, 230, 210]
    }],
    xAxisData: ['周一', '周二', '周三', '周四', '周五', '周六', '周日']
}
```

### 表格卡片 (table)

用于展示表格数据。

```javascript
{
    id: 'table-example',
    type: 'table',
    title: '用户列表',
    columns: [
        { name: 'name', label: '姓名', type: 'text' },
        { name: 'email', label: '邮箱', type: 'text' },
        { name: 'status', label: '状态', type: 'status' }
    ],
    data: {
        items: [...],
        total: 100
    }
}
```

### 信息卡片 (info)

用于展示属性信息。

```javascript
{
    id: 'info-example',
    type: 'info',
    title: '系统信息',
    infoType: 'properties',
    properties: [
        { label: '版本', content: 'v2.0.0' },
        { label: '环境', content: 'Production' }
    ]
}
```

## 主题配置

支持多种内置主题：

- `default`：默认主题
- `primary`：主色调主题
- `success`：成功主题（绿色）
- `warning`：警告主题（橙色）
- `danger`：危险主题（红色）
- `info`：信息主题（蓝色）
- `dark`：深色主题

```javascript
// 创建实例时指定主题
const cards = AmisCards.create({
    theme: 'dark'
});

// 动态切换主题
await cards.setTheme('primary');
```

## API 参考

### AmisCards.create(options)

创建 AmisCards 实例。

**参数：**
- `options.container` - 容器选择器或元素
- `options.theme` - 主题名称
- `options.config` - 全局配置

**返回：** AmisCards 实例

### instance.render(cards)

渲染卡片数组。

**参数：**
- `cards` - 卡片配置数组

**返回：** Promise

### instance.setTheme(theme, rerender)

设置主题。

**参数：**
- `theme` - 主题名称
- `rerender` - 是否重新渲染

**返回：** Promise

## 样式定制

### CSS 变量

系统提供了丰富的 CSS 变量用于样式定制：

```css
:root {
    --amis-cards-primary: #007bff;
    --amis-cards-success: #28a745;
    --amis-cards-warning: #ffc107;
    --amis-cards-danger: #dc3545;
    --amis-cards-info: #17a2b8;
    
    --amis-cards-spacing-xs: 0.25rem;
    --amis-cards-spacing-sm: 0.5rem;
    --amis-cards-spacing-md: 1rem;
    --amis-cards-spacing-lg: 1.5rem;
    --amis-cards-spacing-xl: 3rem;
}
```

### 图标样式定制

```css
/* 自定义图标容器样式 */
.stat-icon-container {
    border-radius: 8px;
    transition: all 0.3s ease;
}

/* 自定义图标悬停效果 */
.stat-icon-container:hover {
    transform: scale(1.1);
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
}

/* 主题相关的图标样式 */
.amis-cards-theme-primary .stat-icon-with-bg {
    background: linear-gradient(135deg, rgba(0, 123, 255, 0.1), rgba(0, 123, 255, 0.2));
}
```

## 最佳实践

### 图标使用建议

1. **选择合适的图标尺寸**
   - 小卡片使用 `sm` 或 `md`
   - 大卡片使用 `lg` 或 `xl`
   - 移动端建议使用较小尺寸

2. **图标位置选择**
   - `left`：适合大多数情况
   - `top`：适合需要突出图标的场景
   - `right`：适合数值较长的情况

3. **颜色搭配**
   - 使用主题色系保持一致性
   - 图标颜色与卡片主题匹配
   - 背景色使用半透明效果

### 性能优化

1. **图标加载优化**
   - 优先使用 FontAwesome 图标
   - URL 图标使用 CDN 或本地资源
   - SVG 图标使用 Data URL 减少请求

2. **渲染优化**
   - 批量渲染多个卡片
   - 避免频繁的主题切换
   - 使用缓存减少重复计算

## 兼容性

- 现代浏览器（Chrome 60+, Firefox 55+, Safari 12+, Edge 79+）
- 移动端浏览器
- Amis 6.12.0+

## 更新日志

### v2.0.0
- 🎉 新增图标支持功能
- 🎨 支持多种图标位置和尺寸
- 🔧 优化图标样式和动画效果
- 📱 改进移动端图标显示
- 🌙 完善深色主题图标适配

### v1.0.0
- 🚀 初始版本发布
- 📊 支持统计、图表、表格、信息卡片
- 🎨 多主题支持
- 📱 响应式设计