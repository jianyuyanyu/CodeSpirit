# 统计卡片 (stat) 使用指南

## 概述

统计卡片是 CodeSpirit Amis Cards 中最常用的卡片类型，专门用于展示数值统计信息。支持数值格式化、趋势显示、进度条、图标配置等丰富功能，适用于仪表板、监控大屏、数据概览等场景。

## 基本用法

### 最简单的统计卡片

```javascript
{
    id: 'basic-stat',
    type: 'stat',
    title: '用户总数',
    data: {
        value: 1234,
        label: '注册用户',
        unit: '人'
    }
}
```

### 带主题的统计卡片

```javascript
{
    id: 'themed-stat',
    type: 'stat',
    title: '今日收入',
    subtitle: '实时收入统计',
    theme: 'success',
    size: 'large',
    data: {
        value: 98765.43,
        label: '收入金额',
        formatter: 'currency'
    }
}
```

## 数值格式化

### 支持的格式化器

| 格式化器 | 说明 | 示例输入 | 示例输出 |
|----------|------|----------|----------|
| `integer` | 整数格式 | 1234 | 1,234 |
| `currency` | 货币格式 | 1234.56 | ¥1,234.56 |
| `percentage` | 百分比格式 | 0.875 | 87.5% |
| `fileSize` | 文件大小格式 | 2147483648 | 2.00 GB |

### 格式化示例

```javascript
// 整数格式
{
    id: 'user-count',
    type: 'stat',
    title: '用户总数',
    data: {
        value: 12580,
        label: '注册用户',
        unit: '人',
        formatter: 'integer'
    }
}

// 货币格式
{
    id: 'revenue',
    type: 'stat',
    title: '今日收入',
    data: {
        value: 98765.43,
        label: '收入金额',
        formatter: 'currency'
    }
}

// 百分比格式
{
    id: 'completion-rate',
    type: 'stat',
    title: '完成率',
    data: {
        value: 0.875,
        label: '任务完成',
        formatter: 'percentage'
    }
}

// 文件大小格式
{
    id: 'storage-usage',
    type: 'stat',
    title: '存储使用',
    data: {
        value: 2684354560, // 2.5GB
        label: '已使用存储',
        formatter: 'fileSize'
    }
}
```

## 图标配置

### 基本图标配置

```javascript
{
    id: 'icon-stat',
    type: 'stat',
    title: '用户统计',
    data: {
        value: 12580,
        label: '注册用户',
        unit: '人',
        formatter: 'integer',
        
        // 基本图标配置
        icon: 'users',
        iconColor: '#007bff',
        iconSize: 'lg',
        iconPosition: 'left'
    }
}
```

### 图标位置选项

```javascript
// 左侧图标（默认）
{
    data: {
        icon: 'users',
        iconPosition: 'left'
    }
}

// 右侧图标
{
    data: {
        icon: 'dollar-sign',
        iconPosition: 'right'
    }
}

// 顶部图标
{
    data: {
        icon: 'chart-line',
        iconPosition: 'top'
    }
}

// 底部图标
{
    data: {
        icon: 'server',
        iconPosition: 'bottom'
    }
}
```

### 图标尺寸选项

```javascript
// 超小图标
{
    data: {
        icon: 'star',
        iconSize: 'xs'  // 24x24px
    }
}

// 小图标
{
    data: {
        icon: 'star',
        iconSize: 'sm'  // 32x32px
    }
}

// 中等图标（默认）
{
    data: {
        icon: 'star',
        iconSize: 'md'  // 48x48px
    }
}

// 大图标
{
    data: {
        icon: 'star',
        iconSize: 'lg'  // 64x64px
    }
}

// 超大图标
{
    data: {
        icon: 'star',
        iconSize: 'xl'  // 80x80px
    }
}
```

### 图标样式配置

```javascript
{
    data: {
        icon: 'shield-alt',
        iconColor: '#007bff',
        iconSize: 'lg',
        iconPosition: 'left',
        
        // 图标背景配置
        iconBackground: 'rgba(0, 123, 255, 0.1)',
        
        // 图标边框配置
        iconBorder: true
    }
}
```

### 图标类型支持

