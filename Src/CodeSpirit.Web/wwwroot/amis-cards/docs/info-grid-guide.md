# InfoGrid 信息网格卡片使用指南

## 概述

InfoGrid 信息网格卡片是 CodeSpirit Amis Cards V2.0 中专门用于展示网格化信息项的卡片类型。它特别适用于监控大屏、系统概览、状态展示等需要以网格形式展示多个信息项的场景。每个信息项包含图标、标签、数值和可选的描述信息。

### 主要特性

- **灵活的网格布局**：支持自适应列数、固定列数和自定义CSS网格
- **丰富的图标支持**：支持FontAwesome图标，可配置颜色、尺寸和样式
- **响应式设计**：自动适配不同屏幕尺寸，优化移动端显示
- **数据绑定**：支持静态数据和Amis表达式动态数据
- **主题支持**：完整的主题系统，支持深色模式
- **高度可定制**：支持CSS变量和自定义样式

## 基本用法

### 最简配置

```javascript
{
    id: 'basic-info-grid',
    type: 'info-grid',
    title: '系统概览',
    items: [
        {
            label: '用户总数',
            value: '12,580',
            unit: '人',
            icon: 'users'
        },
        {
            label: '活跃用户',
            value: '8,432',
            unit: '人',
            icon: 'user-check'
        }
    ]
}
```

### 带主题的配置

```javascript
{
    id: 'themed-info-grid',
    type: 'info-grid',
    title: '监控状态',
    subtitle: '实时系统状态信息',
    theme: 'primary',
    items: [
        {
            label: 'CPU使用率',
            value: '68.5%',
            icon: 'microchip',
            iconColor: '#e67e22',
            highlight: true
        },
        {
            label: '内存使用',
            value: '4.2GB',
            unit: ' / 8GB',
            icon: 'memory',
            iconColor: '#3498db'
        }
    ]
}
```

## 详细配置

### 主要配置参数

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `id` | string | - | 卡片唯一标识符 |
| `type` | string | - | 固定值：`info-grid` |
| `title` | string | - | 卡片标题 |
| `subtitle` | string | - | 卡片副标题 |
| `theme` | string | `default` | 主题：default/primary/success/warning/danger/info/dark |
| `items` | array | `[]` | 信息项配置列表 |
| `grid` | object | - | 网格布局配置 |

### 网格配置 (grid)

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `columns` | string\|number | `'auto-fit'` | 列数配置 |
| `gap` | string | `'1.25rem'` | 网格间距 |
| `minItemWidth` | string | `'220px'` | 最小项目宽度（auto-fit模式） |
| `itemPadding` | string | `'1.5rem'` | 项目内边距 |
| `showIcons` | boolean | `true` | 是否显示图标 |
| `iconPosition` | string | `'left'` | 图标位置：left/right/top/bottom |
| `iconSize` | string | `'lg'` | 图标尺寸：xs/sm/md/lg/xl |

#### 列数配置选项

1. **自适应布局**（推荐）
```javascript
grid: {
    columns: 'auto-fit',
    minItemWidth: '200px'  // 每个项目最小宽度
}
```

2. **固定列数**
```javascript
grid: {
    columns: 4  // 固定4列
}
```

3. **自定义CSS网格**
```javascript
grid: {
    columns: 'repeat(auto-fit, minmax(150px, 1fr))'
}
```

### 信息项配置 (items[])

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `label` | string | - | 标签文本（必需） |
| `value` | string | - | 数值，支持Amis表达式（必需） |
| `unit` | string | - | 单位文本 |
| `icon` | string | - | FontAwesome图标名（不含fa-前缀） |
| `iconColor` | string | `'#3498db'` | 图标颜色 |
| `iconBackground` | string | - | 图标背景色 |
| `iconBorder` | boolean | `false` | 是否显示图标边框 |
| `highlight` | boolean | `false` | 是否高亮显示数值 |
| `description` | string | - | 描述信息 |
| `valueColor` | string | `'#2c3e50'` | 数值颜色 |
| `labelColor` | string | `'#7f8c8d'` | 标签颜色 |

## 使用场景

