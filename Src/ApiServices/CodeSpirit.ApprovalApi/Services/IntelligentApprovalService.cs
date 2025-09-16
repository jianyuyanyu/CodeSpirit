using CodeSpirit.ApprovalApi.Data;
using CodeSpirit.ApprovalApi.Models;
using CodeSpirit.LLM.Clients;
using CodeSpirit.LLM.Factories;
using CodeSpirit.Shared.Repositories;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace CodeSpirit.ApprovalApi.Services;

/// <summary>
/// 智能审批服务实现
/// </summary>
public class IntelligentApprovalService : IIntelligentApprovalService
{
    private readonly IRepository<ApprovalInstance> _instanceRepository;
    private readonly ILLMClientFactory _llmClientFactory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<IntelligentApprovalService> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="instanceRepository">审批实例仓储</param>
    /// <param name="llmClientFactory">LLM客户端工厂</param>
    /// <param name="cache">内存缓存</param>
    /// <param name="logger">日志记录器</param>
    public IntelligentApprovalService(
        IRepository<ApprovalInstance> instanceRepository,
        ILLMClientFactory llmClientFactory,
        IMemoryCache cache,
        ILogger<IntelligentApprovalService> logger)
    {
        _instanceRepository = instanceRepository;
        _llmClientFactory = llmClientFactory;
        _cache = cache;
        _logger = logger;
    }
    
    /// <summary>
    /// 获取LLM客户端
    /// </summary>
    /// <returns>LLM客户端</returns>
    private async Task<ILLMClient> GetLLMClientAsync()
    {
        var client = await _llmClientFactory.CreateClientAsync();
        if (client == null)
        {
            throw new InvalidOperationException("无法创建LLM客户端，请检查LLM设置配置");
        }
        return client;
    }

    /// <summary>
    /// 风险识别
    /// </summary>
    /// <param name="instanceId">审批实例ID</param>
    /// <param name="businessData">业务数据</param>
    /// <param name="workflowCode">工作流代码</param>
    /// <returns>风险评估结果</returns>
    public async Task<RiskAssessmentResult> AssessRiskAsync(long instanceId, object businessData, string workflowCode)
    {
        try
        {
            var cacheKey = $"risk_assessment_{instanceId}";
            if (_cache.TryGetValue(cacheKey, out RiskAssessmentResult? cachedResult) && cachedResult != null)
            {
                return cachedResult;
            }

            _logger.LogInformation("开始风险评估: 实例ID={InstanceId}, 工作流={WorkflowCode}", instanceId, workflowCode);

            var systemPrompt = GetRiskAssessmentSystemPrompt();
            var userPrompt = BuildRiskAssessmentPrompt(businessData, workflowCode);
            var llmClient = await GetLLMClientAsync();
            var llmResponse = await llmClient.GenerateContentAsync(systemPrompt, userPrompt);

            var result = ParseRiskAssessmentResponse(llmResponse);

            // 缓存结果30分钟
            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(30));

            _logger.LogInformation("风险评估完成: 实例={InstanceId}, 风险等级={RiskLevel}, 分数={Score}",
                instanceId, result.RiskLevel, result.RiskScore);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "风险评估失败: 实例={InstanceId}", instanceId);

