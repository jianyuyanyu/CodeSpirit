# CodeSpirit.ApprovalApi Service层实现

## 概述

本文档描述了CodeSpirit.ApprovalApi的Service层实现，包含了完整的审批流程管理功能。

## 核心服务

### 1. 审批实例服务 (ApprovalInstanceService)
- **功能**: 管理审批实例的生命周期
- **主要方法**:
  - `StartApprovalAsync`: 发起审批
  - `GetDetailAsync`: 获取审批详情
  - `WithdrawAsync`: 撤回审批
  - `GetByEntityAsync`: 根据业务实体获取审批实例
  - `GetMyApplicationsAsync`: 获取我发起的审批

### 2. 审批任务服务 (ApprovalTaskService)
- **功能**: 管理审批任务的处理
- **主要方法**:
  - `ProcessTaskAsync`: 处理审批任务
  - `GetMyPendingTasksAsync`: 获取我的待办任务
  - `GetMyCompletedTasksAsync`: 获取我的已办任务
  - `AddSignAsync`: 加签
  - `TransferTaskAsync`: 转交任务

### 3. 工作流定义服务 (WorkflowDefinitionService)
- **功能**: 管理工作流定义
- **主要方法**:
  - `GetByCodeAsync`: 根据代码获取工作流定义
  - `SetEnabledAsync`: 启用/禁用工作流
  - `CopyAsync`: 复制工作流定义
  - 继承自BaseCRUDService的增删改查方法

### 4. 工作流引擎 (WorkflowEngine)
- **功能**: 核心工作流执行引擎
- **主要方法**:
  - `StartWorkflowAsync`: 启动工作流
  - `ProcessTaskAsync`: 处理审批任务
  - `AddSignAsync`: 加签
  - `TransferTaskAsync`: 转交任务
  - `WithdrawAsync`: 撤回审批
  - `GetPendingTasksAsync`: 获取用户待办任务
  - `GetInstanceAsync`: 获取审批实例详情

### 5. 条件引擎 (ConditionEngine)
- **功能**: 处理工作流条件逻辑
- **主要方法**:
  - `EvaluateAsync`: 评估条件表达式
  - `GetNextNodesAsync`: 获取下一个节点
  - `ResolveApproversAsync`: 解析审批人

### 6. 智能审批服务 (IntelligentApprovalService)
- **功能**: 基于LLM的智能审批功能
- **主要方法**:
  - `AssessRiskAsync`: 风险识别
  - `GetApprovalSuggestionAsync`: 智能审批建议
  - `DetectAnomaliesAsync`: 异常检测
  - `CheckComplianceAsync`: 合规性检查

### 7. 审批日志服务 (ApprovalLogService)
- **功能**: 管理审批操作日志
- **主要方法**:
  - `LogAsync`: 记录审批日志
  - `LogBatchAsync`: 批量记录审批日志
  - `GetLogsAsync`: 获取审批日志
  - `GetUserLogsAsync`: 获取用户操作日志
  - `GetLogStatisticsAsync`: 获取审批日志统计
  - `CleanupExpiredLogsAsync`: 清理过期日志

## 事件系统

### 事件定义 (Events/ApprovalEvents.cs)
- `ApprovalStartedEvent`: 审批启动事件
- `ApprovalCompletedEvent`: 审批完成事件
- `TaskAssignedEvent`: 任务分配事件
- `TaskCompletedEvent`: 任务完成事件
- `BusinessDataStatusChangedEvent`: 业务数据状态更新事件

### 事件处理器 (Events/ApprovalEventHandlers.cs)
- `ApprovalStartedEventHandler`: 处理审批启动事件
- `ApprovalCompletedEventHandler`: 处理审批完成事件
- `TaskAssignedEventHandler`: 处理任务分配事件
- `TaskCompletedEventHandler`: 处理任务完成事件
- `BusinessDataStatusChangedEventHandler`: 处理业务数据状态变更事件

## 服务注册

### ApprovalServiceCollectionExtensions
提供了便捷的服务注册扩展方法：
- `AddApprovalServices`: 注册所有审批相关服务
- `AddApprovalEventHandlers`: 注册事件处理器

## 配置选项

### ApprovalOptions
- `Enabled`: 是否启用审批功能
- `DefaultTimeoutHours`: 默认超时时间
- `EnableAutoReminder`: 是否启用自动提醒
- `EnableApprovalLog`: 是否启用审批日志
- `EnableIntelligentApproval`: 是否启用智能审批
- `Cache`: 缓存配置
- `Notification`: 通知配置
- `LLM`: LLM配置

## 使用示例

### 1. 服务注册
```csharp
// 在Program.cs中注册服务
builder.Services.AddApprovalServices(builder.Configuration);
builder.Services.AddApprovalEventHandlers();
```

### 2. 发起审批
```csharp
var approvalRequest = new StartApprovalDto
{
    WorkflowCode = "LEAVE_APPROVAL",
    EntityType = "HRApi.LeaveApplication",
    EntityId = "12345",
    Title = "请假申请",
    BusinessData = new { Days = 3, Reason = "个人事务" }
};

var instance = await approvalInstanceService.StartApprovalAsync(approvalRequest);
```

### 3. 处理审批任务
```csharp
var processRequest = new ProcessApprovalTaskDto
{
    Result = ApprovalResult.Approve,
    Comment = "同意请假"
};

var result = await approvalTaskService.ProcessTaskAsync(taskId, processRequest);
```

## 技术特性

1. **多租户支持**: 所有服务都支持多租户数据隔离
2. **事件驱动**: 基于事件总线实现松耦合的微服务通信
3. **智能审批**: 集成LLM提供风险评估和审批建议
4. **缓存优化**: 关键数据使用缓存提升性能
5. **审计日志**: 完整的操作日志记录
6. **异常处理**: 统一的异常处理和日志记录
7. **配置灵活**: 丰富的配置选项支持不同场景

## 扩展性

1. **自定义审批人解析器**: 实现`ICustomApproverResolver`接口
2. **自定义条件函数**: 实现`ICustomConditionFunction`接口
3. **自定义事件处理器**: 实现`ITenantAwareEventHandler<T>`接口
4. **自定义智能审批策略**: 扩展`IntelligentApprovalService`

## 注意事项

1. 确保数据库连接字符串正确配置
2. 如需使用智能审批功能，需要配置LLM服务
3. 如需使用Redis缓存，需要配置Redis连接字符串
4. 事件处理器需要在启动时正确注册
5. 多租户环境下需要确保租户ID正确传递