### 1. 考试监控信息头

```javascript
{
    id: 'exam-monitor-header',
    type: 'info-grid',
    title: '考试监控信息',
    subtitle: '实时考试状态',
    theme: 'info',
    grid: {
        columns: 'auto-fit',
        minItemWidth: '180px',
        gap: '1.25rem'
    },
    items: [
        {
            label: '考试编号',
            value: '${examId}',
            icon: 'id-card',
            iconColor: '#9b59b6'
        },
        {
            label: '学校机构',
            value: '${tenantName}',
            icon: 'university',
            iconColor: '#f39c12'
        },
        {
            label: '考试状态',
            value: '${status}',
            icon: 'stop-circle',
            iconColor: '#e74c3c',
            highlight: true
        },
        {
            label: '在线情况',
            value: '${onlineCount}/${totalParticipants}',
            icon: 'users',
            iconColor: '#e67e22',
            highlight: true
        },
        {
            label: '开始时间',
            value: '${startTime}',
            icon: 'play-circle',
            iconColor: '#27ae60'
        },
        {
            label: '结束时间',
            value: '${endTime}',
            icon: 'stop-circle',
            iconColor: '#e74c3c'
        },
        {
            label: '考试时长',
            value: '${duration}分钟',
            icon: 'hourglass-half',
            iconColor: '#f1c40f'
        },
        {
            label: '最近更新',
            value: '${lastUpdate}',
            icon: 'sync-alt',
            iconColor: '#16a085'
        }
    ]
}
```

### 2. 系统状态监控

```javascript
{
    id: 'system-status',
    type: 'info-grid',
    title: '系统状态监控',
    theme: 'warning',
    grid: {
        columns: 3,
        gap: '2rem'
    },
    items: [
        {
            label: 'CPU使用率',
            value: '${cpuUsage}%',
            icon: 'microchip',
            iconColor: '${cpuUsage > 80 ? "#e74c3c" : "#27ae60"}',
            highlight: '${cpuUsage > 80}',
            description: '${cpuStatus}'
        },
        {
            label: '内存使用',
            value: '${memoryUsed}',
            unit: ' / ${memoryTotal}',
            icon: 'memory',
            iconColor: '${memoryPercent > 85 ? "#e74c3c" : "#3498db"}',
            description: '使用率 ${memoryPercent}%'
        },
        {
            label: '磁盘空间',
            value: '${diskUsed}',
            unit: ' / ${diskTotal}',
            icon: 'hdd',
            iconColor: '${diskPercent > 90 ? "#e74c3c" : "#9b59b6"}',
            description: '剩余 ${diskFree}'
        }
    ]
}
```

### 3. 数据统计概览

```javascript
{
    id: 'data-overview',
    type: 'info-grid',
    title: '数据统计概览',
    subtitle: '今日实时数据',
    theme: 'success',
    grid: {
        columns: 'auto-fit',
        minItemWidth: '240px',
        gap: '1.5rem'
    },
    items: [
        {
            label: '新增用户',
            value: '${todayNewUsers}',
            unit: '人',
            icon: 'user-plus',
            iconColor: '#28a745',
            iconBackground: 'rgba(40, 167, 69, 0.1)',
            description: '较昨日 +${userGrowth}%'
        },
        {
            label: '活跃用户',
            value: '${activeUsers}',
            unit: '人',
            icon: 'users',
            iconColor: '#17a2b8',
            iconBackground: 'rgba(23, 162, 184, 0.1)',
            description: '活跃率 ${activeRate}%'
        },
        {
            label: '订单数量',
            value: '${todayOrders}',
            unit: '单',
            icon: 'shopping-cart',
            iconColor: '#fd7e14',
            iconBackground: 'rgba(253, 126, 20, 0.1)',
            description: '成交额 ¥${orderAmount}'
        },
        {
            label: '系统负载',
            value: '${systemLoad}',
            icon: 'server',
            iconColor: '${systemLoad > 0.8 ? "#dc3545" : "#6f42c1"}',
            iconBackground: '${systemLoad > 0.8 ? "rgba(220, 53, 69, 0.1)" : "rgba(111, 66, 193, 0.1)"}',
            highlight: '${systemLoad > 0.8}',
            description: '${systemLoadStatus}'
        }
    ]
}
```

