# Pathfinder 技术路线图

## 文档信息

| 项目 | 内容 |
|------|------|
| **文档类型** | 技术路线图 |
| **版本** | v1.0 |
| **创建日期** | 2025年11月3日 |
| **维护团队** | Pathfinder开发团队 |

---

## 1. MVP阶段（Week 1-12）

### 目标
验证"AI拆解+督促"的核心价值，获得1000个种子用户

### Phase 1: 基础框架搭建（Week 1-2）

#### Week 1: 项目初始化

**开发任务：**
- [ ] 创建 Pathfinder.Api 项目结构
  - 基于 CodeSpirit.ServiceDefaults 模板
  - 配置项目依赖（EF Core, LLM, Amis等）
- [ ] 配置 Aspire 编排
  - 更新 CodeSpirit.AppHost/Program.cs
  - 添加 Pathfinder.Api 服务引用
- [ ] 配置数据库连接
  - SQL Server 连接字符串
  - MySQL 连接字符串（可选）
- [ ] 配置 Redis 缓存
- [ ] 配置 RabbitMQ 消息队列

**技术产出：**
- ✅ Pathfinder.Api 项目可启动
- ✅ Aspire Dashboard 显示所有服务
- ✅ 数据库连接测试通过

**估算时间：** 2-3天

#### Week 2: 数据模型设计

**开发任务：**
- [ ] 设计核心实体
  - Goal（目标）
  - Task（任务）
  - TaskDependency（任务依赖）
  - Reminder（提醒）
  - UserReminderSettings（提醒设置）
- [ ] 实现 PathfinderDbContext
- [ ] 创建 EF Core 迁移
  - SQL Server 迁移
  - MySQL 迁移
- [ ] 配置实体关系
- [ ] 添加种子数据（可选）

**技术产出：**
- ✅ 数据库架构完成
- ✅ 迁移脚本可执行
- ✅ 实体配置测试通过

**估算时间：** 3-4天

---

### Phase 2: 目标管理功能（Week 3-4）

#### Week 3: 目标CRUD

**开发任务：**
- [ ] 实现 Goal 实体服务
  - GoalService（继承 BaseCRUDService）
  - IGoalRepository
  - GoalDto、CreateGoalDto、UpdateGoalDto
- [ ] 实现 GoalsController
  - 基于 AmisControllerBase
  - 自动生成 AMIS 界面配置
- [ ] AI目标解析（初步）
  - 集成 CodeSpirit.LLM
  - 实现 GoalParserService
  - 提取目标类型、时间约束等

**技术产出：**
- ✅ 目标列表页面（AMIS生成）
- ✅ 目标创建表单（AMIS生成）
- ✅ AI解析基础功能

**API设计：**
```
POST   /api/goals              创建目标
GET    /api/goals              获取目标列表
GET    /api/goals/{id}         获取目标详情
PUT    /api/goals/{id}         更新目标
DELETE /api/goals/{id}         删除目标
```

**估算时间：** 4-5天

#### Week 4: 前端界面优化

**开发任务：**
- [ ] 优化 AMIS 配置
  - 自定义目标卡片样式
  - 添加进度条显示
  - 优化表单布局
- [ ] 添加权限控制
  - 使用 CodeSpirit.Authorization
  - [RequirePermission] 特性
- [ ] 添加审计日志
  - 使用 CodeSpirit.Audit
  - 自动记录操作

**技术产出：**
- ✅ 完善的目标管理界面
- ✅ 权限控制生效
- ✅ 操作日志记录

**估算时间：** 3天

---

### Phase 3: AI任务拆解（Week 5-6）

#### Week 5: 拆解引擎核心

**开发任务：**
- [ ] **实现目标可行性评估（重要！）** ⭐
  - GoalFeasibilityEvaluator 服务
  - 评估目标的明确性、可执行性、完整性
  - **仅当目标明确、可执行时才支持AI拆解**
  - 模糊目标返回澄清问题和建议
  - 前端澄清对话框实现