```javascript
// FontAwesome 图标（推荐）
{
    data: {
        icon: 'users'        // 简写形式
        // icon: 'fa-users'  // 标准形式
        // icon: 'fa fa-users' // 完整形式
    }
}

// URL 图标
{
    data: {
        icon: 'https://cdn.example.com/icon.svg'
    }
}

// Data URL 图标
{
    data: {
        icon: 'data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iMjQiIGhlaWdodD0iMjQi...'
    }
}
```

## 趋势显示

### 基本趋势配置

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
        
        // 趋势配置
        trend: {
            direction: 'up',      // 趋势方向：up, down, stable
            value: 12.5,          // 趋势值
            period: '较昨日',     // 时间周期
            percentage: true      // 是否为百分比
        }
    }
}
```

### 趋势方向类型

```javascript
// 上升趋势
{
    trend: {
        direction: 'up',
        value: 12.5,
        period: '较昨日',
        percentage: true
    }
}

// 下降趋势
{
    trend: {
        direction: 'down',
        value: 8.3,
        period: '较上周',
        percentage: true
    }
}

// 稳定趋势
{
    trend: {
        direction: 'stable',
        value: 0.1,
        period: '较昨日',
        percentage: true
    }
}
```

## 进度条显示

### 基本进度条配置

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
        
        // 进度条配置
        target: 1000000,        // 目标值
        showProgress: true,     // 显示进度条
        description: '距离月度目标还需努力！'
    }
}
```

### 进度条样式

```javascript
{
    data: {
        value: 8589934592,      // 8GB
        label: '已使用内存',
        formatter: 'fileSize',
        target: 17179869184,    // 16GB
        showProgress: true,
        description: '内存使用率 50%'
    }
}
```

## 实际应用示例

### 基础统计演示

基于演示代码中的基础统计示例：

```javascript
const basicStatsCards = [
    {
        id: 'users-total',
        type: 'stat',
        title: '总用户数',
        subtitle: '系统注册用户统计',
        theme: 'primary',
        data: {
            value: 12580,
            label: '注册用户',
            unit: '人',
            formatter: 'integer',
            icon: 'users',
            iconColor: '#007bff',
            iconSize: 'lg',
            iconPosition: 'left',
            iconBackground: 'rgba(0, 123, 255, 0.1)',
            iconBorder: false
        }
    },
    {
        id: 'revenue-today',
        type: 'stat',
        title: '今日收入',
        subtitle: '实时收入统计',
        theme: 'success',
        data: {
            value: 98765.43,
            label: '收入金额',
            formatter: 'currency',
            icon: 'dollar-sign',
            iconColor: '#28a745',
            iconSize: 'lg',
            iconPosition: 'left',
            iconBackground: 'rgba(40, 167, 69, 0.1)',
            iconBorder: false
        }
    },
    {
        id: 'completion-rate',
        type: 'stat',
        title: '任务完成率',
        subtitle: '今日任务执行情况',
        theme: 'info',
        data: {
            value: 87.5,
            label: '完成率',
            formatter: 'percentage',
            icon: 'check-circle',
            iconColor: '#17a2b8',
            iconSize: 'lg',
            iconPosition: 'left',
            iconBackground: 'rgba(23, 162, 184, 0.1)',
            iconBorder: false
        }
    },
    {
        id: 'active-sessions',
        type: 'stat',
        title: '活跃会话',
        subtitle: '当前在线用户数',
        theme: 'warning',
        data: {
            value: 1337,
            label: '在线用户',
            unit: '个',
            formatter: 'integer',
            icon: 'wifi',
            iconColor: '#ffc107',
            iconSize: 'lg',
            iconPosition: 'left',
            iconBackground: 'rgba(255, 193, 7, 0.1)',
            iconBorder: false
        }
    }
];
```

### 高级统计演示

基于演示代码中的高级统计示例：