### 4. 移动端优化配置

```javascript
{
    id: 'mobile-info-grid',
    type: 'info-grid',
    title: '移动端展示',
    theme: 'primary',
    grid: {
        columns: 'auto-fit',
        minItemWidth: '150px',  // 移动端较小宽度
        gap: '1rem'
    },
    items: [
        {
            label: '消息',
            value: '${messageCount}',
            icon: 'envelope',
            iconColor: '#007bff'
        },
        {
            label: '通知',
            value: '${notificationCount}',
            icon: 'bell',
            iconColor: '#ffc107'
        },
        {
            label: '任务',
            value: '${taskCount}',
            icon: 'tasks',
            iconColor: '#28a745'
        }
    ]
}
```

## 图标配置

### 图标类型支持

1. **FontAwesome图标**（推荐）
```javascript
{
    icon: 'users',          // 简写形式
    icon: 'fa-users',       // 标准形式
    icon: 'fa fa-users'     // 完整形式
}
```

2. **图标尺寸**
```javascript
grid: {
    iconSize: 'xs'    // 24x24px, 字体12px
    iconSize: 'sm'    // 32x32px, 字体16px
    iconSize: 'md'    // 48x48px, 字体24px (默认)
    iconSize: 'lg'    // 64x64px, 字体32px
    iconSize: 'xl'    // 80x80px, 字体40px
}
```

3. **图标样式**
```javascript
{
    icon: 'users',
    iconColor: '#3498db',                           // 图标颜色
    iconBackground: 'rgba(52, 152, 219, 0.1)',     // 背景色
    iconBorder: true                                // 显示边框
}
```

### 图标位置配置

```javascript
grid: {
    iconPosition: 'left'    // 图标在左侧（默认）
    iconPosition: 'right'   // 图标在右侧
    iconPosition: 'top'     // 图标在上方
    iconPosition: 'bottom'  // 图标在下方
}
```

## 数据绑定

### 静态数据

```javascript
{
    label: '用户总数',
    value: '12,580',
    unit: '人'
}
```

### 动态数据（Amis表达式）

```javascript
{
    label: '在线用户',
    value: '${onlineUsers}',
    unit: '人',
    description: '在线率: ${ROUND(onlineUsers/totalUsers*100)}%'
}
```

### 条件样式

```javascript
{
    label: '系统状态',
    value: '${status}',
    icon: '${status === "正常" ? "check-circle" : "exclamation-triangle"}',
    iconColor: '${status === "正常" ? "#28a745" : "#dc3545"}',
    highlight: '${status !== "正常"}'
}
```

### 数值格式化

```javascript
{
    label: '内存使用',
    value: '${ROUND(memoryUsed/1024/1024/1024, 2)}GB',
    description: '使用率 ${ROUND(memoryUsed/memoryTotal*100)}%'
}
```

## 响应式设计

InfoGrid卡片自动适配不同屏幕尺寸：

### 断点配置

- **桌面端** (≥1200px)：使用配置的列数或自适应布局
- **平板端** (768px-1199px)：自动调整最小宽度
- **移动端** (<768px)：优化为单列或双列显示

### 响应式网格配置

```javascript
// 推荐的响应式配置
grid: {
    columns: 'auto-fit',
    minItemWidth: '200px',    // 桌面端最小宽度
    gap: '1.25rem'
}

// 移动端会自动调整为：
// minItemWidth: '150px'
// gap: '1rem'
```

### 移动端优化

```javascript
{
    id: 'mobile-optimized',
    type: 'info-grid',
    title: '移动端优化',
    grid: {
        columns: 'auto-fit',
        minItemWidth: '140px',  // 更小的最小宽度
        gap: '0.75rem'          // 更紧凑的间距
    },
    items: [
        {
            label: '简短标签',
            value: '${value}',
            icon: 'icon-name'
            // 移动端建议使用较短的标签和描述
        }
    ]
}
```

## 样式定制

### CSS类名结构

