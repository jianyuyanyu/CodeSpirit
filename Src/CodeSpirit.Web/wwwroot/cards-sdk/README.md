# CodeSpirit 统计卡片前端SDK

一个强大的统计卡片展示SDK，支持多种卡片类型和实时数据更新。

## 🚀 快速开始

### 1. 引入SDK

```html
<!-- 引入样式文件 -->
<link rel="stylesheet" href="/cards-sdk/cards-sdk.css">

<!-- 引入JavaScript文件 -->
<script src="/cards-sdk/cards-sdk.js"></script>

<!-- 如果需要图表功能，还需引入ECharts -->
<script src="https://cdnjs.cloudflare.com/ajax/libs/echarts/5.4.0/echarts.min.js"></script>
```

### 2. 初始化SDK

```javascript
const cardsSDK = new CodeSpiritCards.SDK({
    container: '#cards-container',
    baseUrl: '/api',
    theme: 'default',
    autoRefresh: true,
    refreshInterval: 30000
});
```

### 3. 渲染卡片

```javascript
const cardConfigs = [
    {
        id: 'exam-count',
        type: 'stat',
        title: '今日考试',
        data: {
            value: 156,
            label: '场次',
            trend: { value: 12, direction: 'up', period: '较昨日' }
        },
        style: { theme: 'primary' }
    }
];

await cardsSDK.render('#cards-container', cardConfigs);
```

## 📊 支持的卡片类型

### 1. 统计卡片 (stat)

用于展示数值统计信息，支持趋势显示。

```javascript
{
    id: 'unique-id',
    type: 'stat',
    title: '卡片标题',
    subtitle: '副标题',
    size: 'medium', // small | medium | large
    style: { theme: 'primary' }, // primary | success | warning | danger | info
    data: {
        value: 1234,
        label: '数量单位',
        unit: '个',
        trend: {
            direction: 'up', // up | down | stable
            value: 12,
            period: '较昨日'
        }
    }
}
```

### 2. 图表卡片 (chart)

用于展示图表数据，基于ECharts。

```javascript
{
    id: 'chart-1',
    type: 'chart',
    title: '趋势图表',
    size: 'large',
    style: { height: 300 },
    data: {
        chartType: 'line',
        chartTitle: '月度趋势',
        xData: ['1月', '2月', '3月', '4月', '5月', '6月'],
        yData: [120, 132, 101, 134, 90, 230]
    }
}
```

### 3. 信息卡片 (info)

用于展示静态信息内容。

```javascript
{
    id: 'info-1',
    type: 'info',
    title: '系统信息',
    data: {
        content: '<p>这里是信息内容</p>'
    }
}
```

### 4. 操作卡片 (action)

用于展示快速操作按钮。

```javascript
{
    id: 'actions-1',
    type: 'action',
    title: '快速操作',
    data: {
        actions: [
            {
                label: '创建项目',
                icon: 'fas fa-plus',
                onclick: 'handleCreate()'
            }
        ]
    }
}
```

## 🎨 主题样式

支持以下主题：

- `default` - 默认蓝色主题
- `primary` - 主要蓝色
- `success` - 成功绿色
- `warning` - 警告橙色
- `danger` - 危险红色
- `info` - 信息青色

## 📱 响应式设计

SDK自动适配不同屏幕尺寸：

- **桌面端**: 多列网格布局
- **平板端**: 2列布局
- **手机端**: 单列布局

## 🔄 数据更新

### 手动更新

```javascript
// 更新单个卡片
await cardsSDK.update('card-id', { value: 999 });

// 刷新所有卡片
await cardsSDK.refresh();
```

### 自动刷新

```javascript
const cardsSDK = new CodeSpiritCards.SDK({
    autoRefresh: true,
    refreshInterval: 30000 // 30秒刷新一次
});
```

### 数据源配置

```javascript
{
    id: 'dynamic-card',
    type: 'stat',
    dataSource: '/api/statistics/exam-count', // 数据接口
    // ... 其他配置
}
```

## 🎯 事件监听

```javascript
// 监听卡片渲染完成
cardsSDK.eventBus.on('card-rendered', (data) => {
    console.log('卡片渲染完成:', data);
});

// 监听卡片数据更新
cardsSDK.eventBus.on('card-updated', (data) => {
    console.log('卡片数据更新:', data);
});

// 监听卡片销毁
cardsSDK.eventBus.on('card-destroyed', (data) => {
    console.log('卡片已销毁:', data);
});
```

## 🔧 API 参考

### SDK 配置选项

| 参数 | 类型 | 默认值 | 描述 |
|------|------|--------|------|
| container | string | '#cards-container' | 容器选择器 |
| baseUrl | string | '/api' | API基础路径 |
| theme | string | 'default' | 默认主题 |
| autoRefresh | boolean | true | 是否自动刷新 |
| refreshInterval | number | 30000 | 刷新间隔(毫秒) |

### 核心方法

| 方法 | 参数 | 返回值 | 描述 |
|------|------|--------|------|
| render | (containerId, configs) | Promise | 渲染卡片组 |
| renderCard | (container, config) | Promise | 渲染单个卡片 |
| update | (cardId, data) | Promise | 更新卡片数据 |
| refresh | (cardId?) | Promise | 刷新卡片 |
| destroy | (cardId) | void | 销毁卡片 |
| registerRenderer | (type, renderer) | void | 注册自定义渲染器 |

## 🔌 扩展开发

### 自定义渲染器

```javascript
class CustomCardRenderer {
    async render(config) {
        const card = document.createElement('div');
        card.className = 'card custom-card';
        // 自定义渲染逻辑
        return card;
    }
    
    async update(element, config) {
        // 自定义更新逻辑
    }
}

// 注册自定义渲染器
cardsSDK.registerRenderer('custom', new CustomCardRenderer());
```

## 🌐 浏览器支持

- Chrome 60+
- Firefox 55+
- Safari 12+
- Edge 79+

## 📄 许可证

MIT License - 详见 LICENSE 文件

## 🤝 贡献指南

欢迎提交 Issue 和 Pull Request！
