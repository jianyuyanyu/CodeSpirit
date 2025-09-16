using CodeSpirit.ApprovalApi.Models;

namespace CodeSpirit.ApprovalApi.Services;

/// <summary>
/// 智能审批服务接口
/// </summary>
public interface IIntelligentApprovalService : IScopedDependency
{
    /// <summary>
    /// 风险识别
    /// </summary>
    /// <param name="instanceId">审批实例ID</param>
    /// <param name="businessData">业务数据</param>
    /// <param name="workflowCode">工作流代码</param>
    /// <returns>风险评估结果</returns>
    Task<RiskAssessmentResult> AssessRiskAsync(long instanceId, object businessData, string workflowCode);

    /// <summary>
    /// 智能审批建议
    /// </summary>
    /// <param name="instanceId">审批实例ID</param>
    /// <param name="taskId">任务ID</param>
    /// <param name="businessData">业务数据</param>
    /// <param name="historicalData">历史审批数据</param>
    /// <returns>智能审批建议</returns>
    Task<IntelligentApprovalSuggestion> GetApprovalSuggestionAsync(
        long instanceId, 
        long taskId, 
        object businessData, 
        List<ApprovalHistoryData> historicalData);

    /// <summary>
    /// 异常检测
    /// </summary>
    /// <param name="businessData">业务数据</param>
    /// <param name="entityType">业务实体类型</param>
    /// <returns>异常检测结果</returns>
    Task<AnomalyDetectionResult> DetectAnomaliesAsync(object businessData, string entityType);

    /// <summary>
    /// 合规性检查
    /// </summary>
    /// <param name="businessData">业务数据</param>
    /// <param name="workflowCode">工作流代码</param>
    /// <returns>合规性检查结果</returns>
    Task<ComplianceCheckResult> CheckComplianceAsync(object businessData, string workflowCode);
}

/// <summary>
/// 风险评估结果
/// </summary>
public class RiskAssessmentResult
{
    /// <summary>
    /// 风险等级
    /// </summary>
    [DisplayName("风险等级")]
    public RiskLevel RiskLevel { get; set; } = RiskLevel.Low;

    /// <summary>
    /// 风险分数（0-100）
    /// </summary>
    [DisplayName("风险分数")]
    public int RiskScore { get; set; }

    /// <summary>
    /// 风险因子列表
    /// </summary>
    [DisplayName("风险因子")]
    public List<RiskFactor> RiskFactors { get; set; } = new();