```html
<div class="amis-cards-info-grid">
    <div class="amis-cards-info-grid-container">
        <div class="exam-monitor-info info-grid-dynamic">
            <div class="exam-monitor-info-item">
                <i class="fa fa-icon"></i>
                <div>
                    <div class="info-label">标签</div>
                    <div class="info-value">数值</div>
                    <div class="info-description">描述</div>
                </div>
            </div>
        </div>
    </div>
</div>
```

### 主要CSS类名

- `.amis-cards-info-grid` - 信息网格卡片根容器
- `.amis-cards-info-grid-container` - 内容容器
- `.exam-monitor-info` - 网格容器
- `.exam-monitor-info-item` - 单个信息项
- `.info-label` - 标签样式
- `.info-value` - 数值样式
- `.info-highlight` - 高亮数值样式
- `.info-description` - 描述文本样式

### 自定义样式示例

```css
/* 自定义网格间距 */
.my-info-grid .exam-monitor-info {
    gap: 2rem;
}

/* 自定义标签样式 */
.my-info-grid .info-label {
    color: #2c3e50;
    font-weight: 700;
    font-size: 0.875rem;
    text-transform: uppercase;
    letter-spacing: 0.5px;
}

/* 自定义数值样式 */
.my-info-grid .info-value {
    color: #34495e;
    font-weight: 600;
    font-size: 1.25rem;
    margin: 0.25rem 0;
}

/* 自定义高亮样式 */
.my-info-grid .info-highlight {
    color: #e74c3c;
    font-size: 1.4rem;
    font-weight: 700;
    text-shadow: 0 1px 2px rgba(0,0,0,0.1);
}

/* 自定义图标样式 */
.my-info-grid .exam-monitor-info-item i {
    width: 3rem;
    height: 3rem;
    line-height: 3rem;
    text-align: center;
    border-radius: 50%;
    margin-right: 1rem;
    box-shadow: 0 2px 8px rgba(0,0,0,0.1);
}

/* 自定义描述样式 */
.my-info-grid .info-description {
    color: #7f8c8d;
    font-size: 0.8rem;
    font-style: italic;
    margin-top: 0.25rem;
}
```

### CSS变量定制

InfoGrid支持CSS变量进行动态定制：

```css
.amis-cards-info-grid {
    --info-grid-min-width: 220px;
    --info-grid-gap: 1.5rem;
    --info-grid-columns: repeat(auto-fit, minmax(var(--info-grid-min-width), 1fr));
}
```

## 主题支持

### 内置主题

- `default` - 默认主题（蓝色调）
- `primary` - 主要主题（蓝色）
- `success` - 成功主题（绿色）
- `warning` - 警告主题（橙色）
- `danger` - 危险主题（红色）
- `info` - 信息主题（青色）
- `dark` - 深色主题

### 主题使用

```javascript
{
    id: 'themed-grid',
    type: 'info-grid',
    theme: 'success',  // 使用成功主题
    title: '系统状态',
    items: [...]
}
```

### 深色主题适配

深色主题会自动调整：
- 背景色和边框色
- 文字颜色对比度
- 图标颜色亮度
- 悬停效果

## 性能优化

### 1. 合理的网格配置

```javascript
// 推荐：使用合适的最小宽度
grid: {
    columns: 'auto-fit',
    minItemWidth: '200px'  // 避免过小导致文字换行
}

// 避免：过多的固定列数
grid: {
    columns: 8  // 可能导致项目过窄
}
```

### 2. 图标优化

```javascript
// 推荐：使用FontAwesome图标
{
    icon: 'users',
    iconColor: '#3498db'
}

// 避免：复杂的自定义图标
{
    icon: 'data:image/svg+xml;base64,very-long-svg-data...'
}
```

### 3. 数据更新优化

```javascript
// 推荐：使用Amis的数据更新机制
{
    value: '${realTimeData}',  // 自动更新
    description: '更新时间: ${lastUpdate}'
}
```

## 最佳实践

### 1. 网格布局设计