- [ ] 实现 TaskBreakdownService
  - AI Prompt 设计与优化
  - LLM 调用封装
  - 响应解析与验证
  - 集成可行性评估逻辑
- [ ] 实现 Task 实体服务
  - TaskService
  - TaskDto、CreateTaskDto
- [ ] 依赖关系处理
  - TaskDependencyService
  - 循环依赖检测算法

**技术产出：**
- ✅ AI拆解核心逻辑
- ✅ 任务依赖关系管理
- ✅ 拆解结果验证

**Prompt示例：**
```
你是一个项目管理专家，擅长将目标拆解为可执行的任务。

用户目标：{goal.Description}
目标类型：{goal.Category}
完成时间：{goal.TargetDate}

请拆解为5-15个子任务，输出JSON格式...
```

**估算时间：** 4-5天

#### Week 6: 时间节点分配

**开发任务：**
- [ ] 实现时间节点分配算法
  - 基于依赖关系的拓扑排序
  - 关键路径计算
  - 时间缓冲添加（20%）
- [ ] 拆解结果展示页面
  - AMIS 时间线视图
  - 任务卡片组件
  - 依赖关系可视化
- [ ] 用户调整功能
  - 手动修改任务
  - 重新拆解
  - 保存确认

**技术产出：**
- ✅ 时间节点自动分配
- ✅ 拆解结果页面
- ✅ 用户可编辑功能

**估算时间：** 3-4天

---

### Phase 4: 任务看板（Week 7-8）

#### Week 7: 看板核心功能

**开发任务：**
- [ ] 实现任务状态管理
  - 状态转换逻辑
  - 依赖任务解锁
  - 进度计算
- [ ] TasksController 实现
  - 基于 AmisControllerBase
  - 支持筛选、排序
- [ ] AMIS 看板配置
  - 列表视图
  - 看板视图（Kanban）
  - 日历视图（可选）

**技术产出：**
- ✅ 任务看板界面
- ✅ 状态流转功能
- ✅ 多视图切换

**API设计：**
```
GET    /api/tasks              获取任务列表
GET    /api/tasks/{id}         获取任务详情
PATCH  /api/tasks/{id}/status  更新任务状态
PUT    /api/tasks/{id}         更新任务
DELETE /api/tasks/{id}         删除任务
```

**估算时间：** 4-5天

#### Week 8: 任务详情与编辑

**开发任务：**
- [ ] 任务详情页面
  - 详细信息展示
  - 依赖任务列表
  - 执行记录
- [ ] 任务编辑功能
  - 内联编辑
  - 批量操作
- [ ] 进度统计
  - 目标进度计算
  - 统计图表（Charts组件）

**技术产出：**
- ✅ 完善的任务管理功能
- ✅ 批量操作支持
- ✅ 进度可视化

**估算时间：** 3-4天

---

### Phase 5: 智能提醒系统（Week 9-10）

#### Week 9: 提醒规则引擎

**开发任务：**
- [ ] 实现 ReminderService
  - 提醒规则定义
  - 规则匹配引擎
- [ ] 实现定时任务
  - 使用 CodeSpirit.ScheduledTasks
  - ReminderCheckTask（每分钟）
  - DailySummaryTask（每日早晨）
- [ ] 提醒渠道抽象
  - INotificationChannel 接口
  - PushNotificationChannel（站内）
  - WeChatNotificationChannel（微信）
  - EmailNotificationChannel（邮件）

**技术产出：**
- ✅ 提醒规则引擎
- ✅ 定时检查任务
- ✅ 多渠道支持

**提醒类型：**
- daily_summary（每日清单）
- deadline（截止提醒）
- overdue（逾期提醒）
- nudge（停滞提醒）
- achievement（成就提醒）

**估算时间：** 4-5天

#### Week 10: 用户提醒设置

**开发任务：**
- [ ] UserReminderSettings 实体
- [ ] 提醒设置页面
  - AMIS 表单生成
  - 渠道配置
  - 时间设置
- [ ] 免打扰时段
  - 时段检查逻辑
  - 延后发送机制

