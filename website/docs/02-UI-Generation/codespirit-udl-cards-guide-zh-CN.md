# CodeSpirit UDL Cards 卡片使用指南

## 概述

CodeSpirit UDL Cards 是根据CodeSpirit UDL Cards的设计基于Amis 框架构建的统一卡片系统，提供丰富的卡片类型和灵活的配置选项。本指南详细介绍各种卡片的使用方法、配置参数和实际应用场景。

## 支持的卡片类型

### 1. 统计卡片 (stat)
用于展示数值统计信息，支持趋势显示、进度条和图标。

![image-20250626181602780](/img/image-20250626181602780.png)

![image-20250626181804395](/img/image-20250626181804395.png)

### 2. 图表卡片 (chart)
用于展示各种类型的数据图表，如折线图、柱状图、饼图等。

![image-20250626181832749](/img/image-20250626181832749.png)

### 3. 表格卡片 (table)
用于展示表格数据，支持分页、搜索、排序等功能。

![image-20250626181907776](/img/image-20250626181907776.png)

### 4. 信息卡片 (info)
用于展示静态信息内容，支持富文本和自定义布局。

![image-20250626181936607](/img/image-20250626181936607.png)

### 5. 信息网格卡片 (info-grid)
用于以网格形式展示多个信息项，适用于监控大屏等场景。

![image-20250626182001966](/img/image-20250626182001966.png)

![image-20250626182025984](/img/image-20250626182025984.png)

## 监考大屏

![image-20250626182139861](/img/image-20250626182139861.png)

## 快速开始

### 基本引入

```html
<!DOCTYPE html>
<html lang="zh-CN">
<head>
    <!-- Amis CSS 资源 -->
    <link rel="stylesheet" href="/sdk/6.12.0/antd.css">
    <link rel="stylesheet" href="/sdk/6.12.0/helper.css">
    <link rel="stylesheet" href="/sdk/6.12.0/iconfont.css">
    
    <!-- AmisCards 样式 -->
    <link rel="stylesheet" href="/amis-cards/styles/amis-cards.css">
</head>
<body>
    <div id="cards-container"></div>
    
    <!-- JavaScript 依赖 -->
    <script src="/sdk/6.12.0/sdk.js"></script>
    <script src="/amis-cards/core/amis-cards-core.js"></script>
    <script src="/amis-cards/renderers/stat-renderer.js"></script>
    <!-- 其他渲染器... -->
</body>
</html>
```

### 初始化实例

```javascript
// 创建 AmisCards 实例
const cardsInstance = AmisCards.create({
    container: '#cards-container',
    theme: 'default'
});

// 渲染卡片
await cardsInstance.render([
    {
        id: 'sample-stat',
        type: 'stat',
        title: '示例统计',
        data: {
            value: 1234,
            label: '用户数',
            unit: '人'
        }
    }
]);
```

## 统计卡片 (stat)

### 基本配置

```javascript
{
    id: 'basic-stat',
    type: 'stat',
    title: '基础统计',
    subtitle: '统计说明',
    theme: 'primary',
    size: 'medium',
    data: {
        value: 1234,
        label: '统计标签',
        unit: '个',
        formatter: 'integer'
    }
}
```

### 图标配置

统计卡片支持丰富的图标配置选项：

```javascript
{
    id: 'icon-stat',
    type: 'stat',
    title: '带图标统计',
    data: {
        value: 12580,
        label: '注册用户',
        unit: '人',
        formatter: 'integer',
        
        // 图标基本配置
        icon: 'users',                    // 图标名称（FontAwesome）
        iconColor: '#007bff',             // 图标颜色
        iconSize: 'lg',                   // 图标尺寸：xs, sm, md, lg, xl
        iconPosition: 'left',             // 图标位置：left, right, top, bottom
        
        // 图标样式配置
        iconBackground: 'rgba(0, 123, 255, 0.1)',  // 图标背景色
        iconBorder: false                 // 是否显示边框
    }
}
```

### 趋势显示

```javascript
{
    id: 'trend-stat',
    type: 'stat',
    title: '用户增长',
    subtitle: '新用户注册趋势',
    theme: 'primary',
    data: {
        value: 2468,
        label: '新增用户',
        unit: '人',
        formatter: 'integer',
        trend: {
            direction: 'up',      // up | down | stable
            value: 12.5,          // 趋势值
            period: '较昨日',     // 时间周期
            percentage: true      // 是否为百分比
        },
        icon: 'user-plus',
        iconColor: '#007bff',
        iconSize: 'xl',
        iconPosition: 'top'
    }
}
```

