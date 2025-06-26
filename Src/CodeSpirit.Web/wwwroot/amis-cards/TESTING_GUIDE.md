# CodeSpirit Amis Cards V2.0 测试验证指南

## 概述

本文档为 CodeSpirit Amis Cards V2.0 项目的全面测试验证指南，确保系统在各种环境下的稳定性和性能表现。

## 🧪 测试分类

### 1. 功能测试

#### 1.1 核心功能验证

**卡片渲染测试**
- [ ] 统计卡片正常显示数值、标签、趋势
- [ ] 图表卡片支持6种图表类型渲染
- [ ] 表格卡片显示数据、分页、搜索功能
- [ ] 信息卡片显示8种内容类型
- [ ] 卡片尺寸变体（small/medium/large）正常应用

**主题系统测试**
- [ ] 6种内置主题正常切换
- [ ] CSS变量系统正常工作
- [ ] 主题配置正确加载
- [ ] 自定义主题支持
- [ ] 暗色模式自动检测

**交互功能测试**
- [ ] 卡片悬停效果
- [ ] 点击反馈动画
- [ ] 动态数据更新
- [ ] 配置实时更新
- [ ] 错误状态显示

#### 1.2 数据服务测试

**API请求测试**
- [ ] GET请求正常处理
- [ ] POST请求正常处理
- [ ] 错误处理机制
- [ ] 超时重试机制
- [ ] 缓存机制验证

**数据格式验证**
- [ ] JSON数据解析
- [ ] 数据转换正确性
- [ ] 异常数据处理
- [ ] 空数据处理
- [ ] 大数据量处理

### 2. 性能测试

#### 2.1 加载性能测试

**指标要求（基于PRINCIPLES.md）**
- [ ] 初始化时间 < 500ms
- [ ] 卡片渲染时间 < 300ms
- [ ] 内存使用 < 50MB
- [ ] 首屏渲染时间 < 1s

**测试场景**
```javascript
// 性能测试示例代码
function performanceTest() {
    const startTime = performance.now();
    
    // 初始化AmisCards
    window.AmisCards.init({
        container: '#test-container',
        cards: generateTestCards(100) // 100张卡片
    });
    
    const endTime = performance.now();
    console.log(`初始化耗时: ${endTime - startTime}ms`);
    
    // 内存使用检查
    if (performance.memory) {
        console.log(`内存使用: ${performance.memory.usedJSHeapSize / 1024 / 1024}MB`);
    }
}
```

#### 2.2 运行时性能测试

**大数据量测试**
- [ ] 渲染100+卡片性能
- [ ] 滚动流畅度测试
- [ ] 内存泄漏检测
- [ ] CPU使用率监控

**动画性能测试**
- [ ] 60fps动画流畅度
- [ ] 复杂动画CPU占用
- [ ] 动画内存影响
- [ ] 低端设备兼容性

### 3. 兼容性测试

#### 3.1 浏览器兼容性

**现代浏览器测试**
- [ ] Chrome 90+ ✅
- [ ] Firefox 88+ ✅
- [ ] Safari 14+ ✅
- [ ] Edge 90+ ✅

**移动端浏览器测试**
- [ ] Chrome Mobile
- [ ] Safari Mobile
- [ ] Samsung Internet
- [ ] UC Browser

#### 3.2 设备兼容性

**响应式设计测试**
- [ ] 手机端 (< 576px)
- [ ] 平板竖屏 (576px - 768px)
- [ ] 平板横屏 (768px - 992px)
- [ ] 桌面端 (992px - 1200px)
- [ ] 大屏桌面 (≥ 1200px)

**触控交互测试**
- [ ] 触摸滑动
- [ ] 双击缩放
- [ ] 长按操作
- [ ] 手势识别

### 4. 安全性测试

#### 4.1 输入验证测试

**XSS防护测试**
```javascript
// XSS测试用例
const maliciousInput = '<script>alert("XSS")</script>';
const sanitizedOutput = window.AmisCards.Utils.sanitizeHtml(maliciousInput);
// 应该输出: '&lt;script&gt;alert("XSS")&lt;/script&gt;'
```

**数据验证测试**
- [ ] 配置参数验证
- [ ] API响应验证
- [ ] 用户输入过滤
- [ ] SQL注入防护

#### 4.2 权限控制测试

**认证系统测试**
- [ ] TokenManager集成验证
- [ ] 权限检查机制
- [ ] 会话过期处理
- [ ] 角色权限验证

### 5. 集成测试

#### 5.1 与现有系统隔离测试

**命名空间隔离验证**
```javascript
// 验证命名空间隔离
console.log(typeof window.AmisCards); // 应该是 'object'
console.log(typeof window.CardsSDK);  // 应该是 'undefined' 或不影响现有功能
```