            // 返回默认低风险结果
            return new RiskAssessmentResult
            {
                RiskLevel = RiskLevel.Low,
                RiskScore = 10,
                Description = "风险评估服务暂时不可用，默认为低风险",
                AssessmentTime = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// 智能审批建议
    /// </summary>
    /// <param name="instanceId">审批实例ID</param>
    /// <param name="taskId">任务ID</param>
    /// <param name="businessData">业务数据</param>
    /// <param name="historicalData">历史审批数据</param>
    /// <returns>智能审批建议</returns>
    public async Task<IntelligentApprovalSuggestion> GetApprovalSuggestionAsync(
        long instanceId,
        long taskId,
        object businessData,
        List<ApprovalHistoryData> historicalData)
    {
        try
        {
            var cacheKey = $"approval_suggestion_{taskId}";
            if (_cache.TryGetValue(cacheKey, out IntelligentApprovalSuggestion? cachedSuggestion) && cachedSuggestion != null)
            {
                return cachedSuggestion;
            }

            _logger.LogInformation("生成智能审批建议: 实例ID={InstanceId}, 任务ID={TaskId}", instanceId, taskId);

            // 获取历史审批数据
            if (!historicalData.Any())
            {
                historicalData = await GetHistoricalApprovalDataAsync(businessData);
            }

            var systemPrompt = GetApprovalSuggestionSystemPrompt();
            var userPrompt = BuildApprovalSuggestionPrompt(businessData, historicalData);
            var llmClient = await GetLLMClientAsync();
            var llmResponse = await llmClient.GenerateContentAsync(systemPrompt, userPrompt);

            var suggestion = ParseApprovalSuggestionResponse(llmResponse);

            // 缓存建议15分钟
            _cache.Set(cacheKey, suggestion, TimeSpan.FromMinutes(15));

            _logger.LogInformation("智能审批建议生成: 任务={TaskId}, 建议={Suggestion}, 置信度={Confidence}",
                taskId, suggestion.SuggestedResult, suggestion.Confidence);

            return suggestion;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成智能审批建议失败: 任务={TaskId}", taskId);

            // 返回默认建议
            return new IntelligentApprovalSuggestion
            {
                SuggestedResult = ApprovalResult.Approve,
                Confidence = 0.5,
                Reasoning = "智能建议服务暂时不可用，建议人工审批",
                GeneratedTime = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// 异常检测
    /// </summary>
    /// <param name="businessData">业务数据</param>
    /// <param name="entityType">业务实体类型</param>
    /// <returns>异常检测结果</returns>
    public async Task<AnomalyDetectionResult> DetectAnomaliesAsync(object businessData, string entityType)
    {
        try
        {
            _logger.LogInformation("开始异常检测: 实体类型={EntityType}", entityType);

            var systemPrompt = GetAnomalyDetectionSystemPrompt();
            var userPrompt = BuildAnomalyDetectionPrompt(businessData, entityType);
            var llmClient = await GetLLMClientAsync();
            var llmResponse = await llmClient.GenerateContentAsync(systemPrompt, userPrompt);

            var result = ParseAnomalyDetectionResponse(llmResponse);

            _logger.LogInformation("异常检测完成: 实体类型={EntityType}, 异常数={AnomalyCount}",
                entityType, result.Anomalies.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "异常检测失败: 实体类型={EntityType}", entityType);

            return new AnomalyDetectionResult
            {
                HasAnomalies = false,
                AnomalyScore = 0,
                Anomalies = new List<Anomaly>()
            };
        }
    }

    /// <summary>
    /// 合规性检查
    /// </summary>
    /// <param name="businessData">业务数据</param>
    /// <param name="workflowCode">工作流代码</param>
    /// <returns>合规性检查结果</returns>
    public async Task<ComplianceCheckResult> CheckComplianceAsync(object businessData, string workflowCode)
    {
        try
        {
            _logger.LogInformation("开始合规性检查: 工作流={WorkflowCode}", workflowCode);

            var systemPrompt = GetComplianceCheckSystemPrompt();
            var userPrompt = BuildComplianceCheckPrompt(businessData, workflowCode);
            var llmClient = await GetLLMClientAsync();
            var llmResponse = await llmClient.GenerateContentAsync(systemPrompt, userPrompt);

            var result = ParseComplianceCheckResponse(llmResponse);

            _logger.LogInformation("合规性检查完成: 工作流={WorkflowCode}, 合规={IsCompliant}, 违规数={ViolationCount}",
                workflowCode, result.IsCompliant, result.Violations.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "合规性检查失败: 工作流={WorkflowCode}", workflowCode);

            return new ComplianceCheckResult
            {
                IsCompliant = true,
                ComplianceScore = 1.0,
                Violations = new List<ComplianceViolation>()
            };
        }
    }

    /// <summary>
    /// 获取风险评估系统提示词
    /// </summary>
    /// <returns>系统提示词</returns>
    private string GetRiskAssessmentSystemPrompt()
    {
        return @"你是一个专业的风险评估专家。请根据提供的业务数据进行风险评估，并返回JSON格式的结果。
评估维度包括：
1. 金额风险：涉及金额的大小和合理性
2. 时间风险：时间安排的紧急程度和合理性
3. 合规风险：是否符合相关规定和流程
4. 历史风险：基于历史数据的风险模式
5. 异常风险：数据中的异常情况

返回格式：
{
  ""riskLevel"": ""Low|Medium|High|Critical"",
  ""riskScore"": 0-100,
  ""riskFactors"": [
    {
      ""name"": ""风险因子名称"",
      ""type"": ""风险类型"",
      ""value"": 风险值,
      ""weight"": 权重,
      ""description"": ""描述""
    }
  ],
  ""description"": ""风险描述"",
  ""recommendations"": [""建议1"", ""建议2""]
}";
    }

    /// <summary>
    /// 获取审批建议系统提示词
    /// </summary>
    /// <returns>系统提示词</returns>
    private string GetApprovalSuggestionSystemPrompt()
    {
        return @"你是一个智能审批助手。请根据业务数据和历史审批案例，提供审批建议。

返回格式：
{
  ""suggestedResult"": ""Approve|Reject|Transfer|AdditionalSign"",
  ""confidence"": 0.0-1.0,
  ""reasoning"": ""建议理由"",
  ""similarCases"": [
    {
      ""caseId"": 案例ID,
      ""similarity"": 相似度,
      ""result"": ""审批结果"",
      ""description"": ""案例描述""
    }
  ],
  ""keyMetrics"": {
    ""metric1"": ""value1"",
    ""metric2"": ""value2""
  }
}";
    }

    /// <summary>
    /// 获取异常检测系统提示词
    /// </summary>
    /// <returns>系统提示词</returns>
    private string GetAnomalyDetectionSystemPrompt()
    {
        return @"你是一个数据异常检测专家。请分析业务数据中的异常情况。

返回格式：
{
  ""hasAnomalies"": true|false,
  ""anomalyScore"": 0.0-1.0,
  ""anomalies"": [
    {
      ""type"": ""异常类型"",
      ""field"": ""异常字段"",
      ""value"": ""异常值"",
      ""expectedValue"": ""期望值"",
      ""description"": ""异常描述""
    }
  ]
}";
    }

    /// <summary>
    /// 获取合规性检查系统提示词
    /// </summary>
    /// <returns>系统提示词</returns>
    private string GetComplianceCheckSystemPrompt()
    {
        return @"你是一个合规性检查专家。请检查业务数据是否符合相关规定和流程。

返回格式：
{
  ""isCompliant"": true|false,
  ""complianceScore"": 0.0-1.0,
  ""violations"": [
    {
      ""ruleName"": ""规则名称"",
      ""description"": ""违规描述"",
      ""severity"": ""Info|Warning|Error|Critical""
    }
  ]
}";
    }

    /// <summary>
    /// 构建风险评估提示词
    /// </summary>
    /// <param name="businessData">业务数据</param>
    /// <param name="workflowCode">工作流代码</param>
    /// <returns>提示词</returns>
    private string BuildRiskAssessmentPrompt(object businessData, string workflowCode)
    {
        return $@"请对以下业务数据进行风险评估：

工作流类型：{workflowCode}
业务数据：{JsonConvert.SerializeObject(businessData, Formatting.Indented)}

请分析潜在风险并提供评估结果。";
    }

    /// <summary>
    /// 构建审批建议提示词
    /// </summary>
    /// <param name="businessData">业务数据</param>
    /// <param name="historicalData">历史数据</param>
    /// <returns>提示词</returns>
    private string BuildApprovalSuggestionPrompt(object businessData, List<ApprovalHistoryData> historicalData)
    {
        var historicalJson = JsonConvert.SerializeObject(historicalData.Take(5), Formatting.Indented);
        
        return $@"请根据以下信息提供审批建议：

当前业务数据：
{JsonConvert.SerializeObject(businessData, Formatting.Indented)}

历史审批案例（最近5个相似案例）：
{historicalJson}

请分析并提供审批建议。";
    }

    /// <summary>
    /// 构建异常检测提示词
    /// </summary>
    /// <param name="businessData">业务数据</param>
    /// <param name="entityType">实体类型</param>
    /// <returns>提示词</returns>
    private string BuildAnomalyDetectionPrompt(object businessData, string entityType)
    {
        return $@"请检测以下业务数据中的异常：

实体类型：{entityType}
业务数据：{JsonConvert.SerializeObject(businessData, Formatting.Indented)}

请识别数据中的异常情况。";
    }

    /// <summary>
    /// 构建合规性检查提示词
    /// </summary>
    /// <param name="businessData">业务数据</param>
    /// <param name="workflowCode">工作流代码</param>
    /// <returns>提示词</returns>
    private string BuildComplianceCheckPrompt(object businessData, string workflowCode)
    {
        return $@"请检查以下业务数据的合规性：

工作流类型：{workflowCode}
业务数据：{JsonConvert.SerializeObject(businessData, Formatting.Indented)}

请检查是否符合相关规定和流程要求。";
    }

    /// <summary>
    /// 解析风险评估响应
    /// </summary>
    /// <param name="response">LLM响应</param>
    /// <returns>风险评估结果</returns>
    private RiskAssessmentResult ParseRiskAssessmentResponse(string response)
    {
        try
        {
            var result = JsonConvert.DeserializeObject<RiskAssessmentResult>(response);
            return result ?? new RiskAssessmentResult();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "解析风险评估响应失败，使用默认结果");
            return new RiskAssessmentResult
            {
                RiskLevel = RiskLevel.Medium,
                RiskScore = 50,
                Description = "解析LLM响应失败，使用默认中等风险评估"
            };
        }
    }

    /// <summary>
    /// 解析审批建议响应
    /// </summary>
    /// <param name="response">LLM响应</param>
    /// <returns>审批建议</returns>
    private IntelligentApprovalSuggestion ParseApprovalSuggestionResponse(string response)
    {
        try
        {
            var suggestion = JsonConvert.DeserializeObject<IntelligentApprovalSuggestion>(response);
            return suggestion ?? new IntelligentApprovalSuggestion();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "解析审批建议响应失败，使用默认建议");
            return new IntelligentApprovalSuggestion
            {
                SuggestedResult = ApprovalResult.Approve,
                Confidence = 0.5,
                Reasoning = "解析LLM响应失败，建议人工审批"
            };
        }
    }

    /// <summary>
    /// 解析异常检测响应
    /// </summary>
    /// <param name="response">LLM响应</param>
    /// <returns>异常检测结果</returns>
    private AnomalyDetectionResult ParseAnomalyDetectionResponse(string response)
    {
        try
        {
            var result = JsonConvert.DeserializeObject<AnomalyDetectionResult>(response);
            return result ?? new AnomalyDetectionResult();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "解析异常检测响应失败，使用默认结果");
            return new AnomalyDetectionResult
            {
                HasAnomalies = false,
                AnomalyScore = 0
            };
        }
    }

    /// <summary>
    /// 解析合规性检查响应
    /// </summary>
    /// <param name="response">LLM响应</param>
    /// <returns>合规性检查结果</returns>
    private ComplianceCheckResult ParseComplianceCheckResponse(string response)
    {
        try
        {
            var result = JsonConvert.DeserializeObject<ComplianceCheckResult>(response);
            return result ?? new ComplianceCheckResult();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "解析合规性检查响应失败，使用默认结果");
            return new ComplianceCheckResult
            {
                IsCompliant = true,
                ComplianceScore = 1.0
            };
        }
    }

    /// <summary>
    /// 获取历史审批数据
    /// </summary>
    /// <param name="businessData">业务数据</param>
    /// <returns>历史审批数据</returns>
    private async Task<List<ApprovalHistoryData>> GetHistoricalApprovalDataAsync(object businessData)
    {
        try
        {
            // 这里可以根据业务数据的特征查找相似的历史审批记录
            // 暂时返回最近的10个已完成的审批记录
            var recentInstances = await _instanceRepository.CreateQuery()
                .Where(x => x.Status == ApprovalStatus.Approved || x.Status == ApprovalStatus.Rejected)
                .OrderByDescending(x => x.CompletedTime)
                .Take(10)
                .Select(x => new ApprovalHistoryData
                {
                    InstanceId = x.Id,
                    BusinessData = x.BusinessData,
                    Result = x.Status,
                    ProcessedTime = x.CompletedTime ?? DateTime.MinValue,
                    ApproverId = x.ApplicantId // 这里应该是最后审批人，暂时用申请人代替
                })
                .ToListAsync();

            return recentInstances;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取历史审批数据失败");
            return new List<ApprovalHistoryData>();
        }
    }
}