```javascript
const advancedStatsCards = [
    {
        id: 'user-growth',
        type: 'stat',
        title: '用户增长',
        subtitle: '新用户注册趋势',
        theme: 'primary',
        size: 'large',
        data: {
            value: 2468,
            label: '新增用户',
            unit: '人',
            formatter: 'integer',
            trend: {
                direction: 'up',
                value: 12.5,
                period: '较昨日',
                percentage: true
            },
            icon: 'user-plus',
            iconColor: '#007bff',
            iconSize: 'xl',
            iconPosition: 'top',
            iconBackground: 'rgba(0, 123, 255, 0.1)',
            iconBorder: true
        }
    },
    {
        id: 'sales-target',
        type: 'stat',
        title: '销售目标',
        subtitle: '月度销售完成情况',
        theme: 'success',
        size: 'large',
        data: {
            value: 750000,
            label: '当前销售额',
            formatter: 'currency',
            target: 1000000,
            showProgress: true,
            description: '距离月度目标还需努力！',
            icon: 'chart-line',
            iconColor: '#28a745',
            iconSize: 'lg',
            iconPosition: 'right',
            iconBackground: 'rgba(40, 167, 69, 0.1)',
            iconBorder: false
        }
    },
    {
        id: 'error-rate',
        type: 'stat',
        title: '系统错误率',
        subtitle: '24小时错误统计',
        theme: 'danger',
        data: {
            value: 0.05,
            label: '错误率',
            formatter: 'percentage',
            trend: {
                direction: 'down',
                value: 0.02,
                period: '较昨日',
                percentage: true
            },
            icon: 'exclamation-triangle',
            iconColor: '#dc3545',
            iconSize: 'md',
            iconPosition: 'left',
            iconBackground: 'rgba(220, 53, 69, 0.1)',
            iconBorder: false
        }
    },
    {
        id: 'storage-usage',
        type: 'stat',
        title: '存储使用量',
        subtitle: '系统存储空间统计',
        theme: 'info',
        data: {
            value: 2684354560, // 2.5GB
            label: '已使用',
            formatter: 'fileSize',
            target: 5368709120, // 5GB
            showProgress: true,
            icon: 'hdd',
            iconColor: '#17a2b8',
            iconSize: 'lg',
            iconPosition: 'bottom',
            iconBackground: 'rgba(23, 162, 184, 0.1)',
            iconBorder: true
        }
    }
];
```

## 配置参数参考

### 基本配置

| 参数 | 类型 | 默认值 | 描述 |
|------|------|--------|------|
| id | string | - | 卡片唯一标识（必填） |
| type | string | 'stat' | 卡片类型（必填） |
| title | string | - | 卡片标题 |
| subtitle | string | - | 卡片副标题 |
| theme | string | 'default' | 主题：default, primary, success, warning, danger, info |
| size | string | 'medium' | 尺寸：small, medium, large |

### 数据配置 (data)

| 参数 | 类型 | 默认值 | 描述 |
|------|------|--------|------|
| value | number/string | 0 | 统计值（必填） |
| label | string | - | 数值标签 |
| unit | string | - | 数值单位 |
| prefix | string | - | 数值前缀 |
| suffix | string | - | 数值后缀 |
| formatter | string | null | 格式化器：integer, currency, percentage, fileSize |
| description | string | - | 描述信息 |

### 图标配置 (data)

| 参数 | 类型 | 默认值 | 描述 |
|------|------|--------|------|
| icon | string | - | 图标名称或URL |
| iconColor | string | - | 图标颜色 |
| iconSize | string | 'md' | 图标尺寸：xs, sm, md, lg, xl |
| iconPosition | string | 'left' | 图标位置：left, right, top, bottom |
| iconBackground | string | - | 图标背景色 |
| iconBorder | boolean | false | 是否显示图标边框 |

### 趋势配置 (data.trend)

| 参数 | 类型 | 默认值 | 描述 |
|------|------|--------|------|
| direction | string | - | 趋势方向：up, down, stable |
| value | number | - | 趋势值 |
| period | string | - | 时间周期描述 |
| percentage | boolean | false | 是否为百分比格式 |

### 进度条配置 (data)

| 参数 | 类型 | 默认值 | 描述 |
|------|------|--------|------|
| target | number | - | 目标值 |
| showProgress | boolean | false | 是否显示进度条 |

## 样式定制

### CSS 变量

