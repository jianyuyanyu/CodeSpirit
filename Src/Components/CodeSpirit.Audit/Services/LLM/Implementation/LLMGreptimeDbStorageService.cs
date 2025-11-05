using CodeSpirit.Audit.Models;
using CodeSpirit.Audit.Models.LLM;
using CodeSpirit.Audit.Services.LLM.Dtos;
using CodeSpirit.MultiTenant.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Text;
using System.Globalization;

namespace CodeSpirit.Audit.Services.LLM.Implementation;

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
    
    /// <summary>
    /// 初始化LLM审计GreptimeDB存储服务
    /// </summary>
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
        
        // 配置HttpClient
        _httpClient.BaseAddress = new Uri(_greptimeDbConfig.Url);
        _httpClient.Timeout = TimeSpan.FromSeconds(_greptimeDbConfig.TimeoutSeconds);
        
        // 设置认证（如果配置了用户名密码）
        if (!string.IsNullOrEmpty(_greptimeDbConfig.Username) && !string.IsNullOrEmpty(_greptimeDbConfig.Password))
        {
            var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_greptimeDbConfig.Username}:{_greptimeDbConfig.Password}"));
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authValue);
        }
    }
    
    /// <summary>
    /// 获取表名
    /// </summary>
    private string GetTableName()
    {
        if (string.IsNullOrWhiteSpace(_options.TablePrefix))
        {
            return _options.TableName;
        }
        
        return $"{_options.TablePrefix}_{_options.TableName}";
    }
    
    /// <summary>
    /// 获取当前租户ID
    /// </summary>
    private string GetCurrentTenantId()
    {
        return _tenantContext?.TenantId ?? "default";
    }
    
    /// <summary>
    /// 转义SQL字符串
    /// </summary>
    private string EscapeSqlString(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return "";
        
        return input.Replace("'", "''").Replace("\\", "\\\\");
    }
    
    /// <inheritdoc/>
    public async Task<bool> InitializeAsync()
    {
        try
        {
            var tableName = GetTableName();
            
            var createTableSql = $@"
                CREATE TABLE IF NOT EXISTS {tableName} (
                    `id` STRING,
                    `tenant_id` STRING,
                    `user_id` STRING,
                    `user_name` STRING,
                    `operation_time` TIMESTAMP TIME INDEX,
                    `llm_provider` STRING,
                    `model_name` STRING,
                    `interaction_type` STRING,
                    `business_scenario` STRING,
                    `system_prompt` STRING,
                    `user_prompt` STRING,
                    `llm_response` STRING,
                    `processed_data` STRING,
                    `input_tokens` BIGINT,
                    `output_tokens` BIGINT,
                    `total_tokens` BIGINT,
                    `processing_time_ms` BIGINT,
                    `cost_usd` DOUBLE,
                    `is_success` BOOLEAN,
                    `error_message` STRING,
                    `retry_count` INT,
                    `was_json_repaired` BOOLEAN,
                    `quality_score` INT,
                    `batch_id` STRING,
                    `batch_sequence` INT,
                    `parent_audit_id` STRING,
                    `business_entity_id` STRING,
                    `business_entity_type` STRING,
                    `data_count` INT,
                    `metadata` STRING,
                    PRIMARY KEY(`id`)
                )";
            
            var success = await ExecuteSqlAsync(createTableSql);
            
            if (success)
            {
                _logger.LogInformation("LLM审计表初始化成功: {TableName}", tableName);
            }
            else
            {
                _logger.LogError("LLM审计表初始化失败: {TableName}", tableName);
            }
            
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "初始化LLM审计表时发生异常");
            return false;
        }
    }
    
    /// <inheritdoc/>
    public async Task<bool> StoreAsync(LLMAuditLog auditLog)
    {
        try
        {
            var tableName = GetTableName();
            var tenantId = auditLog.TenantId ?? GetCurrentTenantId();
            
            // 确保字符串不为null
            auditLog.SystemPrompt ??= string.Empty;
            auditLog.UserPrompt ??= string.Empty;
            auditLog.LLMResponse ??= string.Empty;
            auditLog.ProcessedData ??= string.Empty;
            auditLog.ErrorMessage ??= string.Empty;
            
            _logger.LogInformation("准备存储LLM审计日志到GreptimeDB: {Id}, 响应长度: {ResponseLength}, 提示词长度: {PromptLength}, 成本: {Cost}", 
                auditLog.Id, auditLog.LLMResponse.Length, auditLog.UserPrompt.Length, auditLog.CostUsd);
            
            
            // 序列化元数据为JSON
            var metadataJson = auditLog.Metadata?.Count > 0 
                ? JsonConvert.SerializeObject(auditLog.Metadata) 
                : "";
            
            var insertSql = $@"
                INSERT INTO {tableName} (
                    `id`, `tenant_id`, `user_id`, `user_name`, `operation_time`,
                    `llm_provider`, `model_name`, `interaction_type`, `business_scenario`,
                    `system_prompt`, `user_prompt`, `llm_response`, `processed_data`,
                    `input_tokens`, `output_tokens`, `total_tokens`, `processing_time_ms`,
                    `cost_usd`, `is_success`, `error_message`, `retry_count`, `was_json_repaired`,
                    `quality_score`, `batch_id`, `batch_sequence`, `parent_audit_id`,
                    `business_entity_id`, `business_entity_type`, `data_count`, `metadata`
                ) VALUES (
                    '{EscapeSqlString(auditLog.Id)}',
                    '{EscapeSqlString(tenantId)}',
                    '{EscapeSqlString(auditLog.UserId)}',
                    '{EscapeSqlString(auditLog.UserName)}',
                    '{auditLog.OperationTime:yyyy-MM-dd HH:mm:ss.fff}',
                    '{EscapeSqlString(auditLog.LLMProvider)}',
                    '{EscapeSqlString(auditLog.ModelName)}',
                    '{EscapeSqlString(auditLog.InteractionType)}',
                    '{EscapeSqlString(auditLog.BusinessScenario)}',
                    '{EscapeSqlString(auditLog.SystemPrompt)}',
                    '{EscapeSqlString(auditLog.UserPrompt)}',
                    '{EscapeSqlString(auditLog.LLMResponse)}',
                    '{EscapeSqlString(auditLog.ProcessedData)}',
                    {auditLog.TokenUsage?.InputTokens ?? 0},
                    {auditLog.TokenUsage?.OutputTokens ?? 0},
                    {auditLog.TokenUsage?.TotalTokens ?? 0},
                    {auditLog.ProcessingTimeMs},
                    {(auditLog.CostUsd?.ToString(CultureInfo.InvariantCulture) ?? "NULL")},
                    {auditLog.IsSuccess.ToString().ToLower()},
                    '{EscapeSqlString(auditLog.ErrorMessage)}',
                    {auditLog.RetryCount},
                    {auditLog.WasJsonRepaired.ToString().ToLower()},
                    {auditLog.QualityScore?.ToString() ?? "NULL"},
                    '{EscapeSqlString(auditLog.BatchId)}',
                    {auditLog.BatchSequence?.ToString() ?? "NULL"},
                    '{EscapeSqlString(auditLog.ParentAuditId)}',
                    '{EscapeSqlString(auditLog.BusinessEntityId)}',
                    '{EscapeSqlString(auditLog.BusinessEntityType)}',
                    {auditLog.DataCount?.ToString() ?? "NULL"},
                    '{EscapeSqlString(metadataJson)}'
                )";
            
            var success = await ExecuteSqlAsync(insertSql);
            
            if (success)
            {
                _logger.LogInformation("✓ LLM审计日志已成功存储到GreptimeDB: {Id}, 响应长度: {ResponseLength}", 
                    auditLog.Id, auditLog.LLMResponse.Length);
            }
            else
            {
                _logger.LogError("✗ 存储LLM审计日志到GreptimeDB失败: {Id}", auditLog.Id);
            }
            
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "存储LLM审计日志到GreptimeDB时发生异常: {Id}", auditLog.Id);
            return false;
        }
    }
    
    /// <inheritdoc/>
    public async Task<bool> BulkStoreAsync(IEnumerable<LLMAuditLog> auditLogs)
    {
        try
        {
            var logsList = auditLogs.ToList();
            if (!logsList.Any())
            {
                return true;
            }
            
            var tableName = GetTableName();
            
            // 分批处理
            var batches = SplitIntoBatches(logsList, _options.BatchSize);
            
            foreach (var batch in batches)
            {
                var valuesBuilder = new StringBuilder();
                
                foreach (var log in batch)
                {
                    if (valuesBuilder.Length > 0)
                    {
                        valuesBuilder.Append(",");
                    }
                    
                    // 确保字符串不为null
                    log.SystemPrompt ??= string.Empty;
                    log.UserPrompt ??= string.Empty;
                    log.LLMResponse ??= string.Empty;
                    log.ProcessedData ??= string.Empty;
                    log.ErrorMessage ??= string.Empty;
                    
                    var tenantId = log.TenantId ?? GetCurrentTenantId();
                    var metadataJson = log.Metadata?.Count > 0 
                        ? JsonConvert.SerializeObject(log.Metadata) 
                        : "";
                    
                    valuesBuilder.Append($@"
                        ('{EscapeSqlString(log.Id)}',
                         '{EscapeSqlString(tenantId)}',
                         '{EscapeSqlString(log.UserId)}',
                         '{EscapeSqlString(log.UserName)}',
                         '{log.OperationTime:yyyy-MM-dd HH:mm:ss.fff}',
                         '{EscapeSqlString(log.LLMProvider)}',
                         '{EscapeSqlString(log.ModelName)}',
                         '{EscapeSqlString(log.InteractionType)}',
                         '{EscapeSqlString(log.BusinessScenario)}',
                         '{EscapeSqlString(log.SystemPrompt)}',
                         '{EscapeSqlString(log.UserPrompt)}',
                         '{EscapeSqlString(log.LLMResponse)}',
                         '{EscapeSqlString(log.ProcessedData)}',
                         {log.TokenUsage?.InputTokens ?? 0},
                         {log.TokenUsage?.OutputTokens ?? 0},
                         {log.TokenUsage?.TotalTokens ?? 0},
                         {log.ProcessingTimeMs},
                         {(log.CostUsd?.ToString(CultureInfo.InvariantCulture) ?? "NULL")},
                         {log.IsSuccess.ToString().ToLower()},
                         '{EscapeSqlString(log.ErrorMessage)}',
                         {log.RetryCount},
                         {log.WasJsonRepaired.ToString().ToLower()},
                         {log.QualityScore?.ToString() ?? "NULL"},
                         '{EscapeSqlString(log.BatchId)}',
                         {log.BatchSequence?.ToString() ?? "NULL"},
                         '{EscapeSqlString(log.ParentAuditId)}',
                         '{EscapeSqlString(log.BusinessEntityId)}',
                         '{EscapeSqlString(log.BusinessEntityType)}',
                         {log.DataCount?.ToString() ?? "NULL"},
                         '{EscapeSqlString(metadataJson)}')");
                }
                
                var insertSql = $@"
                    INSERT INTO {tableName} (
                        `id`, `tenant_id`, `user_id`, `user_name`, `operation_time`,
                        `llm_provider`, `model_name`, `interaction_type`, `business_scenario`,
                        `system_prompt`, `user_prompt`, `llm_response`, `processed_data`,
                        `input_tokens`, `output_tokens`, `total_tokens`, `processing_time_ms`,
                        `cost_usd`, `is_success`, `error_message`, `retry_count`, `was_json_repaired`,
                        `quality_score`, `batch_id`, `batch_sequence`, `parent_audit_id`,
                        `business_entity_id`, `business_entity_type`, `data_count`, `metadata`
                    ) VALUES {valuesBuilder}";
                
                var success = await ExecuteSqlAsync(insertSql);
                
                if (!success)
                {
                    _logger.LogError("批量存储LLM审计日志到GreptimeDB失败，批次大小: {BatchSize}", batch.Count());
                    return false;
                }
            }
            
            _logger.LogInformation("批量存储LLM审计日志到GreptimeDB成功: {Count}条", logsList.Count);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量存储LLM审计日志到GreptimeDB时发生异常");
            return false;
        }
    }
    
    /// <inheritdoc/>
    public async Task<LLMAuditLog?> GetByIdAsync(string id)
    {
        try
        {
            var tableName = GetTableName();
            var tenantId = GetCurrentTenantId();
            
            var selectSql = $@"
                SELECT * FROM {tableName}
                WHERE `id` = '{EscapeSqlString(id)}' AND `tenant_id` = '{EscapeSqlString(tenantId)}'
                LIMIT 1";
            
            var result = await ExecuteQueryAsync(selectSql);
            
            if (result.Any())
            {
                return MapToLLMAuditLog(result.First());
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "从GreptimeDB根据ID获取LLM审计日志时发生异常: {Id}", id);
            return null;
        }
    }
    
    /// <inheritdoc/>
    public async Task<(IEnumerable<LLMAuditLog> Items, long Total)> SearchAsync(LLMAuditQueryDto query)
    {
        try
        {
            var tableName = GetTableName();
            var tenantId = GetCurrentTenantId();
            var whereConditions = new List<string> { $"`tenant_id` = '{EscapeSqlString(tenantId)}'" };
            
            // 构建查询条件
            if (!string.IsNullOrEmpty(query.UserId))
            {
                whereConditions.Add($"`user_id` = '{EscapeSqlString(query.UserId)}'");
            }
            
            if (!string.IsNullOrEmpty(query.LLMProvider))
            {
                whereConditions.Add($"`llm_provider` = '{EscapeSqlString(query.LLMProvider)}'");
            }
            
            if (!string.IsNullOrEmpty(query.ModelName))
            {
                whereConditions.Add($"`model_name` = '{EscapeSqlString(query.ModelName)}'");
            }
            
            if (!string.IsNullOrEmpty(query.InteractionType))
            {
                whereConditions.Add($"`interaction_type` = '{EscapeSqlString(query.InteractionType)}'");
            }
            
            if (!string.IsNullOrEmpty(query.BusinessScenario))
            {
                whereConditions.Add($"`business_scenario` = '{EscapeSqlString(query.BusinessScenario)}'");
            }
            
            if (query.IsSuccess.HasValue)
            {
                whereConditions.Add($"`is_success` = {query.IsSuccess.Value.ToString().ToLower()}");
            }
            
            if (query.StartTime.HasValue)
            {
                whereConditions.Add($"`operation_time` >= '{query.StartTime.Value:yyyy-MM-dd HH:mm:ss}'");
            }
            
            if (query.EndTime.HasValue)
            {
                whereConditions.Add($"`operation_time` <= '{query.EndTime.Value:yyyy-MM-dd HH:mm:ss}'");
            }
            
            var whereClause = string.Join(" AND ", whereConditions);
            
            // 获取总数
            var countSql = $"SELECT COUNT(*) as total FROM {tableName} WHERE {whereClause}";
            var countResult = await ExecuteQueryAsync(countSql);
            var total = countResult.FirstOrDefault()?["total"]?.ToString();
            var totalCount = long.TryParse(total, out var count) ? count : 0L;
            
            // 获取数据
            var orderBy = "ORDER BY `operation_time` DESC";
            var limit = query.PerPage > 0 ? $"LIMIT {query.PerPage}" : "LIMIT 100";
            var offset = query.Page > 1 ? $"OFFSET {(query.Page - 1) * query.PerPage}" : "";
            
            var selectSql = $@"
                SELECT * FROM {tableName}
                WHERE {whereClause}
                {orderBy}
                {limit} {offset}";
            
            var result = await ExecuteQueryAsync(selectSql);
            var items = result.Select(MapToLLMAuditLog).ToList();
            
            return (items, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "从GreptimeDB搜索LLM审计日志时发生异常");
            return (Enumerable.Empty<LLMAuditLog>(), 0);
        }
    }
    
    /// <inheritdoc/>
    public async Task<Dictionary<string, long>> GetAggregationAsync(string field, DateTime startTime, DateTime endTime, string? tenantId = null)
    {
        try
        {
            var tableName = GetTableName();
            var currentTenantId = tenantId ?? GetCurrentTenantId();
            
            var aggregationSql = $@"
                SELECT `{field}`, COUNT(*) as count
                FROM {tableName}
                WHERE `tenant_id` = '{EscapeSqlString(currentTenantId)}'
                  AND `operation_time` >= '{startTime:yyyy-MM-dd HH:mm:ss}'
                  AND `operation_time` <= '{endTime:yyyy-MM-dd HH:mm:ss}'
                GROUP BY `{field}`
                ORDER BY count DESC";
            
            var result = await ExecuteQueryAsync(aggregationSql);
            var aggregation = new Dictionary<string, long>();
            
            foreach (var row in result)
            {
                var fieldValue = row[field]?.ToString() ?? "未知";
                var countValue = row["count"]?.ToString();
                if (long.TryParse(countValue, out var count))
                {
                    aggregation[fieldValue] = count;
                }
            }
            
            return aggregation;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "从GreptimeDB获取LLM审计日志聚合统计时发生异常");
            return new Dictionary<string, long>();
        }
    }
    
    /// <inheritdoc/>
    public async Task<bool> HealthCheckAsync()
    {
        try
        {
            // 执行简单的查询来检查连接
            var healthCheckSql = "SELECT 1 as health_check";
            var result = await ExecuteQueryAsync(healthCheckSql);
            
            return result.Any();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LLM审计GreptimeDB存储健康检查失败");
            return false;
        }
    }
    
    #region 私有辅助方法
    
    /// <summary>
    /// 分批处理数据
    /// </summary>
    private IEnumerable<IEnumerable<T>> SplitIntoBatches<T>(IEnumerable<T> source, int batchSize)
    {
        var batch = new List<T>();
        foreach (var item in source)
        {
            batch.Add(item);
            if (batch.Count >= batchSize)
            {
                yield return batch;
                batch = new List<T>();
            }
        }
        
        if (batch.Any())
        {
            yield return batch;
        }
    }
    
    /// <summary>
    /// 执行SQL命令
    /// </summary>
    private async Task<bool> ExecuteSqlAsync(string sql)
    {
        const int maxRetries = 3;
        
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var formParams = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("sql", sql)
                };
                var content = new FormUrlEncodedContent(formParams);
                
                _logger.LogDebug("执行GreptimeDB SQL，尝试 {Attempt}/{MaxRetries}, Database: {Database}", 
                    attempt, maxRetries, _greptimeDbConfig.Database);
                
                var response = await _httpClient.PostAsync($"/v1/sql?db={_greptimeDbConfig.Database}", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogDebug("GreptimeDB SQL执行成功，响应: {Response}", responseContent);
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("GreptimeDB SQL执行失败: {StatusCode}, {Error}", 
                        response.StatusCode, errorContent);
                    
                    // 检查是否是表不存在的错误
                    if (response.StatusCode == System.Net.HttpStatusCode.BadRequest && 
                        errorContent.Contains("Table not found") && 
                        !sql.Contains("CREATE TABLE"))
                    {
                        _logger.LogWarning("检测到表不存在错误，尝试自动初始化表");
                        
                        // 尝试初始化表
                        var initSuccess = await InitializeAsync();
                        if (initSuccess)
                        {
                            _logger.LogInformation("表初始化成功，重新执行SQL");
                            // 重新执行原始SQL
                            continue;
                        }
                        else
                        {
                            _logger.LogError("表初始化失败，终止SQL执行");
                            return false;
                        }
                    }
                    
                    // 如果是服务不可用错误，等待后重试
                    if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable && attempt < maxRetries)
                    {
                        var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                        _logger.LogWarning("GreptimeDB服务不可用，{Delay}秒后重试", delay.TotalSeconds);
                        await Task.Delay(delay);
                        continue;
                    }
                    
                    return false;
                }
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "GreptimeDB HTTP请求失败，尝试 {Attempt}/{MaxRetries}: {Message}", 
                    attempt, maxRetries, httpEx.Message);
                
                if (attempt < maxRetries)
                {
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                    _logger.LogWarning("网络错误，{Delay}秒后重试", delay.TotalSeconds);
                    await Task.Delay(delay);
                    continue;
                }
            }
            catch (TaskCanceledException tcEx) when (tcEx.InnerException is TimeoutException)
            {
                _logger.LogError("GreptimeDB请求超时，尝试 {Attempt}/{MaxRetries}", attempt, maxRetries);
                
                if (attempt < maxRetries)
                {
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                    _logger.LogWarning("请求超时，{Delay}秒后重试", delay.TotalSeconds);
                    await Task.Delay(delay);
                    continue;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "执行GreptimeDB SQL失败，尝试 {Attempt}/{MaxRetries}: {SQL}", 
                    attempt, maxRetries, sql);
                
                if (attempt < maxRetries)
                {
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                    await Task.Delay(delay);
                    continue;
                }
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// 执行查询SQL
    /// </summary>
    private async Task<IEnumerable<Dictionary<string, object>>> ExecuteQueryAsync(string sql)
    {
        try
        {
            var formParams = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("sql", sql)
            };
            var content = new FormUrlEncodedContent(formParams);
            
            _logger.LogDebug("执行GreptimeDB查询，Database: {Database}", _greptimeDbConfig.Database);
            
            var response = await _httpClient.PostAsync($"/v1/sql?db={_greptimeDbConfig.Database}", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogDebug("GreptimeDB查询成功，响应: {Response}", responseContent);
                
                var result = JsonConvert.DeserializeObject<GreptimeDbQueryResponse>(responseContent);
                if (result?.Output != null && result.Output.Any())
                {
                    var results = new List<Dictionary<string, object>>();
                    
                    var output = result.Output.First();
                    if (output.Records != null)
                    {
                        var columns = output.Records.Schema?.Column_schemas ?? new List<ColumnSchema>();
                        
                        foreach (var row in output.Records.Rows ?? new List<List<object>>())
                        {
                            var record = new Dictionary<string, object>();
                            for (int i = 0; i < columns.Count && i < row.Count; i++)
                            {
                                record[columns[i].Name] = row[i] ?? "";
                            }
                            results.Add(record);
                        }
                    }
                    
                    return results;
                }
                
                return Enumerable.Empty<Dictionary<string, object>>();
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("GreptimeDB查询失败: {StatusCode}, {Error}", 
                    response.StatusCode, errorContent);
                return Enumerable.Empty<Dictionary<string, object>>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "执行GreptimeDB查询失败: {SQL}", sql);
            return Enumerable.Empty<Dictionary<string, object>>();
        }
    }
    
    /// <summary>
    /// 将查询结果映射为LLMAuditLog对象
    /// </summary>
    private LLMAuditLog MapToLLMAuditLog(Dictionary<string, object> row)
    {
        // 调试：记录所有字段的原始数据
        var llmResponseRaw = row.GetValueOrDefault("llm_response");
        var llmResponseStr = llmResponseRaw?.ToString() ?? "";
        var userPromptRaw = row.GetValueOrDefault("user_prompt");
        var userPromptStr = userPromptRaw?.ToString() ?? "";
        var systemPromptRaw = row.GetValueOrDefault("system_prompt");
        var systemPromptStr = systemPromptRaw?.ToString() ?? "";
        
       
        
        var auditLog = new LLMAuditLog
        {
            Id = row.GetValueOrDefault("id")?.ToString() ?? "",
            TenantId = row.GetValueOrDefault("tenant_id")?.ToString() ?? "",
            UserId = row.GetValueOrDefault("user_id")?.ToString() ?? "",
            UserName = row.GetValueOrDefault("user_name")?.ToString() ?? "",
            LLMProvider = row.GetValueOrDefault("llm_provider")?.ToString() ?? "",
            ModelName = row.GetValueOrDefault("model_name")?.ToString() ?? "",
            InteractionType = row.GetValueOrDefault("interaction_type")?.ToString() ?? "",
            BusinessScenario = row.GetValueOrDefault("business_scenario")?.ToString() ?? "",
            SystemPrompt = systemPromptStr,
            UserPrompt = userPromptStr,
            LLMResponse = llmResponseStr,
            ProcessedData = row.GetValueOrDefault("processed_data")?.ToString() ?? "",
            ErrorMessage = row.GetValueOrDefault("error_message")?.ToString() ?? "",
            BatchId = row.GetValueOrDefault("batch_id")?.ToString(),
            ParentAuditId = row.GetValueOrDefault("parent_audit_id")?.ToString(),
            BusinessEntityId = row.GetValueOrDefault("business_entity_id")?.ToString(),
            BusinessEntityType = row.GetValueOrDefault("business_entity_type")?.ToString()
        };
        
        // 解析时间 - GreptimeDB可能返回long（毫秒时间戳）或字符串
        var operationTimeRaw = row.GetValueOrDefault("operation_time");
        if (operationTimeRaw != null)
        {
            if (operationTimeRaw is long timestamp)
            {
                // Unix毫秒时间戳
                auditLog.OperationTime = DateTimeOffset.FromUnixTimeMilliseconds(timestamp).UtcDateTime;
                _logger.LogDebug("时间字段是long类型(毫秒时间戳): {Timestamp}, 解析为: {ParsedTime}", timestamp, auditLog.OperationTime);
            }
            else if (DateTime.TryParse(operationTimeRaw.ToString(), out var parsedTime))
            {
                auditLog.OperationTime = parsedTime;
                _logger.LogDebug("时间字段解析为DateTime: {ParsedTime}", parsedTime);
            }
            else
            {
                _logger.LogWarning("时间字段解析失败！原始值: {RawValue}, 类型: {Type}", 
                    operationTimeRaw, operationTimeRaw.GetType().Name);
            }
        }
        
        // 解析数值字段
        if (long.TryParse(row.GetValueOrDefault("processing_time_ms")?.ToString(), out var processingTime))
        {
            auditLog.ProcessingTimeMs = processingTime;
        }
        
        if (decimal.TryParse(row.GetValueOrDefault("cost_usd")?.ToString(), out var cost))
        {
            auditLog.CostUsd = cost;
        }
        
        if (bool.TryParse(row.GetValueOrDefault("is_success")?.ToString(), out var isSuccess))
        {
            auditLog.IsSuccess = isSuccess;
        }
        
        if (int.TryParse(row.GetValueOrDefault("retry_count")?.ToString(), out var retryCount))
        {
            auditLog.RetryCount = retryCount;
        }
        
        if (bool.TryParse(row.GetValueOrDefault("was_json_repaired")?.ToString(), out var wasJsonRepaired))
        {
            auditLog.WasJsonRepaired = wasJsonRepaired;
        }
        
        if (int.TryParse(row.GetValueOrDefault("quality_score")?.ToString(), out var qualityScore))
        {
            auditLog.QualityScore = qualityScore;
        }
        
        if (int.TryParse(row.GetValueOrDefault("batch_sequence")?.ToString(), out var batchSequence))
        {
            auditLog.BatchSequence = batchSequence;
        }
        
        if (int.TryParse(row.GetValueOrDefault("data_count")?.ToString(), out var dataCount))
        {
            auditLog.DataCount = dataCount;
        }
        
        // 解析Token使用统计
        auditLog.TokenUsage = new LLMTokenUsage();
        if (int.TryParse(row.GetValueOrDefault("input_tokens")?.ToString(), out var inputTokens))
        {
            auditLog.TokenUsage.InputTokens = inputTokens;
        }
        
        if (int.TryParse(row.GetValueOrDefault("output_tokens")?.ToString(), out var outputTokens))
        {
            auditLog.TokenUsage.OutputTokens = outputTokens;
        }
        
        if (int.TryParse(row.GetValueOrDefault("total_tokens")?.ToString(), out var totalTokens))
        {
            auditLog.TokenUsage.TotalTokens = totalTokens;
        }
        
        // 解析元数据
        var metadataJson = row.GetValueOrDefault("metadata")?.ToString();
        if (!string.IsNullOrEmpty(metadataJson))
        {
            try
            {
                auditLog.Metadata = JsonConvert.DeserializeObject<Dictionary<string, object>>(metadataJson) 
                                   ?? new Dictionary<string, object>();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "解析LLM审计日志元数据失败: {Id}", auditLog.Id);
                auditLog.Metadata = new Dictionary<string, object>();
            }
        }
        
        return auditLog;
    }
    
    #endregion
    
    #region GreptimeDB响应模型
    
    /// <summary>
    /// GreptimeDB查询响应
    /// </summary>
    public class GreptimeDbQueryResponse
    {
        public List<QueryOutput>? Output { get; set; }
    }
    
    /// <summary>
    /// 查询输出
    /// </summary>
    public class QueryOutput
    {
        public Records? Records { get; set; }
    }
    
    /// <summary>
    /// 记录集
    /// </summary>
    public class Records
    {
        public Schema? Schema { get; set; }
        public List<List<object>>? Rows { get; set; }
    }
    
    /// <summary>
    /// 模式定义
    /// </summary>
    public class Schema
    {
        public List<ColumnSchema>? Column_schemas { get; set; }
    }
    
    /// <summary>
    /// 列模式
    /// </summary>
    public class ColumnSchema
    {
        public string Name { get; set; } = "";
        public string Data_type { get; set; } = "";
    }
    
    #endregion
}