**CSS类名隔离验证**
- [ ] `amis-cards-` 前缀使用
- [ ] 与现有样式无冲突
- [ ] 样式优先级正确
- [ ] 主题切换无影响

#### 5.2 Amis框架集成测试

**Page组件集成**
- [ ] Amis Page正常渲染
- [ ] 卡片作为Amis组件使用
- [ ] 数据流正常传递
- [ ] 事件处理正确

## 🔧 测试工具和环境

### 自动化测试工具

**单元测试**
```javascript
// Jest测试示例
describe('AmisCards Core', () => {
    test('should initialize correctly', () => {
        const cards = new window.AmisCards.Core();
        expect(cards).toBeInstanceOf(Object);
    });
    
    test('should render stat card', () => {
        const config = {
            type: 'stat',
            value: 1234,
            label: 'Test Stat'
        };
        const result = window.AmisCards.StatRenderer.render(config);
        expect(result).toContain('1234');
    });
});
```

**性能监控**
```javascript
// 性能监控代码
const observer = new PerformanceObserver((list) => {
    const entries = list.getEntries();
    entries.forEach((entry) => {
        if (entry.name.includes('amis-cards')) {
            console.log(`${entry.name}: ${entry.duration}ms`);
        }
    });
});
observer.observe({entryTypes: ['measure', 'navigation']});
```

### 手动测试清单

#### 基础功能测试
1. **访问演示页面**
   - 打开 `/amis-cards/demo/index.html`
   - 验证页面正常加载
   - 检查控制台无错误

2. **卡片渲染测试**
   - 验证4种卡片类型正常显示
   - 测试主题切换功能
   - 检查响应式布局

3. **交互测试**
   - 点击主题切换按钮
   - 悬停查看动画效果
   - 滚动页面测试性能

#### 兼容性测试
1. **浏览器测试**
   - 在不同浏览器中打开
   - 验证功能一致性
   - 检查样式兼容性

2. **设备测试**
   - 手机端测试
   - 平板端测试
   - 不同分辨率测试

## 📊 测试报告模板

### 测试执行记录

| 测试项目 | 状态 | 执行时间 | 备注 |
|----------|------|----------|------|
| 功能测试 | ✅ | 2024-01-15 | 所有核心功能正常 |
| 性能测试 | ⚠️ | 2024-01-15 | 大数据量下需优化 |
| 兼容性测试 | ✅ | 2024-01-15 | 主流浏览器兼容 |
| 安全性测试 | ✅ | 2024-01-15 | 无安全漏洞 |

### 性能指标

| 指标 | 目标值 | 实际值 | 状态 |
|------|--------|--------|------|
| 初始化时间 | < 500ms | 320ms | ✅ |
| 卡片渲染时间 | < 300ms | 180ms | ✅ |
| 内存使用 | < 50MB | 28MB | ✅ |
| 首屏渲染 | < 1s | 0.8s | ✅ |

### 问题反馈

**高优先级问题**
- [ ] 问题1: 描述
- [ ] 问题2: 描述

**中优先级问题**
- [ ] 问题3: 描述
- [ ] 问题4: 描述

**低优先级问题**
- [ ] 问题5: 描述
- [ ] 问题6: 描述

## 🚀 测试环境配置

### 开发环境测试
```bash
# 1. 启动开发服务器
cd /d/repos/code-spirit/Src/CodeSpirit.Web
dotnet run --launch-profile "https"

# 2. 访问测试页面
# https://localhost:7143/amis-cards/demo/index.html
```

### 生产环境测试
```bash
# 1. 构建生产版本
dotnet build -c Release

# 2. 发布到测试服务器
dotnet publish -c Release -o ./publish

# 3. 部署验证
# 访问生产环境URL进行验证
```

## 📋 测试检查清单

### 发布前必检项

**代码质量**
- [ ] ESLint检查通过
- [ ] 代码审查完成
- [ ] 单元测试覆盖率 > 80%
- [ ] 文档更新完整

**功能验证**
- [ ] 所有卡片类型正常工作
- [ ] 主题系统完整可用
- [ ] 响应式设计正确
- [ ] 动画效果流畅

**性能验证**
- [ ] 加载时间达标
- [ ] 内存使用合理
- [ ] 无内存泄漏
- [ ] 动画性能良好

**兼容性验证**
- [ ] 主流浏览器支持
- [ ] 移动端适配良好
- [ ] 触控交互正常
- [ ] 可访问性支持

**安全验证**
- [ ] XSS防护有效
- [ ] 输入验证完整
- [ ] 权限控制正确
- [ ] 敏感信息保护

## 📝 测试结论

本测试指南涵盖了 CodeSpirit Amis Cards V2.0 项目的所有关键测试点，确保系统在各种场景下的稳定性和可靠性。所有测试项目均需在发布前完成验证。

测试过程中发现的问题应及时记录并处理，确保最终交付的产品符合项目原则和质量标准。 