```css
:root {
    /* 统计卡片基础变量 */
    --stat-card-padding: 1.5rem;
    --stat-card-border-radius: 8px;
    --stat-card-background: #ffffff;
    --stat-card-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
    
    /* 统计值样式 */
    --stat-value-font-size: 2.5rem;
    --stat-value-font-weight: 700;
    --stat-value-color: #333;
    
    /* 标签样式 */
    --stat-label-font-size: 0.9rem;
    --stat-label-color: #666;
    
    /* 趋势样式 */
    --stat-trend-up-color: #28a745;
    --stat-trend-down-color: #dc3545;
    --stat-trend-stable-color: #6c757d;
    
    /* 进度条样式 */
    --stat-progress-height: 4px;
    --stat-progress-background: #e9ecef;
    --stat-progress-border-radius: 2px;
}
```

### 自定义样式示例

```css
/* 自定义统计卡片样式 */
.custom-stat-card {
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    color: white;
    border-radius: 12px;
}

.custom-stat-card .stat-value {
    color: white;
    text-shadow: 0 1px 2px rgba(0, 0, 0, 0.1);
}

.custom-stat-card .stat-label {
    color: rgba(255, 255, 255, 0.8);
}

/* 响应式样式 */
@media (max-width: 768px) {
    .stat-card {
        padding: 1rem;
    }
    
    .stat-value {
        font-size: 2rem;
    }
}
```

## 常见用例

### 1. 仪表板统计

```javascript
// 用于管理仪表板的关键指标展示
const dashboardStats = [
    {
        id: 'total-users',
        type: 'stat',
        title: '总用户数',
        theme: 'primary',
        data: {
            value: 15420,
            unit: '人',
            formatter: 'integer',
            icon: 'users'
        }
    }
];
```

### 2. 实时监控

```javascript
// 用于系统监控的实时数据展示
const monitoringStats = [
    {
        id: 'cpu-usage',
        type: 'stat',
        title: 'CPU 使用率',
        theme: 'warning',
        data: {
            value: 65.8,
            formatter: 'percentage',
            trend: { direction: 'up', value: 5.2 },
            icon: 'microchip'
        }
    }
];
```

### 3. 业务指标

```javascript
// 用于业务数据的关键指标展示
const businessStats = [
    {
        id: 'monthly-revenue',
        type: 'stat',
        title: '月度收入',
        theme: 'success',
        data: {
            value: 1250000,
            formatter: 'currency',
            target: 1500000,
            showProgress: true,
            icon: 'chart-line'
        }
    }
];
```

## 最佳实践

### 1. 数值选择
- 选择有意义的统计数值
- 避免过于复杂的计算
- 确保数据的实时性和准确性

### 2. 视觉设计
- 合理使用颜色和主题
- 保持图标风格的一致性
- 适当的间距和布局

### 3. 交互体验
- 提供适当的加载状态
- 考虑数据更新的频率
- 支持点击查看详情

### 4. 响应式适配
- 在不同设备上测试显示效果
- 调整字体大小和图标尺寸
- 确保触控操作的友好性

## 故障排除

### 常见问题

1. **图标不显示**
   - 检查图标名称是否正确
   - 确认 FontAwesome 库已正确加载
   - 验证图标 URL 是否可访问

2. **数值格式化异常**
   - 检查 formatter 参数是否正确
   - 确认数值类型是否匹配
   - 验证数值范围是否合理

3. **趋势显示问题**
   - 检查 trend 配置是否完整
   - 确认 direction 值是否正确
   - 验证 trend.value 是否为有效数值

4. **进度条不显示**
   - 检查 showProgress 是否设置为 true
   - 确认 target 值是否已设置
   - 验证 value 和 target 的数值关系

### 调试建议

1. 使用浏览器开发者工具检查控制台错误
2. 检查卡片配置的 JSON 格式是否正确
3. 验证必填参数是否都已提供
4. 确认 CSS 样式是否正确加载

## 参考资源

- [FontAwesome 图标库](https://fontawesome.com/icons)
- [图标支持文档](./icon-support.md)
- [主题配置指南](../configs/theme-configs.js)
- [演示页面](../demo/index.html) 