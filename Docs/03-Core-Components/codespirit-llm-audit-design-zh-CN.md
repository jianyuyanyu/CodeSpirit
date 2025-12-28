# CodeSpirit.LLM.Audit - LLM审计组件设计方案

## 📋 文档信息

- **版本**: v1.0
- **创建日期**: 2025-01-09
- **状态**: 待评审
- **作者**: AI Assistant

## 📖 目录

1. [概述](#概述)
2. [设计目标](#设计目标)
3. [架构设计](#架构设计)
4. [数据模型](#数据模型)
5. [配置设计](#配置设计)
6. [服务实现](#服务实现)
7. [存储适配](#存储适配)
8. [查询服务](#查询服务)
9. [集成方案](#集成方案)
10. [使用示例](#使用示例)
11. [性能优化](#性能优化)
12. [监控与运维](#监控与运维)

---

## 概述

### 背景

当前CodeSpirit框架已具备完善的审计组件（`CodeSpirit.Audit`）和LLM组件（`CodeSpirit.LLM`），但缺乏对LLM交互过程的审计能力。随着AI功能的广泛应用，需要对LLM的提示词、输出结果、校正过程进行全面审计，以实现：

- **合规性追溯**：记录AI决策过程，满足合规要求
- **质量监控**：监控LLM输出质量和准确性
- **成本分析**：统计Token使用和API调用成本
- **性能优化**：分析LLM响应时间和成功率
- **安全防护**：检测异常调用和敏感信息泄露

### 设计原则

1. **独立数据模型**：使用专用的LLM审计模型，而非继承通用审计模型
2. **复用基础设施**：充分利用现有审计组件的消息队列、存储适配等能力
3. **统一配置管理**：在审计配置中扩展LLM审计配置，保持配置一致性
4. **多存储支持**：支持Elasticsearch和GreptimeDB两种存储后端
5. **低侵入性**：通过装饰器模式集成，不影响现有LLM组件功能

---

## 设计目标

### 功能目标

- ✅ 记录LLM交互的完整生命周期（提示词 → 输出 → 校正）
- ✅ 支持多种LLM提供商（OpenAI、阿里云灵积等）
- ✅ 提供丰富的查询和统计API
- ✅ 自动脱敏敏感信息
- ✅ 支持多租户数据隔离

### 性能目标

- 审计记录延迟 < 100ms（异步处理）
- 支持10000+ TPS的审计记录吞吐
- 查询响应时间 < 1s（常规查询）
- 存储空间利用率优化 > 30%（相比继承方案）

### 技术目标

- 代码复用率 > 70%（复用现有审计基础设施）
- 测试覆盖率 > 85%
- 文档完整度 100%

---

## 架构设计

### 整体架构

```
┌─────────────────────────────────────────────────────────────┐
│                      LLM业务服务层                            │
│  (ExamApi, ConfigCenter, 其他使用LLM的服务)                   │
└─────────────────┬───────────────────────────────────────────┘
                  │
                  │ 调用
                  ↓
┌─────────────────────────────────────────────────────────────┐
│              LLM组件 (CodeSpirit.LLM)                        │
│  ┌──────────────────────────────────────────────────────┐   │
│  │  LLMAssistant (增强版 with 审计钩子)                   │   │
│  │  - GenerateContentAsync()                            │   │
│  │  - ProcessStructuredTaskAsync()                      │   │
│  │  └─→ 自动触发审计记录                                 │   │
│  └──────────────────────────────────────────────────────┘   │
└─────────────────┬───────────────────────────────────────────┘
                  │
                  │ 审计数据
                  ↓
┌─────────────────────────────────────────────────────────────┐
│            LLM审计服务 (ILLMAuditService)                     │
│  ┌──────────────────────────────────────────────────────┐   │
│  │  - LogLLMInteractionAsync()                          │   │
│  │  - SearchLLMAuditsAsync()                            │   │
│  │  - GetUsageStatsAsync()                              │   │
│  │  - GetCostStatsAsync()                               │   │
│  └──────────────────────────────────────────────────────┘   │
└─────────────────┬───────────────────────────────────────────┘
                  │
                  │ 发送到消息队列
                  ↓
┌─────────────────────────────────────────────────────────────┐
│              RabbitMQ (复用审计组件)                          │
│  Exchange: llm.audit.exchange                                │
│  Queue: llm.audit.queue                                      │
│  RoutingKey: llm.audit.log                                   │
└─────────────────┬───────────────────────────────────────────┘
                  │
                  │ 消费
                  ↓
┌─────────────────────────────────────────────────────────────┐
│          LLM审计消费者服务 (LLMAuditConsumerService)          │
│  - 批量消费审计消息                                            │
│  - 敏感数据脱敏处理                                            │
│  - 批量写入存储                                                │
└─────────────────┬───────────────────────────────────────────┘
                  │
                  │ 存储
                  ↓
┌──────────────────────────────┬──────────────────────────────┐
│    Elasticsearch             │      GreptimeDB               │
│    (文档型存储)                │      (时序数据库)              │
│    索引: llm_audit_logs      │      表: llm_audit_logs       │
└──────────────────────────────┴──────────────────────────────┘
```

### 核心组件

#### 1. LLM审计数据模型（独立设计）

```csharp
namespace CodeSpirit.Audit.LLM.Models;

/// <summary>
/// LLM审计日志模型
/// </summary>
public class LLMAuditLog : IMultiTenant
{
    // 基础字段
    public string Id { get; set; }
    public string TenantId { get; set; }
    public string UserId { get; set; }
    public string UserName { get; set; }
    public DateTime OperationTime { get; set; }
    
    // LLM特有字段
    public string LLMProvider { get; set; }      // OpenAI, Aliyun, etc.
    public string ModelName { get; set; }        // gpt-4, qwen-plus, etc.
    public string InteractionType { get; set; }  // Generate, Correct, Analyze
    public string BusinessScenario { get; set; } // QuestionAudit, ContentGen
    
    // 提示词和输出
    public string SystemPrompt { get; set; }
    public string UserPrompt { get; set; }
    public string LLMResponse { get; set; }
    public string ProcessedData { get; set; }
    
    // Token和性能
    public LLMTokenUsage TokenUsage { get; set; }
    public long ProcessingTimeMs { get; set; }
    public decimal? CostUsd { get; set; }
    
    // 状态和质量
    public bool IsSuccess { get; set; }
    public string ErrorMessage { get; set; }
    public int RetryCount { get; set; }
    public bool WasJsonRepaired { get; set; }
    public int? QualityScore { get; set; }
    
    // 元数据
    public Dictionary<string, object> Metadata { get; set; }
}
```

#### 2. LLM审计服务接口

```csharp
namespace CodeSpirit.Audit.LLM.Services;

/// <summary>
/// LLM审计服务接口
/// </summary>
public interface ILLMAuditService
{
    // 记录
    Task LogLLMInteractionAsync(LLMAuditLog auditLog);
    Task LogBatchLLMInteractionsAsync(IEnumerable<LLMAuditLog> auditLogs);
    
    // 查询
    Task<(IEnumerable<LLMAuditLog> Items, long Total)> SearchAsync(LLMAuditQueryDto query);
    Task<LLMAuditLog?> GetByIdAsync(string id);
    
    // 统计
    Task<LLMUsageStatsDto> GetUsageStatsAsync(DateTime start, DateTime end, string? tenantId = null);
    Task<LLMCostStatsDto> GetCostStatsAsync(DateTime start, DateTime end, string? tenantId = null);
    Task<LLMQualityStatsDto> GetQualityStatsAsync(DateTime start, DateTime end, string? tenantId = null);
    Task<Dictionary<DateTime, long>> GetUsageTrendAsync(DateTime start, DateTime end, int intervalHours = 24);
    
    // 健康检查
    Task<bool> HealthCheckAsync();
}
```

#### 3. LLM审计存储服务接口

```csharp
namespace CodeSpirit.Audit.LLM.Services;

/// <summary>
/// LLM审计存储服务接口
/// </summary>
public interface ILLMAuditStorageService
{
    Task<bool> InitializeAsync();
    Task<bool> StoreAsync(LLMAuditLog auditLog);
    Task<bool> BulkStoreAsync(IEnumerable<LLMAuditLog> auditLogs);
    Task<LLMAuditLog?> GetByIdAsync(string id);
    Task<(IEnumerable<LLMAuditLog> Items, long Total)> SearchAsync(LLMAuditQueryDto query);
    Task<Dictionary<string, long>> GetAggregationAsync(string field, DateTime start, DateTime end, string? tenantId = null);
    Task<bool> HealthCheckAsync();
}
```

---

## 数据模型

### 1. LLMAuditLog（核心审计日志）

```csharp
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CodeSpirit.Core;

namespace CodeSpirit.Audit.LLM.Models;

/// <summary>
/// LLM审计日志模型
/// </summary>
public class LLMAuditLog : IMultiTenant
{
    /// <summary>
    /// 审计ID
    /// </summary>
    [DisplayName("审计ID")]
    [Key]
    [StringLength(50)]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// 租户ID
    /// </summary>
    [DisplayName("租户ID")]
    [StringLength(50)]
    [Required]
    public string TenantId { get; set; } = string.Empty;
    
    /// <summary>
    /// 用户ID
    /// </summary>
    [DisplayName("用户ID")]
    [StringLength(50)]
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>
    /// 用户名
    /// </summary>
    [DisplayName("用户名")]
    [StringLength(100)]
    public string UserName { get; set; } = string.Empty;
    
    /// <summary>
    /// 操作时间（UTC）
    /// </summary>
    [DisplayName("操作时间")]
    [Required]
    public DateTime OperationTime { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// LLM提供商
    /// </summary>
    [DisplayName("LLM提供商")]
    [StringLength(50)]
    public string LLMProvider { get; set; } = string.Empty;
    
    /// <summary>
    /// LLM模型名称
    /// </summary>
    [DisplayName("模型名称")]
    [StringLength(100)]
    public string ModelName { get; set; } = string.Empty;
    
    /// <summary>
    /// 交互类型
    /// </summary>
    [DisplayName("交互类型")]
    [StringLength(50)]
    public string InteractionType { get; set; } = string.Empty;
    
    /// <summary>
    /// 业务场景
    /// </summary>
    [DisplayName("业务场景")]
    [StringLength(100)]
    public string BusinessScenario { get; set; } = string.Empty;
    
    /// <summary>
    /// 系统提示词
    /// </summary>
    [DisplayName("系统提示词")]
    public string SystemPrompt { get; set; } = string.Empty;
    
    /// <summary>
    /// 用户提示词
    /// </summary>
    [DisplayName("用户提示词")]
    public string UserPrompt { get; set; } = string.Empty;
    
    /// <summary>
    /// LLM原始响应
    /// </summary>
    [DisplayName("LLM响应")]
    public string LLMResponse { get; set; } = string.Empty;
    
    /// <summary>
    /// 处理后的数据
    /// </summary>
    [DisplayName("处理后数据")]
    public string ProcessedData { get; set; } = string.Empty;
    
    /// <summary>
    /// Token使用统计
    /// </summary>
    [DisplayName("Token使用")]
    public LLMTokenUsage TokenUsage { get; set; } = new LLMTokenUsage();
    
    /// <summary>
    /// 处理耗时（毫秒）
    /// </summary>
    [DisplayName("处理耗时(ms)")]
    [Range(0, long.MaxValue)]
    public long ProcessingTimeMs { get; set; }
    
    /// <summary>
    /// 成本（美元）
    /// </summary>
    [DisplayName("成本(USD)")]
    public decimal? CostUsd { get; set; }
    
    /// <summary>
    /// 是否成功
    /// </summary>
    [DisplayName("是否成功")]
    public bool IsSuccess { get; set; }
    
    /// <summary>
    /// 错误信息
    /// </summary>
    [DisplayName("错误信息")]
    public string ErrorMessage { get; set; } = string.Empty;
    
    /// <summary>
    /// 重试次数
    /// </summary>
    [DisplayName("重试次数")]
    [Range(0, int.MaxValue)]
    public int RetryCount { get; set; }
    
    /// <summary>
    /// 是否进行了JSON修复
    /// </summary>
    [DisplayName("JSON修复")]
    public bool WasJsonRepaired { get; set; }
    
    /// <summary>
    /// 质量评分（1-10）
    /// </summary>
    [DisplayName("质量评分")]
    [Range(1, 10)]
    public int? QualityScore { get; set; }
    
    /// <summary>
    /// 批次ID（用于关联同一批次的多个LLM调用）
    /// </summary>
    [DisplayName("批次ID")]
    [StringLength(50)]
    public string? BatchId { get; set; }
    
    /// <summary>
    /// 批次序号（在批次中的序号）
    /// </summary>
    [DisplayName("批次序号")]
    public int? BatchSequence { get; set; }
    
    /// <summary>
    /// 父审计ID（用于关联重试和修正操作）
    /// </summary>
    [DisplayName("父审计ID")]
    [StringLength(50)]
    public string? ParentAuditId { get; set; }
    
    /// <summary>
    /// 关联的业务实体ID（如题目ID、试卷ID等）
    /// </summary>
    [DisplayName("业务实体ID")]
    [StringLength(50)]
    public string? BusinessEntityId { get; set; }
    
    /// <summary>
    /// 关联的业务实体类型（Question、Exam等）
    /// </summary>
    [DisplayName("业务实体类型")]
    [StringLength(50)]
    public string? BusinessEntityType { get; set; }
    
    /// <summary>
    /// 处理的数据量（如生成的题目数、审核的题目数）
    /// </summary>
    [DisplayName("数据量")]
    public int? DataCount { get; set; }
    
    /// <summary>
    /// 附加元数据
    /// </summary>
    [DisplayName("元数据")]
    public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
}

/// <summary>
/// Token使用统计
/// </summary>
public class LLMTokenUsage
{
    /// <summary>
    /// 输入Token数
    /// </summary>
    [DisplayName("输入Token")]
    public int InputTokens { get; set; }
    
    /// <summary>
    /// 输出Token数
    /// </summary>
    [DisplayName("输出Token")]
    public int OutputTokens { get; set; }
    
    /// <summary>
    /// 总Token数
    /// </summary>
    [DisplayName("总Token")]
    public int TotalTokens { get; set; }
}
```

### 2. LLMAuditQueryDto（查询DTO）

```csharp
using CodeSpirit.Core.Dtos;
using System.ComponentModel;

namespace CodeSpirit.Audit.LLM.Models.Dtos;

/// <summary>
/// LLM审计查询DTO
/// </summary>
public class LLMAuditQueryDto : QueryDtoBase
{
    /// <summary>
    /// 租户ID
    /// </summary>
    [DisplayName("租户ID")]
    public string? TenantId { get; set; }
    
    /// <summary>
    /// 用户ID
    /// </summary>
    [DisplayName("用户ID")]
    public string? UserId { get; set; }
    
    /// <summary>
    /// LLM提供商
    /// </summary>
    [DisplayName("LLM提供商")]
    public string? LLMProvider { get; set; }
    
    /// <summary>
    /// 模型名称
    /// </summary>
    [DisplayName("模型名称")]
    public string? ModelName { get; set; }
    
    /// <summary>
    /// 交互类型
    /// </summary>
    [DisplayName("交互类型")]
    public string? InteractionType { get; set; }
    
    /// <summary>
    /// 业务场景
    /// </summary>
    [DisplayName("业务场景")]
    public string? BusinessScenario { get; set; }
    
    /// <summary>
    /// 是否成功
    /// </summary>
    [DisplayName("是否成功")]
    public bool? IsSuccess { get; set; }
    
    /// <summary>
    /// 开始时间
    /// </summary>
    [DisplayName("开始时间")]
    public DateTime? StartTime { get; set; }
    
    /// <summary>
    /// 结束时间
    /// </summary>
    [DisplayName("结束时间")]
    public DateTime? EndTime { get; set; }
    
    /// <summary>
    /// 最小处理时间（毫秒）
    /// </summary>
    [DisplayName("最小处理时间")]
    public long? MinProcessingTime { get; set; }
    
    /// <summary>
    /// 最大处理时间（毫秒）
    /// </summary>
    [DisplayName("最大处理时间")]
    public long? MaxProcessingTime { get; set; }
    
    /// <summary>
    /// 关键词搜索（在提示词和响应中搜索）
    /// </summary>
    [DisplayName("关键词")]
    public string? Keyword { get; set; }
}
```

### 3. LLMUsageStatsDto（使用统计DTO）

```csharp
namespace CodeSpirit.Audit.LLM.Models.Dtos;

/// <summary>
/// LLM使用统计DTO
/// </summary>
public class LLMUsageStatsDto
{
    /// <summary>
    /// 总交互次数
    /// </summary>
    public long TotalInteractions { get; set; }
    
    /// <summary>
    /// 成功交互次数
    /// </summary>
    public long SuccessfulInteractions { get; set; }
    
    /// <summary>
    /// 失败交互次数
    /// </summary>
    public long FailedInteractions { get; set; }
    
    /// <summary>
    /// 成功率
    /// </summary>
    public double SuccessRate { get; set; }
    
    /// <summary>
    /// 总Token使用量
    /// </summary>
    public long TotalTokensUsed { get; set; }
    
    /// <summary>
    /// 平均处理时间（毫秒）
    /// </summary>
    public double AverageProcessingTime { get; set; }
    
    /// <summary>
    /// 按交互类型统计
    /// </summary>
    public Dictionary<string, long> InteractionsByType { get; set; } = new();
    
    /// <summary>
    /// 按模型统计
    /// </summary>
    public Dictionary<string, long> InteractionsByModel { get; set; } = new();
    
    /// <summary>
    /// 按业务场景统计
    /// </summary>
    public Dictionary<string, long> InteractionsByScenario { get; set; } = new();
    
    /// <summary>
    /// 使用趋势（时间 → 交互次数）
    /// </summary>
    public Dictionary<DateTime, long> UsageTrend { get; set; } = new();
}
```

---

## 配置设计

### 1. 扩展AuditOptions

在现有的 `AuditOptions` 中添加LLM审计配置：

```csharp
namespace CodeSpirit.Audit.Models;

/// <summary>
/// 审计选项配置
/// </summary>
public class AuditOptions
{
    // ... 现有配置 ...
    
    /// <summary>
    /// 存储提供者类型（Elasticsearch 或 GreptimeDB）
    /// 【注意】LLM审计将跟随此配置，不单独配置
    /// </summary>
    public string StorageProvider { get; set; } = "Elasticsearch";
    
    /// <summary>
    /// LLM审计配置
    /// </summary>
    public LLMAuditOptions LLMAudit { get; set; } = new LLMAuditOptions();
}

/// <summary>
/// LLM审计配置选项
/// </summary>
public class LLMAuditOptions
{
    /// <summary>
    /// 是否启用LLM审计
    /// </summary>
    public bool Enabled { get; set; } = true;
    
    /// <summary>
    /// 是否记录提示词
    /// </summary>
    public bool LogPrompts { get; set; } = true;
    
    /// <summary>
    /// 是否记录LLM响应
    /// </summary>
    public bool LogResponses { get; set; } = true;
    
    /// <summary>
    /// 是否记录处理后的数据
    /// </summary>
    public bool LogProcessedData { get; set; } = false;
    
    /// <summary>
    /// 提示词最大长度（超过则截断）
    /// </summary>
    public int MaxPromptLength { get; set; } = 10000;
    
    /// <summary>
    /// 响应最大长度（超过则截断）
    /// </summary>
    public int MaxResponseLength { get; set; } = 50000;
    
    /// <summary>
    /// RabbitMQ配置
    /// </summary>
    public LLMRabbitMQOptions RabbitMQ { get; set; } = new LLMRabbitMQOptions();
    
    /// <summary>
    /// Elasticsearch配置
    /// </summary>
    public LLMElasticsearchOptions Elasticsearch { get; set; } = new LLMElasticsearchOptions();
    
    /// <summary>
    /// GreptimeDB配置
    /// </summary>
    public LLMGreptimeDbOptions GreptimeDB { get; set; } = new LLMGreptimeDbOptions();
    
    /// <summary>
    /// 敏感数据配置
    /// </summary>
    public LLMSensitiveDataOptions SensitiveData { get; set; } = new LLMSensitiveDataOptions();
    
    /// <summary>
    /// 成本计算配置
    /// </summary>
    public CostCalculationOptions CostCalculation { get; set; } = new CostCalculationOptions();
    
    /// <summary>
    /// 业务场景配置（用于自动识别业务场景）
    /// </summary>
    public Dictionary<string, string> ScenarioMapping { get; set; } = new Dictionary<string, string>
    {
        { "QuestionGeneration", "题目生成" },
        { "QuestionAudit", "题目审核" },
        { "QuestionCorrection", "题目校正" },
        { "ContentGeneration", "内容生成" }
    };
}

/// <summary>
/// LLM RabbitMQ配置选项
/// </summary>
public class LLMRabbitMQOptions
{
    /// <summary>
    /// 交换机名称
    /// </summary>
    public string ExchangeName { get; set; } = "llm.audit.exchange";
    
    /// <summary>
    /// 队列名称
    /// </summary>
    public string QueueName { get; set; } = "llm.audit.queue";
    
    /// <summary>
    /// 路由键
    /// </summary>
    public string RoutingKey { get; set; } = "llm.audit.log";
}

/// <summary>
/// LLM Elasticsearch配置选项
/// </summary>
public class LLMElasticsearchOptions
{
    /// <summary>
    /// 索引名称
    /// </summary>
    public string IndexName { get; set; } = "llm_audit_logs";
    
    /// <summary>
    /// 索引前缀
    /// </summary>
    public string IndexPrefix { get; set; } = "codespirit";
    
    /// <summary>
    /// 索引分片数
    /// </summary>
    public int NumberOfShards { get; set; } = 3;
    
    /// <summary>
    /// 索引副本数
    /// </summary>
    public int NumberOfReplicas { get; set; } = 1;
}

/// <summary>
/// LLM GreptimeDB配置选项
/// </summary>
public class LLMGreptimeDbOptions
{
    /// <summary>
    /// 表名
    /// </summary>
    public string TableName { get; set; } = "llm_audit_logs";
    
    /// <summary>
    /// 表前缀
    /// </summary>
    public string TablePrefix { get; set; } = "codespirit";
    
    /// <summary>
    /// 批量插入的批次大小
    /// </summary>
    public int BatchSize { get; set; } = 500;
}

/// <summary>
/// LLM敏感数据配置选项
/// </summary>
public class LLMSensitiveDataOptions
{
    /// <summary>
    /// 是否启用敏感数据脱敏
    /// </summary>
    public bool Enabled { get; set; } = true;
    
    /// <summary>
    /// 敏感字段模式列表
    /// </summary>
    public List<string> SensitiveFieldPatterns { get; set; } = new List<string>
    {
        "password", "pwd", "secret", "token", "apiKey", "key",
        "personalInfo", "idCard", "phone", "email", "address"
    };
    
    /// <summary>
    /// 掩码字符
    /// </summary>
    public string MaskCharacter { get; set; } = "*";
    
    /// <summary>
    /// 前面保留的字符数
    /// </summary>
    public int KeepFirstChars { get; set; } = 0;
    
    /// <summary>
    /// 末尾保留的字符数
    /// </summary>
    public int KeepLastChars { get; set; } = 0;
}

/// <summary>
/// 成本计算配置选项
/// </summary>
public class CostCalculationOptions
{
    /// <summary>
    /// 是否启用成本计算
    /// </summary>
    public bool Enabled { get; set; } = false;
    
    /// <summary>
    /// 模型价格配置（模型名 → 每1000 tokens价格USD）
    /// </summary>
    public Dictionary<string, ModelPricing> ModelPricing { get; set; } = new Dictionary<string, ModelPricing>
    {
        { "gpt-4", new ModelPricing { InputPer1K = 0.03m, OutputPer1K = 0.06m } },
        { "gpt-3.5-turbo", new ModelPricing { InputPer1K = 0.0015m, OutputPer1K = 0.002m } },
        { "qwen-plus", new ModelPricing { InputPer1K = 0.004m, OutputPer1K = 0.012m } },
        { "qwen-turbo", new ModelPricing { InputPer1K = 0.002m, OutputPer1K = 0.006m } }
    };
}

/// <summary>
/// 模型定价
/// </summary>
public class ModelPricing
{
    /// <summary>
    /// 输入每1000 tokens价格（USD）
    /// </summary>
    public decimal InputPer1K { get; set; }
    
    /// <summary>
    /// 输出每1000 tokens价格（USD）
    /// </summary>
    public decimal OutputPer1K { get; set; }
}
```

### 2. 配置示例

```json
{
  "Audit": {
    "Enabled": true,
    "StorageProvider": "Elasticsearch",
    "LogRequestParams": true,
    "LogResponseData": false,
    "RabbitMQ": {
      "ExchangeName": "audit.exchange",
      "QueueName": "audit.queue",
      "RoutingKey": "audit.log"
    },
    "Elasticsearch": {
      "Urls": ["http://localhost:9200"],
      "IndexName": "auditlogs",
      "IndexPrefix": "codespirit"
    },
    "LLMAudit": {
      "Enabled": true,
      "LogPrompts": true,
      "LogResponses": true,
      "LogProcessedData": false,
      "MaxPromptLength": 10000,
      "MaxResponseLength": 50000,
      "RabbitMQ": {
        "ExchangeName": "llm.audit.exchange",
        "QueueName": "llm.audit.queue",
        "RoutingKey": "llm.audit.log"
      },
      "Elasticsearch": {
        "IndexName": "llm_audit_logs",
        "IndexPrefix": "codespirit",
        "NumberOfShards": 3,
        "NumberOfReplicas": 1
      },
      "GreptimeDB": {
        "TableName": "llm_audit_logs",
        "TablePrefix": "codespirit",
        "BatchSize": 500
      },
      "SensitiveData": {
        "Enabled": true,
        "SensitiveFieldPatterns": [
          "password", "apiKey", "token", "secret",
          "personalInfo", "idCard", "phone", "email"
        ],
        "MaskCharacter": "*",
        "KeepFirstChars": 0,
        "KeepLastChars": 0
      },
      "CostCalculation": {
        "Enabled": true,
        "ModelPricing": {
          "gpt-4": {
            "InputPer1K": 0.03,
            "OutputPer1K": 0.06
          },
          "gpt-3.5-turbo": {
            "InputPer1K": 0.0015,
            "OutputPer1K": 0.002
          },
          "qwen-plus": {
            "InputPer1K": 0.004,
            "OutputPer1K": 0.012
          },
          "qwen-turbo": {
            "InputPer1K": 0.002,
            "OutputPer1K": 0.006
          }
        }
      },
      "ScenarioMapping": {
        "QuestionGeneration": "题目生成",
        "QuestionAudit": "题目审核",
        "QuestionCorrection": "题目校正",
        "ContentGeneration": "内容生成"
      }
    }
  }
}
```

**关键点说明**：

1. **统一存储配置**：`Audit.StorageProvider` 控制所有审计（包括LLM审计）的存储后端
2. **独立的消息队列**：LLM审计使用独立的RabbitMQ队列，便于监控和调优
3. **专用索引/表**：LLM审计使用专用的索引或表，便于管理和查询
4. **业务场景映射**：根据实际使用场景配置业务场景识别

---

## 服务实现

### 1. LLMAuditService（核心服务）

```csharp
namespace CodeSpirit.Audit.LLM.Services.Implementation;

/// <summary>
/// LLM审计服务实现
/// </summary>
public class LLMAuditService : ILLMAuditService
{
    private readonly IRabbitMQService _rabbitMQService;
    private readonly ILLMAuditStorageService _storageService;
    private readonly ILogger<LLMAuditService> _logger;
    private readonly LLMAuditOptions _options;
    private readonly ITenantContext? _tenantContext;
    
    public LLMAuditService(
        IRabbitMQService rabbitMQService,
        ILLMAuditStorageService storageService,
        ILogger<LLMAuditService> logger,
        IOptions<AuditOptions> auditOptions,
        ITenantContext? tenantContext = null)
    {
        _rabbitMQService = rabbitMQService;
        _storageService = storageService;
        _logger = logger;
        _options = auditOptions.Value.LLMAudit;
        _tenantContext = tenantContext;
    }
    
    public async Task LogLLMInteractionAsync(LLMAuditLog auditLog)
    {
        if (!_options.Enabled)
        {
            return;
        }
        
        try
        {
            // 填充租户信息
            if (string.IsNullOrEmpty(auditLog.TenantId) && _tenantContext != null)
            {
                auditLog.TenantId = _tenantContext.TenantId ?? "default";
            }
            
            // 敏感数据脱敏
            if (_options.SensitiveData.Enabled)
            {
                auditLog = MaskSensitiveData(auditLog);
            }
            
            // 截断过长内容
            auditLog.UserPrompt = TruncateString(auditLog.UserPrompt, _options.MaxPromptLength);
            auditLog.SystemPrompt = TruncateString(auditLog.SystemPrompt, _options.MaxPromptLength);
            auditLog.LLMResponse = TruncateString(auditLog.LLMResponse, _options.MaxResponseLength);
            
            // 计算成本
            if (_options.CostCalculation.Enabled)
            {
                auditLog.CostUsd = CalculateCost(auditLog);
            }
            
            // 发送到消息队列
            await _rabbitMQService.PublishAsync(
                _options.RabbitMQ.ExchangeName,
                _options.RabbitMQ.RoutingKey,
                auditLog);
            
            _logger.LogInformation(
                "LLM审计已记录: Type={InteractionType}, Model={ModelName}, Tokens={Tokens}, Success={Success}",
                auditLog.InteractionType, auditLog.ModelName, 
                auditLog.TokenUsage.TotalTokens, auditLog.IsSuccess);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "记录LLM审计失败");
        }
    }
    
    public async Task<(IEnumerable<LLMAuditLog> Items, long Total)> SearchAsync(LLMAuditQueryDto query)
    {
        return await _storageService.SearchAsync(query);
    }
    
    // ... 其他方法实现 ...
    
    private LLMAuditLog MaskSensitiveData(LLMAuditLog auditLog)
    {
        // 实现敏感数据脱敏逻辑（复用审计组件的脱敏工具）
        // TODO: 实现
        return auditLog;
    }
    
    private string TruncateString(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
        {
            return value;
        }
        
        return value.Substring(0, maxLength) + "... [截断]";
    }
    
    private decimal? CalculateCost(LLMAuditLog auditLog)
    {
        if (!_options.CostCalculation.ModelPricing.TryGetValue(auditLog.ModelName, out var pricing))
        {
            return null;
        }
        
        var inputCost = (auditLog.TokenUsage.InputTokens / 1000m) * pricing.InputPer1K;
        var outputCost = (auditLog.TokenUsage.OutputTokens / 1000m) * pricing.OutputPer1K;
        
        return inputCost + outputCost;
    }
}
```

### 2. LLM审计消费者服务

```csharp
namespace CodeSpirit.Audit.LLM.Services.Implementation;

/// <summary>
/// LLM审计日志消费者服务
/// </summary>
public class LLMAuditConsumerService : BackgroundService
{
    private readonly IRabbitMQService _rabbitMQService;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LLMAuditConsumerService> _logger;
    private readonly LLMAuditOptions _options;
    private readonly List<LLMAuditLog> _batchBuffer = new();
    private readonly SemaphoreSlim _batchLock = new(1, 1);
    private Timer? _flushTimer;
    
    public LLMAuditConsumerService(
        IRabbitMQService rabbitMQService,
        IServiceProvider serviceProvider,
        ILogger<LLMAuditConsumerService> logger,
        IOptions<AuditOptions> auditOptions)
    {
        _rabbitMQService = rabbitMQService;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = auditOptions.Value.LLMAudit;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("LLM审计已禁用，消费者服务不启动");
            return;
        }
        
        _logger.LogInformation("LLM审计消费者服务启动");
        
        // 启动定时刷新
        _flushTimer = new Timer(
            async _ => await FlushBatchAsync(),
            null,
            TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(10));
        
        await _rabbitMQService.ConsumeAsync<LLMAuditLog>(
            _options.RabbitMQ.QueueName,
            async auditLog => await ProcessAuditLogAsync(auditLog),
            stoppingToken);
    }
    
    private async Task ProcessAuditLogAsync(LLMAuditLog auditLog)
    {
        await _batchLock.WaitAsync();
        try
        {
            _batchBuffer.Add(auditLog);
            
            // 达到批次大小时立即刷新
            if (_batchBuffer.Count >= 100)
            {
                await FlushBatchInternalAsync();
            }
        }
        finally
        {
            _batchLock.Release();
        }
    }
    
    private async Task FlushBatchAsync()
    {
        await _batchLock.WaitAsync();
        try
        {
            await FlushBatchInternalAsync();
        }
        finally
        {
            _batchLock.Release();
        }
    }
    
    private async Task FlushBatchInternalAsync()
    {
        if (_batchBuffer.Count == 0)
        {
            return;
        }
        
        using var scope = _serviceProvider.CreateScope();
        var storageService = scope.ServiceProvider.GetRequiredService<ILLMAuditStorageService>();
        
        try
        {
            var logsToFlush = _batchBuffer.ToList();
            _batchBuffer.Clear();
            
            var success = await storageService.BulkStoreAsync(logsToFlush);
            
            if (success)
            {
                _logger.LogInformation("批量存储LLM审计日志成功: {Count}条", logsToFlush.Count);
            }
            else
            {
                _logger.LogWarning("批量存储LLM审计日志失败: {Count}条", logsToFlush.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刷新LLM审计批次失败");
        }
    }
    
    public override void Dispose()
    {
        _flushTimer?.Dispose();
        base.Dispose();
    }
}
```

---

## 存储适配

### 1. Elasticsearch存储实现

```csharp
namespace CodeSpirit.Audit.LLM.Services.Implementation;

/// <summary>
/// LLM审计Elasticsearch存储服务
/// </summary>
public class LLMElasticsearchStorageService : ILLMAuditStorageService
{
    private readonly IElasticsearchService _elasticsearchService;
    private readonly ITenantContext? _tenantContext;
    private readonly ILogger<LLMElasticsearchStorageService> _logger;
    private readonly LLMElasticsearchOptions _options;
    
    public LLMElasticsearchStorageService(
        IElasticsearchService elasticsearchService,
        ITenantContext? tenantContext,
        ILogger<LLMElasticsearchStorageService> logger,
        IOptions<AuditOptions> auditOptions)
    {
        _elasticsearchService = elasticsearchService;
        _tenantContext = tenantContext;
        _logger = logger;
        _options = auditOptions.Value.LLMAudit.Elasticsearch;
    }
    
    public async Task<bool> InitializeAsync()
    {
        // 创建索引（如果不存在）
        var indexName = GetIndexName();
        
        // TODO: 使用ElasticsearchClient创建索引，包含LLMAuditLog的字段映射
        _logger.LogInformation("初始化LLM审计索引: {IndexName}", indexName);
        
        return true;
    }
    
    public async Task<bool> StoreAsync(LLMAuditLog auditLog)
    {
        try
        {
            // TODO: 实现单条存储
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "存储LLM审计日志失败");
            return false;
        }
    }
    
    public async Task<bool> BulkStoreAsync(IEnumerable<LLMAuditLog> auditLogs)
    {
        try
        {
            // TODO: 实现批量存储
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量存储LLM审计日志失败");
            return false;
        }
    }
    
    public async Task<(IEnumerable<LLMAuditLog> Items, long Total)> SearchAsync(LLMAuditQueryDto query)
    {
        // TODO: 实现搜索
        return (Enumerable.Empty<LLMAuditLog>(), 0);
    }
    
    private string GetIndexName()
    {
        if (string.IsNullOrWhiteSpace(_options.IndexPrefix))
        {
            return _options.IndexName;
        }
        
        return $"{_options.IndexPrefix}_{_options.IndexName}";
    }
}
```

### 2. GreptimeDB存储实现

```csharp
namespace CodeSpirit.Audit.LLM.Services.Implementation;

/// <summary>
/// LLM审计GreptimeDB存储服务
/// </summary>
public class LLMGreptimeDbStorageService : ILLMAuditStorageService
{
    private readonly HttpClient _httpClient;
    private readonly ITenantContext? _tenantContext;
    private readonly ILogger<LLMGreptimeDbStorageService> _logger;
    private readonly LLMGreptimeDbOptions _options;
    private readonly GreptimeDbOptions _greptimeDbConfig;
    
    public LLMGreptimeDbStorageService(
        HttpClient httpClient,
        ITenantContext? tenantContext,
        ILogger<LLMGreptimeDbStorageService> logger,
        IOptions<AuditOptions> auditOptions,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _tenantContext = tenantContext;
        _logger = logger;
        _options = auditOptions.Value.LLMAudit.GreptimeDB;
        _greptimeDbConfig = configuration.GetSection("Audit:GreptimeDB").Get<GreptimeDbOptions>() 
                          ?? new GreptimeDbOptions();
    }
    
    public async Task<bool> InitializeAsync()
    {
        // 创建表（如果不存在）
        var tableName = GetTableName();
        
        var createTableSql = $@"
            CREATE TABLE IF NOT EXISTS {tableName} (
                id STRING,
                tenant_id STRING,
                user_id STRING,
                user_name STRING,
                operation_time TIMESTAMP TIME INDEX,
                llm_provider STRING,
                model_name STRING,
                interaction_type STRING,
                business_scenario STRING,
                system_prompt STRING,
                user_prompt STRING,
                llm_response STRING,
                processed_data STRING,
                input_tokens BIGINT,
                output_tokens BIGINT,
                total_tokens BIGINT,
                processing_time_ms BIGINT,
                cost_usd DOUBLE,
                is_success BOOLEAN,
                error_message STRING,
                retry_count INT,
                was_json_repaired BOOLEAN,
                quality_score INT,
                PRIMARY KEY(id, operation_time)
            )";
        
        // TODO: 执行创建表SQL
        _logger.LogInformation("初始化LLM审计表: {TableName}", tableName);
        
        return true;
    }
    
    // ... 其他方法实现 ...
    
    private string GetTableName()
    {
        if (string.IsNullOrWhiteSpace(_options.TablePrefix))
        {
            return _options.TableName;
        }
        
        return $"{_options.TablePrefix}_{_options.TableName}";
    }
}
```

### 3. 服务注册扩展

在 `AuditExtensions.cs` 中添加：

```csharp
/// <summary>
/// 添加LLM审计服务
/// </summary>
public static IServiceCollection AddLLMAuditServices(
    this IServiceCollection services, 
    IConfiguration configuration)
{
    // 获取审计配置，LLM审计跟随统一的存储提供者配置
    var auditConfig = configuration.GetSection("Audit");
    var storageProvider = auditConfig.GetValue<string>("StorageProvider") 
                        ?? configuration.GetValue<string>("Audit:StorageProvider")
                        ?? "Elasticsearch";
    
    Console.WriteLine($"[LLM审计配置] 跟随通用审计存储提供者: '{storageProvider}'");
    
    // 根据配置注册存储服务
    switch (storageProvider.ToLowerInvariant())
    {
        case "greptimedb":
            Console.WriteLine("[LLM审计配置] 使用GreptimeDB存储提供者");
            services.AddHttpClient<LLMGreptimeDbStorageService>();
            services.AddScoped<ILLMAuditStorageService, LLMGreptimeDbStorageService>();
            break;
        
        case "elasticsearch":
        default:
            Console.WriteLine("[LLM审计配置] 使用Elasticsearch存储提供者");
            services.AddScoped<ILLMAuditStorageService, LLMElasticsearchStorageService>();
            break;
    }
    
    // 注册LLM审计服务
    services.AddScoped<ILLMAuditService, LLMAuditService>();
    
    // 注册LLM审计消费者后台服务
    services.AddHostedService<LLMAuditConsumerService>();
    
    return services;
}
```

**关键改进**：
1. 移除了独立的存储提供者配置，统一使用 `Audit.StorageProvider`
2. 简化了配置查找逻辑
3. 保持与通用审计组件的一致性

---

## 查询服务

### 1. LLM审计查询控制器

```csharp
namespace CodeSpirit.Web.Controllers;

/// <summary>
/// LLM审计查询控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[DisplayName("LLM审计")]
public class LLMAuditController : ControllerBase
{
    private readonly ILLMAuditService _auditService;
    private readonly ILogger<LLMAuditController> _logger;
    
    public LLMAuditController(
        ILLMAuditService auditService,
        ILogger<LLMAuditController> logger)
    {
        _auditService = auditService;
        _logger = logger;
    }
    
    /// <summary>
    /// 查询LLM审计日志
    /// </summary>
    [HttpGet]
    [DisplayName("查询审计日志")]
    public async Task<ActionResult<ApiResponse<PagedResult<LLMAuditLog>>>> GetAuditLogs(
        [FromQuery] LLMAuditQueryDto query)
    {
        try
        {
            var (items, total) = await _auditService.SearchAsync(query);
            
            var result = new PagedResult<LLMAuditLog>
            {
                Items = items.ToList(),
                Total = total,
                Page = query.Page,
                PageSize = query.PageSize
            };
            
            return ApiResponse<PagedResult<LLMAuditLog>>.SuccessResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询LLM审计日志失败");
            return ApiResponse<PagedResult<LLMAuditLog>>.ErrorResult("查询失败");
        }
    }
    
    /// <summary>
    /// 获取LLM使用统计
    /// </summary>
    [HttpGet("stats/usage")]
    [DisplayName("使用统计")]
    public async Task<ActionResult<ApiResponse<LLMUsageStatsDto>>> GetUsageStats(
        [FromQuery] DateTime? startTime,
        [FromQuery] DateTime? endTime,
        [FromQuery] string? tenantId)
    {
        try
        {
            var start = startTime ?? DateTime.UtcNow.AddDays(-7);
            var end = endTime ?? DateTime.UtcNow;
            
            var stats = await _auditService.GetUsageStatsAsync(start, end, tenantId);
            
            return ApiResponse<LLMUsageStatsDto>.SuccessResult(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取LLM使用统计失败");
            return ApiResponse<LLMUsageStatsDto>.ErrorResult("获取统计失败");
        }
    }
    
    /// <summary>
    /// 获取LLM成本统计
    /// </summary>
    [HttpGet("stats/cost")]
    [DisplayName("成本统计")]
    public async Task<ActionResult<ApiResponse<LLMCostStatsDto>>> GetCostStats(
        [FromQuery] DateTime? startTime,
        [FromQuery] DateTime? endTime,
        [FromQuery] string? tenantId)
    {
        try
        {
            var start = startTime ?? DateTime.UtcNow.AddDays(-7);
            var end = endTime ?? DateTime.UtcNow;
            
            var stats = await _auditService.GetCostStatsAsync(start, end, tenantId);
            
            return ApiResponse<LLMCostStatsDto>.SuccessResult(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取LLM成本统计失败");
            return ApiResponse<LLMCostStatsDto>.ErrorResult("获取统计失败");
        }
    }
    
    /// <summary>
    /// 获取LLM质量统计
    /// </summary>
    [HttpGet("stats/quality")]
    [DisplayName("质量统计")]
    public async Task<ActionResult<ApiResponse<LLMQualityStatsDto>>> GetQualityStats(
        [FromQuery] DateTime? startTime,
        [FromQuery] DateTime? endTime,
        [FromQuery] string? tenantId)
    {
        try
        {
            var start = startTime ?? DateTime.UtcNow.AddDays(-7);
            var end = endTime ?? DateTime.UtcNow;
            
            var stats = await _auditService.GetQualityStatsAsync(start, end, tenantId);
            
            return ApiResponse<LLMQualityStatsDto>.SuccessResult(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取LLM质量统计失败");
            return ApiResponse<LLMQualityStatsDto>.ErrorResult("获取统计失败");
        }
    }
    
    /// <summary>
    /// 获取LLM使用趋势
    /// </summary>
    [HttpGet("trends")]
    [DisplayName("使用趋势")]
    public async Task<ActionResult<ApiResponse<Dictionary<DateTime, long>>>> GetUsageTrend(
        [FromQuery] DateTime? startTime,
        [FromQuery] DateTime? endTime,
        [FromQuery] int intervalHours = 24)
    {
        try
        {
            var start = startTime ?? DateTime.UtcNow.AddDays(-7);
            var end = endTime ?? DateTime.UtcNow;
            
            var trend = await _auditService.GetUsageTrendAsync(start, end, intervalHours);
            
            return ApiResponse<Dictionary<DateTime, long>>.SuccessResult(trend);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取LLM使用趋势失败");
            return ApiResponse<Dictionary<DateTime, long>>.ErrorResult("获取趋势失败");
        }
    }
}
```

---

## 实际使用场景分析

基于当前工程中的实际使用情况，LLM主要应用于以下场景：

### 1. 题目生成场景（AIQuestionGeneratorService）

**特点**：
- 单次调用可能生成多个题目
- 支持重试机制和格式修正
- 有详细的进度回调
- 包含题目内容生成和格式修正两种交互类型

**审计要点**：
- 记录每次生成请求的提示词
- 记录LLM返回的原始内容
- 记录重试次数和修正次数
- 区分"内容生成"和"格式修正"两种交互类型
- 关联生成的题目数量和质量

### 2. 题目审核场景（QuestionService）

**特点**：
- 批量审核题目（每批最多10道）
- 支持自动校正功能
- 返回结构化的审核结果
- 包含错误检测和修正建议

**审计要点**：
- 记录审核的题目内容
- 记录AI返回的审核结果
- 记录是否进行了自动校正
- 记录JSON解析和修复过程
- 统计审核通过率和常见错误

### 3. 通用内容生成场景

**特点**：
- 简单的单次调用
- 不涉及复杂的业务逻辑

**审计要点**：
- 基础的提示词和响应记录
- Token使用统计

### 优化建议

根据实际使用场景，我们做出以下优化：

#### 1. 增强交互类型识别

```csharp
/// <summary>
/// LLM交互类型枚举
/// </summary>
public enum LLMInteractionType
{
    /// <summary>
    /// 题目生成
    /// </summary>
    QuestionGeneration,
    
    /// <summary>
    /// 格式修正
    /// </summary>
    FormatCorrection,
    
    /// <summary>
    /// 题目审核
    /// </summary>
    QuestionAudit,
    
    /// <summary>
    /// 题目校正
    /// </summary>
    QuestionCorrection,
    
    /// <summary>
    /// 通用内容生成
    /// </summary>
    ContentGeneration
}
```

#### 2. 添加批次关联

对于批量处理场景（如题目审核），添加批次ID关联：

```csharp
/// <summary>
/// 批次ID（用于关联同一批次的多个LLM调用）
/// </summary>
[DisplayName("批次ID")]
[StringLength(50)]
public string? BatchId { get; set; }

/// <summary>
/// 批次序号（在批次中的序号）
/// </summary>
[DisplayName("批次序号")]
public int? BatchSequence { get; set; }

/// <summary>
/// 父审计ID（用于关联重试和修正操作）
/// </summary>
[DisplayName("父审计ID")]
[StringLength(50)]
public string? ParentAuditId { get; set; }
```

#### 3. 记录业务关联信息

```csharp
/// <summary>
/// 关联的业务实体ID（如题目ID、试卷ID等）
/// </summary>
[DisplayName("业务实体ID")]
[StringLength(50)]
public string? BusinessEntityId { get; set; }

/// <summary>
/// 关联的业务实体类型（Question、Exam等）
/// </summary>
[DisplayName("业务实体类型")]
[StringLength(50)]
public string? BusinessEntityType { get; set; }

/// <summary>
/// 处理的数据量（如生成的题目数、审核的题目数）
/// </summary>
[DisplayName("数据量")]
public int? DataCount { get; set; }
```

---

## 集成方案

### 1. 增强LLMAssistant

通过装饰器模式为LLM组件添加审计能力，并支持业务上下文传递：

```csharp
namespace CodeSpirit.LLM;

/// <summary>
/// 增强的LLM助手（带审计功能）
/// </summary>
public class AuditableLLMAssistant : LLMAssistant
{
    private readonly ILLMAuditService _auditService;
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private readonly ITenantContext? _tenantContext;
    
    public AuditableLLMAssistant(
        ILLMClientFactory llmClientFactory,
        ILogger<LLMAssistant> logger,
        ILLMJsonProcessor jsonProcessor,
        ILLMBatchProcessor batchProcessor,
        ILLMPromptBuilder promptBuilder,
        ILLMAuditService auditService,
        IHttpContextAccessor? httpContextAccessor = null,
        ITenantContext? tenantContext = null)
        : base(llmClientFactory, logger, jsonProcessor, batchProcessor, promptBuilder)
    {
        _auditService = auditService;
        _httpContextAccessor = httpContextAccessor;
        _tenantContext = tenantContext;
    }
    
    public override async Task<string> GenerateContentAsync(string prompt)
    {
        return await GenerateContentWithAuditAsync(
            "", 
            prompt, 
            "Generate", 
            "Generic",
            base.GenerateContentAsync);
    }
    
    public override async Task<string> GenerateContentAsync(string systemPrompt, string userPrompt)
    {
        return await GenerateContentWithAuditAsync(
            systemPrompt, 
            userPrompt, 
            "Generate", 
            "Generic",
            () => base.GenerateContentAsync(systemPrompt, userPrompt));
    }
    
    public override async Task<StructuredTaskResult<T>> ProcessStructuredTaskAsync<T>(
        string prompt, 
        StructuredTaskOptions? options = null)
    {
        var startTime = DateTime.UtcNow;
        var result = await base.ProcessStructuredTaskAsync<T>(prompt, options);
        
        // 记录审计
        await LogStructuredTaskAuditAsync(
            "",
            prompt,
            result,
            "StructuredTask",
            typeof(T).Name,
            startTime);
        
        return result;
    }
    
    private async Task<string> GenerateContentWithAuditAsync(
        string systemPrompt,
        string userPrompt,
        string interactionType,
        string businessScenario,
        Func<Task<string>> generateFunc)
    {
        var startTime = DateTime.UtcNow;
        var success = false;
        var response = "";
        var errorMessage = "";
        
        try
        {
            response = await generateFunc();
            success = true;
            return response;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            throw;
        }
        finally
        {
            var endTime = DateTime.UtcNow;
            
            // 记录审计
            await _auditService.LogLLMInteractionAsync(new LLMAuditLog
            {
                TenantId = GetTenantId(),
                UserId = GetUserId(),
                UserName = GetUserName(),
                OperationTime = startTime,
                LLMProvider = "Aliyun", // TODO: 从配置获取
                ModelName = "qwen-plus", // TODO: 从配置获取
                InteractionType = interactionType,
                BusinessScenario = businessScenario,
                SystemPrompt = systemPrompt,
                UserPrompt = userPrompt,
                LLMResponse = response,
                TokenUsage = EstimateTokens(systemPrompt + userPrompt, response),
                ProcessingTimeMs = (long)(endTime - startTime).TotalMilliseconds,
                IsSuccess = success,
                ErrorMessage = errorMessage
            });
        }
    }
    
    private async Task LogStructuredTaskAuditAsync<T>(
        string systemPrompt,
        string userPrompt,
        StructuredTaskResult<T> result,
        string interactionType,
        string businessScenario,
        DateTime startTime) where T : class
    {
        await _auditService.LogLLMInteractionAsync(new LLMAuditLog
        {
            TenantId = GetTenantId(),
            UserId = GetUserId(),
            UserName = GetUserName(),
            OperationTime = startTime,
            LLMProvider = "Aliyun",
            ModelName = "qwen-plus",
            InteractionType = interactionType,
            BusinessScenario = businessScenario,
            SystemPrompt = systemPrompt,
            UserPrompt = userPrompt,
            LLMResponse = result.RawResponse,
            ProcessedData = result.CleanedJson,
            TokenUsage = EstimateTokens(userPrompt, result.RawResponse),
            ProcessingTimeMs = (long)result.Duration.TotalMilliseconds,
            IsSuccess = result.IsSuccess,
            ErrorMessage = result.IsSuccess ? "" : string.Join("; ", result.Errors),
            WasJsonRepaired = result.WasRepaired
        });
    }
    
    private string GetTenantId()
    {
        return _tenantContext?.TenantId 
            ?? _httpContextAccessor?.HttpContext?.User?.FindFirst("TenantId")?.Value 
            ?? "default";
    }
    
    private string GetUserId()
    {
        return _httpContextAccessor?.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value 
            ?? "system";
    }
    
    private string GetUserName()
    {
        return _httpContextAccessor?.HttpContext?.User?.FindFirst(ClaimTypes.Name)?.Value 
            ?? "System";
    }
    
    private LLMTokenUsage EstimateTokens(string input, string output)
    {
        // 简单估算：中文约1.5字符=1token，英文约4字符=1token
        var inputTokens = (int)(input.Length / 2.5);
        var outputTokens = (int)(output.Length / 2.5);
        
        return new LLMTokenUsage
        {
            InputTokens = inputTokens,
            OutputTokens = outputTokens,
            TotalTokens = inputTokens + outputTokens
        };
    }
}
```

### 2. 服务注册

在 `ServiceCollectionExtensions.cs` 中扩展：

```csharp
namespace CodeSpirit.LLM;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 添加LLM服务（带审计支持）
    /// </summary>
    public static IServiceCollection AddLLMServicesWithAudit(this IServiceCollection services)
    {
        // 注册基础LLM服务
        services.AddLLMServices();
        
        // 检查是否已注册审计服务
        var hasAuditService = services.Any(x => x.ServiceType == typeof(ILLMAuditService));
        
        if (hasAuditService)
        {
            // 注册增强的LLMAssistant（带审计）
            services.AddScoped<LLMAssistant, AuditableLLMAssistant>();
        }
        else
        {
            // 使用默认的LLMAssistant（无审计）
            services.AddScoped<LLMAssistant>();
        }
        
        return services;
    }
}
```

---

## 使用示例

### 1. 配置和启动

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// 添加审计服务（包含通用审计和LLM审计）
builder.Services.AddAuditServices(builder.Configuration);
builder.Services.AddLLMAuditServices(builder.Configuration);

// 添加LLM服务（带审计支持）
builder.Services.AddLLMServicesWithAudit();

// 添加HttpContextAccessor（用于获取当前用户信息）
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// 使用审计中间件
app.UseAudit();

app.Run();
```

### 2. 业务代码使用

```csharp
public class QuestionService
{
    private readonly LLMAssistant _llmAssistant;
    
    public QuestionService(LLMAssistant llmAssistant)
    {
        _llmAssistant = llmAssistant;
    }
    
    public async Task<QuestionDto> AuditQuestionAsync(QuestionDto question)
    {
        // 使用LLM审核题目（自动记录审计）
        var prompt = $"请审核以下题目：{question.Content}";
        var result = await _llmAssistant.GenerateContentAsync(prompt);
        
        // LLM交互已自动记录到审计系统
        return question;
    }
}
```

### 3. 查询审计日志

```http
GET /api/llmaudit?page=1&pageSize=20&modelName=qwen-plus&startTime=2025-01-01
```

### 4. 获取统计信息

```http
GET /api/llmaudit/stats/usage?startTime=2025-01-01&endTime=2025-01-09
```

---

## 性能优化

### 1. 批量处理

- 审计消息通过RabbitMQ异步处理
- 消费者服务批量写入存储（100条/批次或10秒间隔）
- 减少存储I/O次数

### 2. 索引优化

**Elasticsearch索引策略**：
- 按月创建索引（如：`codespirit_llm_audit_logs_2025_01`）
- 主要查询字段建立索引：`tenant_id`, `model_name`, `operation_time`
- 使用分片和副本提高并发查询能力

### 3. 数据截断

- 提示词最大10000字符
- 响应最大50000字符
- 超长内容自动截断

### 4. 敏感数据脱敏

- 使用正则表达式匹配敏感字段
- 脱敏处理在发送到消息队列前完成
- 避免敏感数据进入存储系统

---

## 监控与运维

### 1. 健康检查

```csharp
builder.Services.AddHealthChecks()
    .AddCheck<LLMAuditHealthCheck>("llm_audit");
```

### 2. 指标监控

- **审计记录速率**：records/second
- **存储延迟**：平均写入耗时
- **队列深度**：RabbitMQ队列消息数
- **失败率**：记录失败的比例

### 3. 告警规则

- 队列深度 > 10000：告警（可能消费者处理能力不足）
- 失败率 > 5%：告警（可能存储服务异常）
- 存储延迟 > 5s：告警（可能存储性能问题）

### 4. 数据清理

定期清理过期审计数据（建议保留3-6个月）：

```csharp
// 定时任务：每天凌晨2点执行
public class LLMAuditCleanupJob : IHostedService
{
    public async Task ExecuteAsync()
    {
        var cutoffDate = DateTime.UtcNow.AddMonths(-6);
        // 删除6个月前的数据
        await _storageService.DeleteOldRecordsAsync(cutoffDate);
    }
}
```

---

## 总结

本方案通过以下设计实现了完整的LLM审计能力：

### 核心特性

1. **独立数据模型**：专用的LLMAuditLog模型，字段更贴合LLM场景
2. **复用基础设施**：充分利用现有审计组件的RabbitMQ、存储适配等能力
3. **统一存储配置**：跟随 `Audit.StorageProvider`，避免配置冗余
4. **多存储支持**：支持Elasticsearch和GreptimeDB，可灵活选择
5. **低侵入集成**：通过装饰器模式，对现有LLM组件影响最小
6. **完整查询服务**：提供丰富的查询、统计、趋势分析API

### 针对实际场景的优化

基于当前工程中的实际使用情况，本方案特别优化了以下场景：

#### 1. 题目生成场景优化
- 支持批次关联（`BatchId`、`BatchSequence`）
- 区分"内容生成"和"格式修正"交互类型
- 记录重试次数和修正过程
- 关联生成的题目数量（`DataCount`）

#### 2. 题目审核场景优化
- 支持父子审计关联（`ParentAuditId`）
- 记录JSON修复过程（`WasJsonRepaired`）
- 记录审核的题目数量
- 统计审核通过率

#### 3. 业务关联优化
- 支持业务实体关联（`BusinessEntityId`、`BusinessEntityType`）
- 便于追溯LLM审计与业务数据的关系
- 支持业务级别的审计查询和统计

### 配置简化

**关键改进**：
- ✅ 移除独立的 `LLMAudit.StorageProvider` 配置
- ✅ 统一使用 `Audit.StorageProvider`
- ✅ 简化配置结构，减少维护成本
- ✅ 保持与通用审计的一致性

### 实施优势

1. **开发效率**：
   - 70%+ 代码复用率
   - 统一的配置和服务注册模式
   - 完善的文档和示例

2. **维护成本**：
   - 统一的存储配置管理
   - 一致的服务架构
   - 清晰的代码结构

3. **扩展性**：
   - 支持新的LLM提供商
   - 支持新的业务场景
   - 灵活的查询和统计能力

4. **性能**：
   - 异步消息队列处理
   - 批量写入存储
   - 专用索引优化

### 待确认事项

1. ✅ 数据模型设计是否满足业务需求？
   - 已添加批次关联、业务实体关联等实际场景所需字段
   
2. ✅ 配置结构是否合理？
   - 已简化为跟随统一的存储提供者配置
   
3. ✅ 服务注册和依赖注入方式是否正确？
   - 已参照现有审计组件的模式设计
   
4. ✅ 存储适配方案是否完整？
   - 已支持Elasticsearch和GreptimeDB两种存储
   
5. ✅ 查询API设计是否满足需求？
   - 已提供完整的查询、统计、趋势分析API

### 下一步工作

确认方案后，将按以下顺序实施：

1. **Phase 1 - 核心模型和配置**（优先级：高）
   - 创建 `LLMAuditLog` 数据模型
   - 扩展 `AuditOptions` 配置
   - 定义服务接口

2. **Phase 2 - 存储适配**（优先级：高）
   - 实现Elasticsearch存储服务
   - 实现GreptimeDB存储服务
   - 实现服务注册扩展

3. **Phase 3 - 审计服务**（优先级：中）
   - 实现 `LLMAuditService`
   - 实现 `LLMAuditConsumerService`
   - 实现敏感数据脱敏

4. **Phase 4 - LLM集成**（优先级：中）
   - 实现 `AuditableLLMAssistant`
   - 集成到现有LLM组件
   - 测试审计功能

5. **Phase 5 - 查询服务**（优先级：低）
   - 实现查询API控制器
   - 实现统计和趋势分析
   - 完善文档和示例

请审阅本方案，确认后我将开始实施编码工作。