**技术产出：**
- ✅ 提醒设置界面
- ✅ 免打扰功能
- ✅ 完整的提醒系统

**估算时间：** 3天

---

### Phase 6: 测试与优化（Week 11-12）

#### Week 11: 测试覆盖

**开发任务：**
- [ ] 单元测试
  - GoalService 测试
  - TaskService 测试
  - BreakdownService 测试
  - ReminderService 测试
- [ ] 集成测试
  - API 端到端测试
  - 数据库集成测试
- [ ] 性能测试
  - 负载测试
  - 并发测试

**技术产出：**
- ✅ 测试覆盖率 > 70%
- ✅ 所有核心功能有测试

**估算时间：** 4-5天

#### Week 12: 优化与发布

**开发任务：**
- [ ] 性能优化
  - SQL 查询优化
  - 添加缓存
  - 异步处理优化
- [ ] Bug 修复
  - 修复测试发现的问题
  - 修复用户反馈的问题
- [ ] 文档完善
  - API 文档
  - 用户手册
- [ ] MVP 发布准备
  - 部署脚本
  - 监控配置

**技术产出：**
- ✅ MVP 版本发布
- ✅ 性能达标
- ✅ 文档完善

**估算时间：** 3-4天

---

## 2. 一期扩展阶段（Week 13-24）

### 目标
增强自动化能力与用户粘性，付费用户突破200人

### Phase 7: 自动化服务基础（Week 13-14）

#### Week 13: 自动化服务搭建

**开发任务：**
- [ ] 创建 Pathfinder.AutomationApi 项目
  - 配置 Aspire 编排
  - 添加服务依赖
  - 配置数据库连接
- [ ] **实现Python脚本执行器** ⭐
  - PythonExecutor 核心类
  - 基于 System.Diagnostics.Process
  - 沙箱环境隔离（SandboxManager）
  - 超时控制（30秒默认）
  - 参数注入机制
  - 结果解析（JSON格式）
- [ ] 实现执行代理框架
  - ExecutionAgentBase 抽象类
  - UniversalAgent 通用代理
  - AgentPoolManager 代理池管理
  - ExecutionEngine 统一调度引擎
- [ ] 实现工具注册中心
  - IExecutionTool 接口
  - ToolRegistry 工具注册表
  - ToolMetadata 工具元数据
- [ ] **安全性机制** ⭐
  - 沙箱自动清理
  - 超时强制终止进程树
  - Python库白名单
  - 危险代码检测
  - 审计日志记录

**技术产出：**
- ✅ 自动化服务可独立启动
- ✅ Python脚本执行器完成
- ✅ 代理池管理完成
- ✅ 安全机制到位

**关键配置：**
```json
{
  "Pathfinder": {
    "SandboxPath": "D:/pathfinder_sandbox",
    "PythonExecutor": {
      "PythonPath": "python",
      "ExecutionTimeoutSeconds": 30,
      "MaxMemoryMB": 512
    }
  }
}
```

**估算时间：** 5-6天

#### Week 14: API接口与工具实现

**开发任务：**
- [ ] **实现AutomationController** ⭐
  - POST /api/automation/execute/python - 执行Python脚本
  - POST /api/automation/execute/tool - 执行工具
  - POST /api/automation/test - 测试执行
  - GET /api/automation/tools - 获取工具列表
  - POST /api/automation/configs - 保存配置
- [ ] 实现核心工具（至少3个）
  - WebScraperTool（网页抓取）
  - CalendarApiTool（日历API）
  - EmailSenderTool（邮件发送）
- [ ] 前端界面
  - 代码编辑器（Monaco Editor）
  - 参数配置表单
  - 测试执行按钮
  - 结果展示区
- [ ] 工具测试框架
  - 单元测试
  - 集成测试

**技术产出：**
- ✅ AutomationController API完成
- ✅ 3个可用的工具
- ✅ 前端测试界面
- ✅ 工具测试通过