### 进度条显示

```javascript
{
    id: 'progress-stat',
    type: 'stat',
    title: '销售目标',
    subtitle: '月度销售完成情况',
    theme: 'success',
    data: {
        value: 750000,
        label: '当前销售额',
        formatter: 'currency',
        target: 1000000,        // 目标值
        showProgress: true,     // 显示进度条
        description: '距离月度目标还需努力！',
        icon: 'chart-line',
        iconColor: '#28a745',
        iconSize: 'lg',
        iconPosition: 'right'
    }
}
```

### 配置参数

| 参数 | 类型 | 默认值 | 描述 |
|------|------|--------|------|
| id | string | - | 卡片唯一标识 |
| type | string | 'stat' | 卡片类型 |
| title | string | - | 卡片标题 |
| subtitle | string | - | 卡片副标题 |
| theme | string | 'default' | 主题：primary, success, warning, danger, info |
| size | string | 'medium' | 尺寸：small, medium, large |
| data.value | number/string | 0 | 统计值 |
| data.label | string | - | 数值标签 |
| data.unit | string | - | 数值单位 |
| data.formatter | string | null | 格式化器：integer, currency, percentage, fileSize |
| data.icon | string | - | 图标名称 |
| data.iconColor | string | - | 图标颜色 |
| data.iconSize | string | 'md' | 图标尺寸：xs, sm, md, lg, xl |
| data.iconPosition | string | 'left' | 图标位置：left, right, top, bottom |

## 图表卡片 (chart)

### 基本配置

```javascript
{
    id: 'line-chart',
    type: 'chart',
    title: '访问量趋势',
    subtitle: '最近7天数据',
    size: 'large',
    theme: 'info',
    chartType: 'line',
    height: 300,
    series: [
        {
            name: '访问量',
            data: [120, 132, 101, 134, 90, 230, 210]
        }
    ],
    xAxisData: ['周一', '周二', '周三', '周四', '周五', '周六', '周日']
}
```

### API 数据源

```javascript
{
    id: 'api-chart',
    type: 'chart',
    title: '销售数据图表',
    api: '/api/charts/sales-data',
    chartType: 'bar',
    height: 400
}
```

### 配置参数

| 参数 | 类型 | 默认值 | 描述 |
|------|------|--------|------|
| chartType | string | 'line' | 图表类型：line, bar, pie, area, scatter |
| height | number | 300 | 图表高度 |
| series | array | [] | 数据系列 |
| xAxisData | array | [] | X轴数据 |
| api | string | - | API数据源 |

## 表格卡片 (table)

### 基本配置

```javascript
{
    id: 'user-table',
    type: 'table',
    title: '用户列表',
    subtitle: '系统用户管理',
    size: 'large',
    columns: [
        { name: 'id', label: 'ID', width: 80 },
        { name: 'name', label: '姓名', sortable: true },
        { name: 'email', label: '邮箱', type: 'text' },
        { name: 'status', label: '状态', type: 'status', statusMap: {
            1: '<span class="label label-success">正常</span>',
            0: '<span class="label label-danger">禁用</span>'
        }},
        { name: 'createTime', label: '注册时间', type: 'datetime' }
    ],
    data: {
        items: [
            {
                id: 1,
                name: '张三',
                email: 'zhangsan@example.com',
                status: 1,
                createTime: '2024-01-15 10:30:00'
            }
            // ... 更多数据
        ],
        total: 100
    },
    showPager: true,
    perPage: 20
}
```

### API 数据源

```javascript
{
    id: 'api-table',
    type: 'table',
    title: '用户列表',
    api: '/api/users',
    columns: [
        { name: 'name', label: '姓名', sortable: true },
        { name: 'email', label: '邮箱', copyable: true },
        { name: 'status', label: '状态', type: 'status' }
    ],
    showSearch: true,
    searchFields: [
        { name: 'name', label: '姓名', type: 'input-text', placeholder: '请输入姓名' },
        { name: 'status', label: '状态', type: 'select', options: [
            { label: '全部', value: '' },
            { label: '正常', value: 1 },
            { label: '禁用', value: 0 }
        ]}
    ]
}
```

### 操作按钮