```javascript
// ✅ 推荐：自适应布局
grid: {
    columns: 'auto-fit',
    minItemWidth: '200px',
    gap: '1.25rem'
}

// ❌ 避免：固定过多列数
grid: {
    columns: 6  // 在小屏幕上可能显示不佳
}
```

### 2. 图标使用规范

```javascript
// ✅ 推荐：统一图标风格和尺寸
items: [
    { icon: 'users', iconColor: '#3498db' },
    { icon: 'server', iconColor: '#e74c3c' },
    { icon: 'database', iconColor: '#f39c12' }
]

// ❌ 避免：混乱的图标样式
items: [
    { icon: 'users', iconColor: '#3498db', iconSize: 'lg' },
    { icon: 'server', iconColor: '#ff0000', iconSize: 'sm' }
]
```

### 3. 标签和数值设计

```javascript
// ✅ 推荐：简洁明了的标签
{
    label: 'CPU使用率',
    value: '${cpuUsage}%',
    description: '${cpuStatus}'
}

// ❌ 避免：过长的标签
{
    label: '当前系统CPU处理器使用率百分比',
    value: '${cpuUsage}%'
}
```

### 4. 响应式考虑

```javascript
// ✅ 推荐：考虑移动端显示
grid: {
    columns: 'auto-fit',
    minItemWidth: '180px'  // 在移动端也能正常显示
}

// ❌ 避免：只考虑桌面端
grid: {
    columns: 'repeat(6, 1fr)'  // 移动端显示困难
}
```

### 5. 数据表达式使用

```javascript
// ✅ 推荐：合理使用表达式
{
    value: '${onlineUsers}',
    description: '在线率 ${ROUND(onlineUsers/totalUsers*100)}%',
    highlight: '${onlineUsers < totalUsers * 0.5}'
}

// ❌ 避免：过于复杂的表达式
{
    value: '${ROUND(SQRT(POW(x,2)+POW(y,2))*100)/100}',  // 过于复杂
}
```

## 常见问题

### Q1: 如何设置网格的最小宽度？

A: 使用 `grid.minItemWidth` 属性：

```javascript
grid: {
    columns: 'auto-fit',
    minItemWidth: '220px'  // 设置最小宽度
}
```

### Q2: 图标不显示怎么办？

A: 检查以下几点：
1. 确保FontAwesome CSS已加载
2. 图标名称正确（不含`fa-`前缀）
3. `grid.showIcons` 设置为 `true`

```javascript
// 正确的图标配置
{
    icon: 'users',  // 不是 'fa-users'
    iconColor: '#3498db'
}
```

### Q3: 如何在移动端优化显示？

A: 使用较小的最小宽度和间距：

```javascript
grid: {
    columns: 'auto-fit',
    minItemWidth: '150px',  // 移动端友好的宽度
    gap: '1rem'             // 较小的间距
}
```

### Q4: 如何实现条件高亮？

A: 使用Amis表达式：

```javascript
{
    value: '${status}',
    highlight: '${status !== "正常"}',  // 条件高亮
    iconColor: '${status === "正常" ? "#28a745" : "#dc3545"}'
}
```

### Q5: 如何自定义样式？

A: 使用CSS类名覆盖默认样式：

```css
.my-custom-grid .info-value {
    font-size: 1.5rem;
    color: #2c3e50;
    font-weight: 700;
}
```

然后在配置中添加自定义类名：

```javascript
{
    id: 'custom-grid',
    type: 'info-grid',
    className: 'my-custom-grid',
    // ...其他配置
}
```

## 参考资源

### 相关文档
- [Amis Cards 总体使用指南](./card-usage-guide.md)
- [统计卡片使用指南](./stat-card-guide.md)
- [表格卡片使用指南](./table-card-guide.md)

### 示例页面
- [InfoGrid测试页面](../tests/info-grid-test.html)
- [基础演示页面](../demo/index.html)
- [监控仪表板演示](../demo/monitor-dashboard.html)

### 技术参考
- [Amis官方文档](https://aisuda.bce.baidu.com/amis/)
- [FontAwesome图标库](https://fontawesome.com/icons)
- [CSS Grid布局指南](https://developer.mozilla.org/en-US/docs/Web/CSS/CSS_Grid_Layout)

---