**API示例：**
```bash
# 测试Python脚本执行
curl -X POST http://localhost:5000/api/automation/test \
  -H "Content-Type: application/json" \
  -d '{
    "code": "import requests\nresult = requests.get(get_parameter(\"url\"))\noutput_result({\"status\": result.status_code})",
    "testParameters": {"url": "https://example.com"}
  }'
```

**估算时间：** 4-5天

---

### Phase 8: AI工具选择与执行（Week 15-16）

#### Week 15: AI工具选择引擎

**开发任务：**
- [ ] 实现 ToolSelector
  - 基于语义相似度匹配
  - 向量化工具描述
  - 相似度计算
- [ ] 实现 ParameterGenerator
  - AI生成工具参数
  - 参数验证
- [ ] 集成到任务拆解流程
  - 识别可自动化任务
  - 标记 automation_type

**技术产出：**
- ✅ AI自动选择工具
- ✅ 智能参数生成

**估算时间：** 4-5天

#### Week 16: 执行管道与结果处理

**开发任务：**
- [ ] 实现 ExecutionPipeline
  - 中间件模式
  - 参数验证、日志、缓存
- [ ] 实现 ResultHandler
  - 结果验证
  - AI结果验证（可选）
  - 结构化解析
- [ ] AutomationLog 记录

**技术产出：**
- ✅ 完整的执行管道
- ✅ 结果处理逻辑

**估算时间：** 3-4天

---

### Phase 9: 自动化配置与调度（Week 17-18）

#### Week 17: 自动化配置界面

**开发任务：**
- [ ] AutomationConfig 实体
- [ ] 自动化配置页面
  - AMIS 表单生成
  - 工具选择器
  - 参数配置
- [ ] OAuth 集成
  - 飞书/钉钉授权
  - Token 加密存储

**技术产出：**
- ✅ 用户可配置自动化
- ✅ OAuth授权流程

**估算时间：** 4-5天

#### Week 18: 任务调度与执行

**开发任务：**
- [ ] 实现任务队列
  - 使用 RabbitMQ
  - CodeSpirit.Messaging 集成
- [ ] 实现调度器
  - 定时执行
  - 手动触发
  - 事件驱动
- [ ] 失败重试机制
  - 指数退避
  - 最大重试次数

**技术产出：**
- ✅ 异步任务执行
- ✅ 智能重试

**估算时间：** 3-4天

---

### Phase 10: 进度预警与成就（Week 19-20）

#### Week 19: 进度预警系统

**开发任务：**
- [ ] 实现预警规则引擎
  - 停滞任务检测
  - 逾期任务检测
  - 目标进度检测
- [ ] 预警通知
  - 集成提醒系统
  - AI生成预警文案

**技术产出：**
- ✅ 智能预警系统
- ✅ 主动提醒用户

**估算时间：** 3-4天

#### Week 20: 成就系统

**开发任务：**
- [ ] Achievement 实体
- [ ] UserAchievement 实体
- [ ] 成就检查服务
  - 事件驱动检查
  - 异步处理
- [ ] 成就展示页面
  - 徽章展示
  - 解锁动画

**技术产出：**
- ✅ 完整的成就系统
- ✅ 用户激励机制

**估算时间：** 3-4天

---

### Phase 11: 目标复盘（Week 21-22）

#### Week 21: 复盘报告生成

**开发任务：**
- [ ] GoalReview 实体
- [ ] 实现 GoalReviewService
  - 数据统计
  - AI分析建议
- [ ] 复盘报告页面
  - 数据可视化
  - 图表展示

**技术产出：**
- ✅ 自动生成复盘报告
- ✅ AI提供改进建议

**估算时间：** 3-4天

#### Week 22: 分享与导出

**开发任务：**
- [ ] 复盘报告导出
  - PDF 生成（CodeSpirit.PdfGeneration）
  - 图片生成
- [ ] 分享功能
  - 生成分享海报
  - 社交媒体分享

**技术产出：**
- ✅ 复盘报告可导出
- ✅ 社交分享功能

**估算时间：** 3天

---

### Phase 12: 一期测试与优化（Week 23-24）

#### Week 23: 全面测试