    /// <summary>
    /// 风险描述
    /// </summary>
    [DisplayName("风险描述")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 建议措施
    /// </summary>
    [DisplayName("建议措施")]
    public List<string> Recommendations { get; set; } = new();

    /// <summary>
    /// 评估时间
    /// </summary>
    [DisplayName("评估时间")]
    public DateTime AssessmentTime { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 智能审批建议
/// </summary>
public class IntelligentApprovalSuggestion
{
    /// <summary>
    /// 建议结果
    /// </summary>
    [DisplayName("建议结果")]
    public ApprovalResult SuggestedResult { get; set; }

    /// <summary>
    /// 置信度（0-1）
    /// </summary>
    [DisplayName("置信度")]
    public double Confidence { get; set; }

    /// <summary>
    /// 建议理由
    /// </summary>
    [DisplayName("建议理由")]
    public string Reasoning { get; set; } = string.Empty;

    /// <summary>
    /// 参考案例
    /// </summary>
    [DisplayName("参考案例")]
    public List<SimilarCase> SimilarCases { get; set; } = new();

    /// <summary>
    /// 关键指标分析
    /// </summary>
    [DisplayName("关键指标分析")]
    public Dictionary<string, object> KeyMetrics { get; set; } = new();

    /// <summary>
    /// 生成时间
    /// </summary>
    [DisplayName("生成时间")]
    public DateTime GeneratedTime { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 风险等级
/// </summary>
public enum RiskLevel
{
    /// <summary>
    /// 低风险
    /// </summary>
    [Display(Name = "低风险")]
    Low = 1,

    /// <summary>
    /// 中风险
    /// </summary>
    [Display(Name = "中风险")]
    Medium = 2,

    /// <summary>
    /// 高风险
    /// </summary>
    [Display(Name = "高风险")]
    High = 3,

    /// <summary>
    /// 极高风险
    /// </summary>
    [Display(Name = "极高风险")]
    Critical = 4
}

/// <summary>
/// 风险因子
/// </summary>
public class RiskFactor
{
    /// <summary>
    /// 因子名称
    /// </summary>
    [DisplayName("因子名称")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 因子类型
    /// </summary>
    [DisplayName("因子类型")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 风险值
    /// </summary>
    [DisplayName("风险值")]
    public double Value { get; set; }

    /// <summary>
    /// 权重
    /// </summary>
    [DisplayName("权重")]
    public double Weight { get; set; }

    /// <summary>
    /// 描述
    /// </summary>
    [DisplayName("描述")]
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// 相似案例
/// </summary>
public class SimilarCase
{
    /// <summary>
    /// 案例ID
    /// </summary>
    [DisplayName("案例ID")]
    public long CaseId { get; set; }

    /// <summary>
    /// 相似度
    /// </summary>
    [DisplayName("相似度")]
    public double Similarity { get; set; }

    /// <summary>
    /// 案例结果
    /// </summary>
    [DisplayName("案例结果")]
    public ApprovalResult Result { get; set; }

    /// <summary>
    /// 案例描述
    /// </summary>
    [DisplayName("案例描述")]
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// 异常检测结果
/// </summary>
public class AnomalyDetectionResult
{
    /// <summary>
    /// 是否存在异常
    /// </summary>
    [DisplayName("是否存在异常")]
    public bool HasAnomalies { get; set; }

    /// <summary>
    /// 异常列表
    /// </summary>
    [DisplayName("异常列表")]
    public List<Anomaly> Anomalies { get; set; } = new();

    /// <summary>
    /// 异常分数
    /// </summary>
    [DisplayName("异常分数")]
    public double AnomalyScore { get; set; }
}

/// <summary>
/// 异常信息
/// </summary>
public class Anomaly
{
    /// <summary>
    /// 异常类型
    /// </summary>
    [DisplayName("异常类型")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 异常字段
    /// </summary>
    [DisplayName("异常字段")]
    public string Field { get; set; } = string.Empty;

    /// <summary>
    /// 异常值
    /// </summary>
    [DisplayName("异常值")]
    public object Value { get; set; } = new();

    /// <summary>
    /// 期望值
    /// </summary>
    [DisplayName("期望值")]
    public object ExpectedValue { get; set; } = new();

    /// <summary>
    /// 异常描述
    /// </summary>
    [DisplayName("异常描述")]
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// 合规性检查结果
/// </summary>
public class ComplianceCheckResult
{
    /// <summary>
    /// 是否合规
    /// </summary>
    [DisplayName("是否合规")]
    public bool IsCompliant { get; set; }

    /// <summary>
    /// 违规项列表
    /// </summary>
    [DisplayName("违规项列表")]
    public List<ComplianceViolation> Violations { get; set; } = new();

    /// <summary>
    /// 合规分数
    /// </summary>
    [DisplayName("合规分数")]
    public double ComplianceScore { get; set; }
}

/// <summary>
/// 违规项
/// </summary>
public class ComplianceViolation
{
    /// <summary>
    /// 规则名称
    /// </summary>
    [DisplayName("规则名称")]
    public string RuleName { get; set; } = string.Empty;

    /// <summary>
    /// 违规描述
    /// </summary>
    [DisplayName("违规描述")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 严重程度
    /// </summary>
    [DisplayName("严重程度")]
    public ComplianceSeverity Severity { get; set; }
}

/// <summary>
/// 合规严重程度
/// </summary>
public enum ComplianceSeverity
{
    /// <summary>
    /// 信息
    /// </summary>
    [Display(Name = "信息")]
    Info = 1,

    /// <summary>
    /// 警告
    /// </summary>
    [Display(Name = "警告")]
    Warning = 2,

    /// <summary>
    /// 错误
    /// </summary>
    [Display(Name = "错误")]
    Error = 3,

    /// <summary>
    /// 严重错误
    /// </summary>
    [Display(Name = "严重错误")]
    Critical = 4
}

/// <summary>
/// 审批历史数据
/// </summary>
public class ApprovalHistoryData
{
    /// <summary>
    /// 实例ID
    /// </summary>
    public long InstanceId { get; set; }

    /// <summary>
    /// 业务数据
    /// </summary>
    public object BusinessData { get; set; } = new();

    /// <summary>
    /// 审批结果
    /// </summary>
    public ApprovalStatus Result { get; set; }

    /// <summary>
    /// 处理时间
    /// </summary>
    public DateTime ProcessedTime { get; set; }

    /// <summary>
    /// 审批人ID
    /// </summary>
    public string ApproverId { get; set; } = string.Empty;
}