```javascript
{
    id: 'action-table',
    type: 'table',
    title: '用户管理',
    api: '/api/users',
    columns: [
        // ... 列配置
    ],
    rowActions: [
        { type: 'button', label: '编辑', level: 'link', actionType: 'dialog' },
        { type: 'button', label: '删除', level: 'link', className: 'text-danger' }
    ],
    tableToolbar: [
        { type: 'button', label: '新增用户', level: 'primary', icon: 'fa fa-plus' },
        { type: 'button', label: '导出数据', level: 'default', icon: 'fa fa-download' }
    ]
}
```

### 配置参数

| 参数 | 类型 | 默认值 | 描述 |
|------|------|--------|------|
| columns | array | [] | 表格列配置 |
| api | string | - | API数据源 |
| data | object | - | 静态数据 |
| showPager | boolean | true | 是否显示分页 |
| perPage | number | 20 | 每页显示条数 |
| showSearch | boolean | false | 是否显示搜索栏 |
| searchFields | array | [] | 搜索字段配置 |
| rowActions | array | [] | 行操作按钮 |
| tableToolbar | array | [] | 表格工具栏按钮 |

## 信息卡片 (info)

### 基本配置

```javascript
{
    id: 'system-info',
    type: 'info',
    title: '系统信息',
    subtitle: '当前系统状态',
    theme: 'info',
    infoType: 'properties',
    properties: [
        { label: '系统版本', content: 'CodeSpirit v2.0' },
        { label: '运行环境', content: '.NET 9.0' },
        { label: '启动时间', content: '2024-01-15 09:30:00' },
        { label: '运行时长', content: '15天 8小时' }
    ]
}
```

### HTML 内容

```javascript
{
    id: 'notice-info',
    type: 'info',
    title: '系统通知',
    subtitle: '重要信息公告',
    theme: 'warning',
    infoType: 'html',
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
```

### 配置参数

| 参数 | 类型 | 默认值 | 描述 |
|------|------|--------|------|
| infoType | string | 'text' | 信息类型：text, html, properties, list |
| content | string | - | 文本内容 |
| properties | array | [] | 属性列表 |
| actions | array | [] | 操作按钮 |

## 信息网格卡片 (info-grid)

### 基本配置

```javascript
{
    id: 'system-overview',
    type: 'info-grid',
    title: '系统概览信息',
    subtitle: '基础信息网格展示',
    theme: 'primary',
    items: [
        {
            label: '用户总数',
            value: '12,580',
            unit: '人',
            icon: 'users',
            iconColor: '#17a2b8'
        },
        {
            label: '活跃用户',
            value: '8,432',
            unit: '人',
            icon: 'user-check',
            iconColor: '#28a745'
        },
        {
            label: '今日访问',
            value: '15,678',
            unit: '次',
            icon: 'eye',
            iconColor: '#007bff'
        },
        {
            label: '系统状态',
            value: '正常运行',
            icon: 'check-circle',
            iconColor: '#28a745',
            highlight: false
        }
    ]
}
```

### 监控大屏样式

```javascript
{
    id: 'exam-monitor',
    type: 'info-grid',
    title: '考试监控信息',
    subtitle: '模拟考试监控大屏的顶部信息卡片',
    theme: 'info',
    grid: {
        columns: 'auto-fit',
        minItemWidth: '180px',
        gap: '1rem'
    },
    items: [
        {
            label: '考试编号',
            value: '193191665020536',
            icon: 'id-card',
            iconColor: '#9b59b6'
        },
        {
            label: '学校机构',
            value: 'CodeSpirit 教育',
            icon: 'university',
            iconColor: '#f39c12'
        },
        {
            label: '考试状态',
            value: '已结束',
            icon: 'stop-circle',
            iconColor: '#e74c3c',
            highlight: true
        },
        {
            label: '在线情况',
            value: '0/2',
            icon: 'users',
            iconColor: '#e67e22',
            highlight: true
        }
    ]
}
```

### 配置参数

| 参数 | 类型 | 默认值 | 描述 |
|------|------|--------|------|
| items | array | [] | 信息项列表 |
| grid.columns | string/number | 'auto-fit' | 网格列数 |
| grid.minItemWidth | string | '150px' | 最小项宽度 |
| grid.gap | string | '1rem' | 网格间隙 |

## 主题配置

### 支持的主题