**开发任务：**
- [ ] 功能测试
  - 所有一期功能
  - 端到端测试
- [ ] 性能测试
  - 自动化执行性能
  - 并发处理能力
- [ ] 安全测试
  - OAuth安全性
  - 敏感数据保护

**技术产出：**
- ✅ 测试报告
- ✅ Bug清单

**估算时间：** 4-5天

#### Week 24: 优化与发布

**开发任务：**
- [ ] 性能优化
  - AI调用优化
  - 缓存策略优化
- [ ] Bug 修复
- [ ] 用户体验优化
- [ ] 一期版本发布

**技术产出：**
- ✅ 一期版本发布
- ✅ 用户文档更新

**估算时间：** 3-4天

---

## 3. 关键里程碑

### M1: MVP核心功能完成（Week 10）
- ✅ 目标管理
- ✅ AI任务拆解
- ✅ 任务看板
- ✅ 智能提醒

**验收标准：**
- 用户可完整走通：创建目标 → AI拆解 → 管理任务 → 收到提醒
- 系统稳定运行，无严重Bug

### M2: MVP测试完成（Week 12）
- ✅ 测试覆盖率 > 70%
- ✅ 性能测试通过
- ✅ 用户验收通过

**验收标准：**
- 7日留存率 > 30%
- 响应时间 < 2秒
- 无P0级Bug

### M3: 自动化引擎完成（Week 18）
- ✅ 执行代理框架
- ✅ 工具系统
- ✅ AI工具选择
- ✅ 任务调度

**验收标准：**
- 至少5个可用工具
- 自动化执行成功率 > 80%
- 支持定时/手动/事件触发

### M4: 一期功能完成（Week 22）
- ✅ 自动化执行
- ✅ 进度预警
- ✅ 成就系统
- ✅ 目标复盘

**验收标准：**
- 自动化功能使用率 > 40%
- 用户满意度 > 4.0/5.0

### M5: 一期版本发布（Week 24）
- ✅ 所有功能测试通过
- ✅ 性能达标
- ✅ 文档完善

**验收标准：**
- 月留存率 > 50%
- 付费用户 > 200
- 免费到付费转化率 > 5%

---

## 4. 资源分配

### 开发团队配置

**MVP阶段（3个月）：**
- 后端开发：2人
- 前端开发：1人
- AI工程师：1人
- 测试工程师：1人

**一期扩展（3个月）：**
- 后端开发：3人
- 前端开发：1人
- AI工程师：1人
- 测试工程师：1人
- DevOps：0.5人

### 技术储备要求

**必备技能：**
- .NET 10 / C#
- Entity Framework Core
- ASP.NET Core Web API
- React + AMIS

**加分技能：**
- AI/LLM 应用开发
- RabbitMQ 消息队列
- Redis 缓存
- Docker / Kubernetes

---

## 5. 风险管理

### 技术风险

| 风险项 | 概率 | 影响 | 应对措施 |
|--------|------|------|----------|
| AI服务不稳定 | 中 | 高 | 实现降级策略，规则引擎兜底 |
| 性能不达标 | 中 | 中 | 提前性能测试，优化缓存 |
| 自动化工具失败率高 | 高 | 中 | 限制MVP工具范围，充分测试 |

### 进度风险

| 风险项 | 概率 | 影响 | 应对措施 |
|--------|------|------|----------|
| 开发进度延迟 | 中 | 高 | 预留缓冲时间（20%），灵活调整 |
| 人员变动 | 低 | 高 | 文档完善，代码规范 |
| 需求变更 | 中 | 中 | 敏捷开发，快速迭代 |

---

## 6. 后续展望

### 二期规划（6-12个月）

- 复杂数据处理自动化
- 更多第三方工具集成
- 数据看板与分析
- 移动端应用

### 三期规划（12-18个月）

- 团队协作模式
- AI个性化推荐
- API开放平台
- 企业版功能

---

**文档版本：** v1.0  
**最后更新：** 2025年11月3日  
**维护团队：** Pathfinder开发团队