- `default` - 默认蓝色主题 (#007bff)
- `primary` - 主要蓝色 (#007bff)
- `success` - 成功绿色 (#28a745)
- `warning` - 警告橙色 (#ffc107)
- `danger` - 危险红色 (#dc3545)
- `info` - 信息青色 (#17a2b8)
- `dark` - 暗色主题 (#343a40)
- `light` - 浅色主题 (#f8f9fa)

### 主题切换

```javascript
// 创建实例时指定主题
const cardsInstance = AmisCards.create({
    theme: 'dark'
});

// 动态切换主题
await cardsInstance.setTheme('primary', true);
```

## 响应式设计

### 断点设置

- **Extra Small (xs)**: < 576px (手机竖屏)
- **Small (sm)**: ≥ 576px (手机横屏)
- **Medium (md)**: ≥ 768px (平板)
- **Large (lg)**: ≥ 992px (桌面)
- **Extra Large (xl)**: ≥ 1200px (大屏幕)

### 响应式配置

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

## 混合布局示例

### 仪表板布局

```javascript
const dashboardConfig = {
    type: 'page',
    title: '智慧管理平台',
    body: [
        // 统计卡片区域
        {
            type: 'grid',
            className: 'stats-grid',
            columns: [
                {
                    body: {
                        id: 'total-users',
                        type: 'stat',
                        title: '总用户数',
                        theme: 'primary',
                        data: {
                            value: 12580,
                            unit: '人',
                            icon: 'users',
                            iconColor: '#007bff'
                        }
                    }
                },
                {
                    body: {
                        id: 'active-users',
                        type: 'stat',
                        title: '活跃用户',
                        theme: 'success',
                        data: {
                            value: 8432,
                            unit: '人',
                            icon: 'user-check',
                            iconColor: '#28a745'
                        }
                    }
                }
            ]
        },
        
        // 图表和信息区域
        {
            type: 'grid',
            columns: [
                {
                    md: 8,
                    body: {
                        id: 'trend-chart',
                        type: 'chart',
                        title: '访问趋势',
                        chartType: 'line',
                        height: 300,
                        api: '/api/charts/traffic'
                    }
                },
                {
                    md: 4,
                    body: {
                        id: 'system-info',
                        type: 'info',
                        title: '系统信息',
                        infoType: 'properties',
                        properties: [
                            { label: '版本', content: 'v2.0' },
                            { label: '环境', content: 'Production' }
                        ]
                    }
                }
            ]
        },
        
        // 数据表格区域
        {
            type: 'table',
            title: '用户列表',
            api: '/api/users',
            columns: [
                { name: 'name', label: '姓名' },
                { name: 'email', label: '邮箱' },
                { name: 'status', label: '状态' }
            ]
        }
    ]
};

await cardsInstance.renderPage('#container', dashboardConfig);
```

![image-20250626182303634](/img/image-20250626182303634.png)

## 最佳实践

### 1. 卡片选择

- **统计卡片**：适用于展示数值指标，如用户数、收入、完成率等
- **图表卡片**：适用于趋势分析、数据对比、分布展示
- **表格卡片**：适用于列表数据、管理界面、详细信息展示
- **信息卡片**：适用于公告通知、系统信息、帮助说明
- **信息网格**：适用于监控大屏、概览信息、多项数据展示

### 2. 性能优化

- 使用API数据源时合理设置缓存
- 避免过度使用动画效果
- 适当的数据分页和虚拟滚动
- 图表数据点数量控制在合理范围

### 3. 用户体验

- 保持卡片布局的一致性
- 合理使用颜色和主题
- 提供适当的加载状态提示
- 确保移动端的可用性

### 4. 开发建议

- 使用配置化方式而非硬编码
- 保持组件的独立性和可复用性
- 统一错误处理和异常显示
- 完善的文档和示例

## 常见问题

### Q: 如何自定义卡片样式？
A: 可以通过CSS变量和主题配置来自定义样式，详见样式定制章节。

### Q: 支持哪些数据格式？
A: 支持JSON格式的静态数据和RESTful API返回的数据。

### Q: 如何处理大量数据？
A: 建议使用分页、虚拟滚动等技术，避免一次性渲染大量数据。

### Q: 移动端适配如何实现？
A: 系统采用响应式设计，会自动适配不同屏幕尺寸。

### Q: 如何扩展新的卡片类型？
A: 可以通过继承BaseRenderer类来实现自定义卡片渲染器。

## 更新日志

### v2.0.0
- 🎉 新增图标支持功能
- 🎨 支持多种图标位置和尺寸
- 🔧 优化图标样式和动画效果
- 📱 改进移动端图标显示
- 🌙 完善深色主题图标适配
- 📊 新增信息网格卡片类型
- 🔄 完善API数据源支持
- 🎯 优化表格卡片功能