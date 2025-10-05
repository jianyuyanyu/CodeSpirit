using CodeSpirit.Audit.Models;
using CodeSpirit.Audit.Services.Dtos;
using CodeSpirit.MultiTenant.Abstractions;
using Newtonsoft.Json;
using System.Text;
using System.Data;

namespace CodeSpirit.Audit.Services.Implementation;

/// <summary>
/// GreptimeDB审计存储服务实现
/// </summary>
public class GreptimeDbAuditStorageService : IAuditStorageService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GreptimeDbAuditStorageService> _logger;
    private readonly GreptimeDbOptions _options;
    private readonly ITenantContext? _tenantContext;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    public GreptimeDbAuditStorageService(
        HttpClient httpClient,
        ILogger<GreptimeDbAuditStorageService> logger,
        IConfiguration configuration,
        ITenantContext? tenantContext = null)
    {
        _httpClient = httpClient;
        _logger = logger;
        _tenantContext = tenantContext;
        
        // 绑定GreptimeDB配置
        _options = new GreptimeDbOptions();
        configuration.GetSection("Audit:GreptimeDB").Bind(_options);
        
        // 配置HttpClient
        _httpClient.BaseAddress = new Uri(_options.Url);
        _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
        
        // 设置认证（如果配置了用户名密码）
        if (!string.IsNullOrEmpty(_options.Username) && !string.IsNullOrEmpty(_options.Password))
        {
            var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_options.Username}:{_options.Password}"));
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authValue);
        }
    }
    
    /// <summary>
    /// 获取当前租户ID
    /// </summary>
    private string? GetCurrentTenantId()
    {
        return _tenantContext?.TenantId;
    }
    
    /// <summary>
    /// 获取最终的表名称（包含前缀）
    /// </summary>
    private string GetFinalTableName()
    {
        if (string.IsNullOrWhiteSpace(_options.TablePrefix))
        {
            return _options.TableName;
        }
        
        return $"{_options.TablePrefix}_{_options.TableName}";
    }
    
    /// <summary>
    /// 初始化存储（创建数据库和表）
    /// </summary>
    public async Task<bool> InitializeAsync()
    {
        try
        {
            // 首先检查基础连接（不指定数据库）
            _logger.LogInformation("开始GreptimeDB连接检查，URL: {Url}", _options.Url);
            var canConnect = await CheckBaseConnectionAsync();
            if (!canConnect)
            {
                _logger.LogError("GreptimeDB基础连接失败，无法连接到服务器");
                return false;
            }
            
            // 创建数据库（如果不存在）
            _logger.LogInformation("开始创建GreptimeDB数据库: {Database}", _options.Database);
            var dbCreated = await CreateDatabaseAsync();
            if (!dbCreated)
            {
                _logger.LogError("GreptimeDB数据库创建失败: {Database}", _options.Database);
                return false;
            }
            
            // 创建表（如果不存在）
            var tableName = GetFinalTableName();
            _logger.LogInformation("开始创建GreptimeDB表: {TableName}", tableName);
            
            var createTableSql = $@"
                CREATE TABLE IF NOT EXISTS {tableName} (
                    audit_id STRING,
                    user_id STRING,
                    user_name STRING,
                    ip_address STRING,
                    operation_time TIMESTAMP TIME INDEX,
                    operation_type STRING,
                    description STRING,
                    request_path STRING,
                    request_method STRING,
                    request_params TEXT,
                    execution_duration BIGINT,
                    is_success BOOLEAN,
                    error_message TEXT,
                    status_code INT,
                    before_data TEXT,
                    after_data TEXT,
                    user_agent TEXT,
                    operation_name STRING,
                    tenant_id STRING,
                    additional_data TEXT,
                    attribute_properties TEXT,
                    PRIMARY KEY (audit_id)
                )";
            
            _logger.LogDebug("执行建表SQL: {SQL}", createTableSql);
            var success = await ExecuteSqlAsync(createTableSql);
            
            if (success)
            {
                _logger.LogInformation("GreptimeDB表创建成功: {TableName}", tableName);
                
                // 最后进行健康检查确认一切正常
                var isHealthy = await HealthCheckAsync();
                if (isHealthy)
                {
                    _logger.LogInformation("GreptimeDB初始化完成并通过健康检查");
                }
                else
                {
                    _logger.LogWarning("GreptimeDB表创建成功，但健康检查未通过");
                }
            }
            else
            {
                _logger.LogError("GreptimeDB表创建失败: {TableName}", tableName);
            }
            
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "初始化GreptimeDB存储失败，配置信息 - URL: {Url}, Database: {Database}, TableName: {TableName}", 
                _options.Url, _options.Database, GetFinalTableName());
            return false;
        }
    }
    
    /// <summary>
    /// 存储审计日志
    /// </summary>
    public async Task<bool> StoreAsync(AuditLog auditLog)
    {
        try
        {
            var tableName = GetFinalTableName();
            // 序列化AdditionalData和AttributeProperties为JSON
            var additionalDataJson = auditLog.AdditionalData?.Count > 0 
                ? JsonConvert.SerializeObject(auditLog.AdditionalData) 
                : "";
            var attributePropertiesJson = auditLog.AttributeProperties?.Count > 0 
                ? JsonConvert.SerializeObject(auditLog.AttributeProperties) 
                : "";

            var insertSql = $@"
                INSERT INTO {tableName} (
                    audit_id, user_id, user_name, ip_address, operation_time,
                    operation_type, description, request_path, request_method, request_params,
                    execution_duration, is_success, error_message, status_code,
                    before_data, after_data, user_agent, operation_name, tenant_id,
                    additional_data, attribute_properties
                ) VALUES (
                    '{EscapeSqlString(auditLog.Id)}',
                    '{EscapeSqlString(auditLog.UserId)}',
                    '{EscapeSqlString(auditLog.UserName)}',
                    '{EscapeSqlString(auditLog.IpAddress)}',
                    '{auditLog.OperationTime:yyyy-MM-dd HH:mm:ss.fff}',
                    '{EscapeSqlString(auditLog.OperationType)}',
                    '{EscapeSqlString(auditLog.Description)}',
                    '{EscapeSqlString(auditLog.RequestPath)}',
                    '{EscapeSqlString(auditLog.RequestMethod)}',
                    '{EscapeSqlString(auditLog.RequestParams)}',
                    {auditLog.ExecutionDuration},
                    {auditLog.IsSuccess.ToString().ToLower()},
                    '{EscapeSqlString(auditLog.ErrorMessage)}',
                    {auditLog.StatusCode},
                    '{EscapeSqlString(auditLog.BeforeData)}',
                    '{EscapeSqlString(auditLog.AfterData)}',
                    '{EscapeSqlString(auditLog.UserAgent)}',
                    '{EscapeSqlString(auditLog.OperationName)}',
                    '{EscapeSqlString(auditLog.TenantId ?? GetCurrentTenantId() ?? "")}',
                    '{EscapeSqlString(additionalDataJson)}',
                    '{EscapeSqlString(attributePropertiesJson)}'
                )";
            
            var success = await ExecuteSqlAsync(insertSql);
            
            if (success)
            {
                _logger.LogDebug("审计日志已成功存储到GreptimeDB: {Id}", auditLog.Id);
            }
            else
            {
                _logger.LogError("存储审计日志到GreptimeDB失败: {Id}", auditLog.Id);
            }
            
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "存储审计日志到GreptimeDB失败: {Id}", auditLog.Id);
            return false;
        }
    }
    
    /// <summary>
    /// 批量存储审计日志
    /// </summary>
    public async Task<bool> BulkStoreAsync(IEnumerable<AuditLog> auditLogs)
    {
        try
        {
            var logList = auditLogs.ToList();
            if (!logList.Any())
            {
                return true;
            }
            
            var tableName = GetFinalTableName();
            var tenantId = "";
            
            // 分批处理
            var batches = SplitIntoBatches(logList, _options.BatchSize);
            
            foreach (var batch in batches)
            {
                var valuesBuilder = new StringBuilder();
                
                foreach (var log in batch)
                {
                    if (valuesBuilder.Length > 0)
                    {
                        valuesBuilder.Append(",");
                    }
                    
                    // 序列化AdditionalData和AttributeProperties为JSON
                    var additionalDataJson = log.AdditionalData?.Count > 0 
                        ? JsonConvert.SerializeObject(log.AdditionalData) 
                        : "";
                    var attributePropertiesJson = log.AttributeProperties?.Count > 0 
                        ? JsonConvert.SerializeObject(log.AttributeProperties) 
                        : "";
                    
                    valuesBuilder.Append($@"
                        ('{EscapeSqlString(log.Id)}',
                         '{EscapeSqlString(log.UserId)}',
                         '{EscapeSqlString(log.UserName)}',
                         '{EscapeSqlString(log.IpAddress)}',
                         '{log.OperationTime:yyyy-MM-dd HH:mm:ss.fff}',
                         '{EscapeSqlString(log.OperationType)}',
                         '{EscapeSqlString(log.Description)}',
                         '{EscapeSqlString(log.RequestPath)}',
                         '{EscapeSqlString(log.RequestMethod)}',
                         '{EscapeSqlString(log.RequestParams)}',
                         {log.ExecutionDuration},
                         {log.IsSuccess.ToString().ToLower()},
                         '{EscapeSqlString(log.ErrorMessage)}',
                         {log.StatusCode},
                         '{EscapeSqlString(log.BeforeData)}',
                         '{EscapeSqlString(log.AfterData)}',
                         '{EscapeSqlString(log.UserAgent)}',
                         '{EscapeSqlString(log.OperationName)}',
                         '{EscapeSqlString(tenantId)}',
                         '{EscapeSqlString(additionalDataJson)}',
                         '{EscapeSqlString(attributePropertiesJson)}')");
                }
                
                var insertSql = $@"
                    INSERT INTO {tableName} (
                        audit_id, user_id, user_name, ip_address, operation_time,
                        operation_type, description, request_path, request_method, request_params,
                        execution_duration, is_success, error_message, status_code,
                        before_data, after_data, user_agent, operation_name, tenant_id,
                        additional_data, attribute_properties
                    ) VALUES {valuesBuilder}";
                
                var success = await ExecuteSqlAsync(insertSql);
                
                if (!success)
                {
                    _logger.LogError("批量存储审计日志到GreptimeDB失败，批次大小: {BatchSize}", batch.Count());
                    return false;
                }
            }
            
            _logger.LogInformation("批量存储审计日志到GreptimeDB成功: {Count}条", logList.Count);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量存储审计日志到GreptimeDB失败");
            return false;
        }
    }
    
    /// <summary>
    /// 根据ID获取审计日志
    /// </summary>
    public async Task<AuditLog?> GetByIdAsync(string id)
    {
        try
        {
            var tableName = GetFinalTableName();
            var tenantCondition = "";
            
            var selectSql = $@"
                SELECT * FROM {tableName}
                WHERE audit_id = '{EscapeSqlString(id)}' {tenantCondition}
                LIMIT 1";
            
            var result = await ExecuteQueryAsync(selectSql);
            
            if (result.Any())
            {
                return MapToAuditLog(result.First());
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "从GreptimeDB获取审计日志失败: {Id}", id);
            return null;
        }
    }
    
    /// <summary>
    /// 搜索审计日志
    /// </summary>
    public async Task<(IEnumerable<AuditLog> Items, long Total)> SearchAsync(AuditLogQueryDto query)
    {
        try
        {
            var tableName = GetFinalTableName();
            var whereConditions = new List<string>();
            
            // 如果未指定租户ID且当前上下文有租户，则自动应用租户过滤
            var effectiveTenantId = query.TenantId ?? GetCurrentTenantId();
            if (!string.IsNullOrEmpty(effectiveTenantId))
            {
                whereConditions.Add($"tenant_id = '{EscapeSqlString(effectiveTenantId)}'");
            }
            
            // 构建查询条件
            if (!string.IsNullOrEmpty(query.UserId))
            {
                whereConditions.Add($"user_id = '{EscapeSqlString(query.UserId)}'");
            }
            
            if (!string.IsNullOrEmpty(query.UserName))
            {
                whereConditions.Add($"user_name LIKE '%{EscapeSqlString(query.UserName)}%'");
            }
            
            if (!string.IsNullOrEmpty(query.IpAddress))
            {
                whereConditions.Add($"ip_address = '{EscapeSqlString(query.IpAddress)}'");
            }
            
            if (query.StartTime.HasValue)
            {
                whereConditions.Add($"operation_time >= '{query.StartTime.Value:yyyy-MM-dd HH:mm:ss}'");
            }
            
            if (query.EndTime.HasValue)
            {
                whereConditions.Add($"operation_time <= '{query.EndTime.Value:yyyy-MM-dd HH:mm:ss}'");
            }
            
            if (query.OperationType.HasValue)
            {
                whereConditions.Add($"operation_type = '{EscapeSqlString(query.OperationType.Value.ToString())}'");
            }
            
            if (query.RequestMethod.HasValue)
            {
                whereConditions.Add($"request_method = '{EscapeSqlString(query.RequestMethod.Value.ToString())}'");
            }
            
            if (query.IsSuccess.HasValue)
            {
                whereConditions.Add($"is_success = {query.IsSuccess.Value.ToString().ToLower()}");
            }
            
            // 添加其他查询条件
            if (!string.IsNullOrEmpty(query.OperationName))
            {
                whereConditions.Add($"operation_name LIKE '%{EscapeSqlString(query.OperationName)}%'");
            }
            
            if (!string.IsNullOrEmpty(query.ApiController))
            {
                whereConditions.Add($"api_controller LIKE '%{EscapeSqlString(query.ApiController)}%'");
            }
            
            if (!string.IsNullOrEmpty(query.EntityName))
            {
                whereConditions.Add($"entity_name = '{EscapeSqlString(query.EntityName)}'");
            }
            
            if (query.OperationActionType.HasValue)
            {
                whereConditions.Add($"operation_action_type = '{EscapeSqlString(query.OperationActionType.Value.ToString().ToLower())}'");
            }
            
            if (query.IsBulkOperation.HasValue)
            {
                whereConditions.Add($"is_bulk_operation = {query.IsBulkOperation.Value.ToString().ToLower()}");
            }
            
            if (query.StatusCode.HasValue)
            {
                whereConditions.Add($"status_code = {(int)query.StatusCode.Value}");
            }
            else if (query.CustomStatusCode.HasValue)
            {
                whereConditions.Add($"status_code = {query.CustomStatusCode.Value}");
            }
            
            if (query.MinExecutionDuration.HasValue)
            {
                whereConditions.Add($"execution_duration >= {query.MinExecutionDuration.Value}");
            }
            
            if (query.MaxExecutionDuration.HasValue)
            {
                whereConditions.Add($"execution_duration <= {query.MaxExecutionDuration.Value}");
            }
            
            var whereClause = whereConditions.Any() ? $"WHERE {string.Join(" AND ", whereConditions)}" : "";
            
            // 构建排序
            var orderBy = $"ORDER BY {GetSqlFieldName(query.OrderBy ?? "OperationTime")} {(query.OrderDir?.ToUpper() == "ASC" ? "ASC" : "DESC")}";
            
            // 计算总数
            var countSql = $"SELECT COUNT(*) as total FROM {tableName} {whereClause}";
            var countResult = await ExecuteQueryAsync(countSql);
            var totalValue = countResult.FirstOrDefault()?.GetValueOrDefault("total", 0L) ?? 0L;
            var total = ConvertToInt64(totalValue);
            
            // 分页查询
            var offset = (query.Page - 1) * query.PerPage;
            var selectSql = $@"
                SELECT * FROM {tableName}
                {whereClause}
                {orderBy}
                LIMIT {query.PerPage} OFFSET {offset}";
            
            var results = await ExecuteQueryAsync(selectSql);
            var items = results.Select(MapToAuditLog).ToList();
            
            _logger.LogInformation("GreptimeDB搜索完成 - 返回 {ItemCount} 条记录，总计 {Total} 条", items.Count, total);
            
            return (items, total);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GreptimeDB搜索审计日志失败");
            return (Enumerable.Empty<AuditLog>(), 0);
        }
    }
    
    /// <summary>
    /// 删除审计日志
    /// </summary>
    public async Task<bool> DeleteAsync(string id)
    {
        try
        {
            var tableName = GetFinalTableName();
            var tenantCondition = "";
            
            var deleteSql = $@"
                DELETE FROM {tableName}
                WHERE audit_id = '{EscapeSqlString(id)}' {tenantCondition}";
            
            var success = await ExecuteSqlAsync(deleteSql);
            
            if (success)
            {
                _logger.LogInformation("从GreptimeDB删除审计日志成功: {Id}", id);
            }
            else
            {
                _logger.LogError("从GreptimeDB删除审计日志失败: {Id}", id);
            }
            
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "从GreptimeDB删除审计日志失败: {Id}", id);
            return false;
        }
    }
    
    /// <summary>
    /// 获取操作统计信息
    /// </summary>
    public async Task<Dictionary<string, long>> GetOperationStatsAsync(DateTime startTime, DateTime endTime, string? tenantId = null)
    {
        try
        {
            var tableName = GetFinalTableName();
            
            // 如果未指定租户ID且当前上下文有租户，则自动应用租户过滤
            var effectiveTenantId = tenantId ?? GetCurrentTenantId();
            var tenantCondition = !string.IsNullOrEmpty(effectiveTenantId) 
                ? $" AND tenant_id = '{EscapeSqlString(effectiveTenantId)}'" 
                : "";
            
            var statsSql = $@"
                SELECT operation_type, COUNT(*) as count
                FROM {tableName}
                WHERE operation_time >= '{startTime:yyyy-MM-dd HH:mm:ss}'
                  AND operation_time <= '{endTime:yyyy-MM-dd HH:mm:ss}' {tenantCondition}
                GROUP BY operation_type
                ORDER BY count DESC";
            
            var results = await ExecuteQueryAsync(statsSql);
            var stats = new Dictionary<string, long>();
            
            foreach (var result in results)
            {
                var operationType = result.GetValueOrDefault("operation_type", "").ToString() ?? "";
                var count = ConvertToInt64(result.GetValueOrDefault("count", 0L));
                stats[operationType] = count;
            }
            
            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取GreptimeDB操作统计失败");
            return new Dictionary<string, long>();
        }
    }
    
    /// <summary>
    /// 获取用户操作统计信息
    /// </summary>
    public async Task<Dictionary<string, long>> GetUserStatsAsync(DateTime startTime, DateTime endTime, int topN = 10, string? tenantId = null)
    {
        try
        {
            var tableName = GetFinalTableName();
            
            // 如果未指定租户ID且当前上下文有租户，则自动应用租户过滤
            var effectiveTenantId = tenantId ?? GetCurrentTenantId();
            var tenantCondition = !string.IsNullOrEmpty(effectiveTenantId) 
                ? $" AND tenant_id = '{EscapeSqlString(effectiveTenantId)}'" 
                : "";
            
            var statsSql = $@"
                SELECT user_name, COUNT(*) as count
                FROM {tableName}
                WHERE operation_time >= '{startTime:yyyy-MM-dd HH:mm:ss}'
                  AND operation_time <= '{endTime:yyyy-MM-dd HH:mm:ss}' {tenantCondition}
                  AND user_name IS NOT NULL
                  AND user_name != ''
                GROUP BY user_name
                ORDER BY count DESC
                LIMIT {topN}";
            
            var results = await ExecuteQueryAsync(statsSql);
            var stats = new Dictionary<string, long>();
            
            foreach (var result in results)
            {
                var userName = result.GetValueOrDefault("user_name", "").ToString() ?? "";
                var count = ConvertToInt64(result.GetValueOrDefault("count", 0L));
                stats[userName] = count;
            }
            
            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取GreptimeDB用户统计失败");
            return new Dictionary<string, long>();
        }
    }
    
    /// <summary>
    /// 根据时间获取操作趋势
    /// </summary>
    public async Task<Dictionary<DateTime, long>> GetOperationTrendAsync(DateTime startTime, DateTime endTime, int interval = 24, string? tenantId = null)
    {
        try
        {
            var tableName = GetFinalTableName();
            
            // 如果未指定租户ID且当前上下文有租户，则自动应用租户过滤
            var effectiveTenantId = tenantId ?? GetCurrentTenantId();
            var tenantCondition = !string.IsNullOrEmpty(effectiveTenantId) 
                ? $" AND tenant_id = '{EscapeSqlString(effectiveTenantId)}'" 
                : "";
            
            // 根据间隔构建时间分组
            var timeFormat = interval switch
            {
                1 => "date_trunc('hour', operation_time)",
                24 => "date_trunc('day', operation_time)",
                _ => "date_trunc('hour', operation_time)"
            };
            
            var trendSql = $@"
                SELECT {timeFormat} as time_bucket, COUNT(*) as count
                FROM {tableName}
                WHERE operation_time >= '{startTime:yyyy-MM-dd HH:mm:ss}'
                  AND operation_time <= '{endTime:yyyy-MM-dd HH:mm:ss}' {tenantCondition}
                GROUP BY time_bucket
                ORDER BY time_bucket";
            
            var results = await ExecuteQueryAsync(trendSql);
            var trend = new Dictionary<DateTime, long>();
            
            foreach (var result in results)
            {
                var timeBucket = ConvertToDateTime(result.GetValueOrDefault("time_bucket", 0L));
                var count = ConvertToInt64(result.GetValueOrDefault("count", 0L));
                trend[timeBucket] = count;
            }
            
            return trend;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取GreptimeDB操作趋势失败");
            return new Dictionary<DateTime, long>();
        }
    }
    
    /// <summary>
    /// 健康检查
    /// </summary>
    public async Task<bool> HealthCheckAsync()
    {
        try
        {
            _logger.LogDebug("开始GreptimeDB健康检查，BaseAddress: {BaseAddress}, Database: {Database}", 
                _httpClient.BaseAddress, _options.Database);
            
            // 首先检查基础连接
            var response = await _httpClient.GetAsync("/health");
            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug("GreptimeDB健康端点响应正常");
            }
            else
            {
                _logger.LogWarning("GreptimeDB健康端点不可用: {StatusCode}", response.StatusCode);
            }
            
            // 然后检查SQL查询
            var healthSql = "SELECT 1 as health";
            var result = await ExecuteQueryAsync(healthSql);
            var isHealthy = result.Any();
            
            if (isHealthy)
            {
                _logger.LogInformation("GreptimeDB健康检查通过");
            }
            else
            {
                _logger.LogError("GreptimeDB健康检查失败：无法执行查询");
            }
            
            return isHealthy;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GreptimeDB健康检查异常");
            return false;
        }
    }
    
    #region 私有辅助方法
    
    /// <summary>
    /// 检查基础连接（不指定数据库）
    /// </summary>
    private async Task<bool> CheckBaseConnectionAsync()
    {
        try
        {
            _logger.LogDebug("检查GreptimeDB基础连接，BaseAddress: {BaseAddress}", _httpClient.BaseAddress);
            
            // 尝试访问健康检查端点
            var response = await _httpClient.GetAsync("/health");
            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug("GreptimeDB健康端点响应正常");
                return true;
            }
            else
            {
                _logger.LogWarning("GreptimeDB健康端点不可用: {StatusCode}", response.StatusCode);
                
                // 如果健康端点不可用，尝试执行简单的SQL查询（不指定数据库）
                var testSql = "SELECT 1 as test";
                var formParams = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("sql", testSql)
                };
                var content = new FormUrlEncodedContent(formParams);
                
                var testResponse = await _httpClient.PostAsync("/v1/sql", content);
                var canConnect = testResponse.IsSuccessStatusCode;
                
                if (canConnect)
                {
                    _logger.LogDebug("GreptimeDB基础连接测试成功");
                }
                else
                {
                    var errorContent = await testResponse.Content.ReadAsStringAsync();
                    _logger.LogError("GreptimeDB基础连接测试失败: {StatusCode}, {Error}", 
                        testResponse.StatusCode, errorContent);
                }
                
                return canConnect;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GreptimeDB基础连接检查异常");
            return false;
        }
    }
    
    /// <summary>
    /// 创建数据库（如果不存在）
    /// </summary>
    private async Task<bool> CreateDatabaseAsync()
    {
        try
        {
            // 首先检查数据库是否已存在
            var checkDbSql = "SHOW DATABASES";
            var databases = await ExecuteQueryWithoutDbAsync(checkDbSql);
            
            // 检查数据库是否已存在
            var dbExists = databases.Any(db => 
                db.ContainsKey("Database") && 
                db["Database"].ToString()?.Equals(_options.Database, StringComparison.OrdinalIgnoreCase) == true);
            
            if (dbExists)
            {
                _logger.LogInformation("GreptimeDB数据库已存在: {Database}", _options.Database);
                return true;
            }
            
            // 创建数据库
            var createDbSql = $"CREATE DATABASE IF NOT EXISTS {_options.Database}";
            var success = await ExecuteSqlWithoutDbAsync(createDbSql);
            
            if (success)
            {
                _logger.LogInformation("GreptimeDB数据库创建成功: {Database}", _options.Database);
            }
            else
            {
                _logger.LogError("GreptimeDB数据库创建失败: {Database}", _options.Database);
            }
            
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建GreptimeDB数据库异常: {Database}", _options.Database);
            return false;
        }
    }
    
    /// <summary>
    /// 执行SQL命令（不指定数据库）
    /// </summary>
    private async Task<bool> ExecuteSqlWithoutDbAsync(string sql)
    {
        try
        {
            var formParams = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("sql", sql)
            };
            var content = new FormUrlEncodedContent(formParams);
            
            _logger.LogDebug("执行GreptimeDB SQL（无数据库），SQL: {SQL}", sql);
            
            var response = await _httpClient.PostAsync("/v1/sql", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogDebug("GreptimeDB SQL执行成功（无数据库），响应: {Response}", responseContent);
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("GreptimeDB SQL执行失败（无数据库）: {StatusCode}, {Error}", 
                    response.StatusCode, errorContent);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "执行GreptimeDB SQL失败（无数据库）: {SQL}", sql);
            return false;
        }
    }
    
    /// <summary>
    /// 执行查询SQL（不指定数据库）
    /// </summary>
    private async Task<IEnumerable<Dictionary<string, object>>> ExecuteQueryWithoutDbAsync(string sql)
    {
        try
        {
            var formParams = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("sql", sql)
            };
            var content = new FormUrlEncodedContent(formParams);
            
            _logger.LogDebug("执行GreptimeDB查询（无数据库），SQL: {SQL}", sql);
            
            var response = await _httpClient.PostAsync("/v1/sql", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogDebug("GreptimeDB查询成功（无数据库），响应: {Response}", responseContent);
                
                var result = JsonConvert.DeserializeObject<GreptimeDbQueryResponse>(responseContent);
                if (result?.Output != null && result.Output.Any())
                {
                    var results = new List<Dictionary<string, object>>();
                    
                    // 处理第一个输出项（通常查询只返回一个输出）
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
                _logger.LogError("GreptimeDB查询失败（无数据库）: {StatusCode}, {Error}", 
                    response.StatusCode, errorContent);
                return Enumerable.Empty<Dictionary<string, object>>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "执行GreptimeDB查询失败（无数据库）: {SQL}", sql);
            return Enumerable.Empty<Dictionary<string, object>>();
        }
    }
    
    /// <summary>
    /// 执行SQL命令
    /// </summary>
    private async Task<bool> ExecuteSqlAsync(string sql)
    {
        return await ExecuteSqlAsync(sql, true);
    }
    
    /// <summary>
    /// 执行SQL命令
    /// </summary>
    /// <param name="sql">要执行的SQL</param>
    /// <param name="autoInitialize">是否在数据库不存在时自动初始化</param>
    private async Task<bool> ExecuteSqlAsync(string sql, bool autoInitialize)
    {
        const int maxRetries = 3;
        
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                // GreptimeDB期望form-urlencoded格式
                var formParams = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("sql", sql)
                };
                var content = new FormUrlEncodedContent(formParams);
                
                _logger.LogDebug("执行GreptimeDB SQL，尝试 {Attempt}/{MaxRetries}, URL: {Url}, Database: {Database}", 
                    attempt, maxRetries, _httpClient.BaseAddress, _options.Database);
                
                var response = await _httpClient.PostAsync($"/v1/sql?db={_options.Database}", content);
                
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
                    
                    // 检查是否是数据库不存在的错误
                    if (autoInitialize && 
                        response.StatusCode == System.Net.HttpStatusCode.BadRequest && 
                        errorContent.Contains("Database not found"))
                    {
                        _logger.LogWarning("检测到数据库不存在错误，尝试自动初始化: {Database}", _options.Database);
                        
                        // 尝试初始化（避免递归调用）
                        var initSuccess = await InitializeAsync();
                        if (initSuccess)
                        {
                            _logger.LogInformation("数据库初始化成功，重新执行SQL");
                            // 重新执行SQL，但不再自动初始化避免无限递归
                            return await ExecuteSqlAsync(sql, false);
                        }
                        else
                        {
                            _logger.LogError("数据库初始化失败，终止SQL执行");
                            return false;
                        }
                    }
                    
                    // 如果是服务不可用错误，等待后重试
                    if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable && attempt < maxRetries)
                    {
                        var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt)); // 指数退避
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
                    attempt, maxRetries, sql.Length > 200 ? sql.Substring(0, 200) + "..." : sql);
                
                if (attempt < maxRetries)
                {
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                    await Task.Delay(delay);
                    continue;
                }
            }
        }
        
        _logger.LogError("GreptimeDB SQL执行最终失败，已重试 {MaxRetries} 次", maxRetries);
        return false;
    }
    
    /// <summary>
    /// 执行查询SQL
    /// </summary>
    private async Task<List<Dictionary<string, object>>> ExecuteQueryAsync(string sql)
    {
        try
        {
            // GreptimeDB期望form-urlencoded格式
            var formParams = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("sql", sql)
            };
            var content = new FormUrlEncodedContent(formParams);
            
            var response = await _httpClient.PostAsync($"/v1/sql?db={_options.Database}", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var queryResult = JsonConvert.DeserializeObject<GreptimeDbQueryResponse>(responseContent);
                
                if (queryResult?.Output != null && queryResult.Output.Any())
                {
                    var results = new List<Dictionary<string, object>>();
                    
                    // 处理第一个输出项（通常查询只返回一个输出）
                    var output = queryResult.Output.First();
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
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("GreptimeDB查询失败: {StatusCode}, {Error}", response.StatusCode, errorContent);
                
                // 检查是否是数据库不存在的错误
                if (response.StatusCode == System.Net.HttpStatusCode.BadRequest && 
                    errorContent.Contains("Database not found"))
                {
                    _logger.LogWarning("检测到数据库不存在错误，尝试自动初始化: {Database}", _options.Database);
                    
                    // 尝试初始化
                    var initSuccess = await InitializeAsync();
                    if (initSuccess)
                    {
                        _logger.LogInformation("数据库初始化成功，重新执行查询");
                        // 重新执行查询（只重试一次避免无限递归）
                        return await ExecuteQueryInternalAsync(sql);
                    }
                    else
                    {
                        _logger.LogError("数据库初始化失败，返回空结果");
                    }
                }
            }
            
            return new List<Dictionary<string, object>>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "执行GreptimeDB查询失败: {SQL}", sql);
            return new List<Dictionary<string, object>>();
        }
    }
    
    /// <summary>
    /// 执行查询SQL（内部方法，不进行自动初始化）
    /// </summary>
    private async Task<List<Dictionary<string, object>>> ExecuteQueryInternalAsync(string sql)
    {
        try
        {
            // GreptimeDB期望form-urlencoded格式
            var formParams = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("sql", sql)
            };
            var content = new FormUrlEncodedContent(formParams);
            
            var response = await _httpClient.PostAsync($"/v1/sql?db={_options.Database}", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var queryResult = JsonConvert.DeserializeObject<GreptimeDbQueryResponse>(responseContent);
                
                if (queryResult?.Output != null && queryResult.Output.Any())
                {
                    var results = new List<Dictionary<string, object>>();
                    
                    // 处理第一个输出项（通常查询只返回一个输出）
                    var output = queryResult.Output.First();
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
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("GreptimeDB查询失败（内部重试）: {StatusCode}, {Error}", response.StatusCode, errorContent);
            }
            
            return new List<Dictionary<string, object>>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "执行GreptimeDB查询失败（内部重试）: {SQL}", sql);
            return new List<Dictionary<string, object>>();
        }
    }
    
    /// <summary>
    /// 转义SQL字符串
    /// </summary>
    private string EscapeSqlString(string? input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return "";
        }
        
        return input.Replace("'", "''").Replace("\\", "\\\\");
    }
    
    /// <summary>
    /// 将时间戳转换为DateTime
    /// </summary>
    private DateTime ConvertToDateTime(object? value)
    {
        if (value == null)
        {
            return DateTime.MinValue;
        }
        
        // 如果已经是DateTime，直接返回
        if (value is DateTime dateTime)
        {
            return dateTime;
        }
        
        // 如果是字符串，尝试解析
        if (value is string str && DateTime.TryParse(str, out var parsedDateTime))
        {
            return parsedDateTime;
        }
        
        // 如果是数值类型（毫秒时间戳），转换为DateTime
        if (value is long longValue)
        {
            try
            {
                // GreptimeDB 返回的是毫秒时间戳
                return DateTimeOffset.FromUnixTimeMilliseconds(longValue).DateTime;
            }
            catch
            {
                return DateTime.MinValue;
            }
        }
        
        // 尝试转换其他数值类型
        if (long.TryParse(value.ToString(), out var numericValue))
        {
            try
            {
                return DateTimeOffset.FromUnixTimeMilliseconds(numericValue).DateTime;
            }
            catch
            {
                return DateTime.MinValue;
            }
        }
        
        return DateTime.MinValue;
    }
    
    /// <summary>
    /// 安全转换为 Int64
    /// </summary>
    private long ConvertToInt64(object? value)
    {
        if (value == null) return 0L;
        
        if (value is long longValue) return longValue;
        if (value is int intValue) return intValue;
        if (value is short shortValue) return shortValue;
        if (value is byte byteValue) return byteValue;
        
        if (long.TryParse(value.ToString(), out var result))
            return result;
            
        return 0L;
    }
    
    /// <summary>
    /// 安全转换为 Int32
    /// </summary>
    private int ConvertToInt32(object? value)
    {
        if (value == null) return 0;
        
        if (value is int intValue) return intValue;
        if (value is long longValue) return (int)Math.Min(longValue, int.MaxValue);
        if (value is short shortValue) return shortValue;
        if (value is byte byteValue) return byteValue;
        
        if (int.TryParse(value.ToString(), out var result))
            return result;
            
        return 0;
    }
    
    /// <summary>
    /// 安全转换为 Boolean
    /// </summary>
    private bool ConvertToBoolean(object? value)
    {
        if (value == null) return false;
        
        if (value is bool boolValue) return boolValue;
        
        // GreptimeDB 可能返回数值（0 = false, 非0 = true）
        if (value is long longValue) return longValue != 0;
        if (value is int intValue) return intValue != 0;
        
        // 尝试解析字符串
        var str = value.ToString()?.ToLower();
        return str == "true" || str == "1" || str == "yes" || str == "on";
    }
    
    
    /// <summary>
    /// 获取SQL字段名
    /// </summary>
    private string GetSqlFieldName(string dtoFieldName)
    {
        return dtoFieldName switch
        {
            "Id" => "audit_id",
            "UserId" => "user_id",
            "UserName" => "user_name",
            "IpAddress" => "ip_address",
            "OperationTime" => "operation_time",
            "OperationType" => "operation_type",
            "Description" => "description",
            "RequestPath" => "request_path",
            "RequestMethod" => "request_method",
            "ExecutionDuration" => "execution_duration",
            "IsSuccess" => "is_success",
            "StatusCode" => "status_code",
            _ => "operation_time"
        };
    }
    
    
    /// <summary>
    /// 将查询结果映射为AuditLog对象
    /// </summary>
    private AuditLog MapToAuditLog(Dictionary<string, object> record)
    {
        var auditLog = new AuditLog
        {
            Id = record.GetValueOrDefault("audit_id", "").ToString() ?? "",
            UserId = record.GetValueOrDefault("user_id", "").ToString() ?? "",
            UserName = record.GetValueOrDefault("user_name", "").ToString() ?? "",
            IpAddress = record.GetValueOrDefault("ip_address", "").ToString() ?? "",
            OperationTime = ConvertToDateTime(record.GetValueOrDefault("operation_time", 0L)),
            OperationType = record.GetValueOrDefault("operation_type", "").ToString() ?? "",
            Description = record.GetValueOrDefault("description", "").ToString() ?? "",
            RequestPath = record.GetValueOrDefault("request_path", "").ToString() ?? "",
            RequestMethod = record.GetValueOrDefault("request_method", "").ToString() ?? "",
            RequestParams = record.GetValueOrDefault("request_params", "").ToString() ?? "",
            ExecutionDuration = ConvertToInt64(record.GetValueOrDefault("execution_duration", 0L)),
            IsSuccess = ConvertToBoolean(record.GetValueOrDefault("is_success", true)),
            ErrorMessage = record.GetValueOrDefault("error_message", "").ToString() ?? "",
            StatusCode = ConvertToInt32(record.GetValueOrDefault("status_code", 200)),
            BeforeData = record.GetValueOrDefault("before_data", "").ToString() ?? "",
            AfterData = record.GetValueOrDefault("after_data", "").ToString() ?? "",
            UserAgent = record.GetValueOrDefault("user_agent", "").ToString() ?? "",
            OperationName = record.GetValueOrDefault("operation_name", "").ToString() ?? "",
            TenantId = record.GetValueOrDefault("tenant_id", "").ToString() ?? ""
        };

        // 反序列化AdditionalData
        var additionalDataJson = record.GetValueOrDefault("additional_data", "").ToString();
        if (!string.IsNullOrEmpty(additionalDataJson))
        {
            try
            {
                // 检查是否包含损坏的JsonElement数据
                if (additionalDataJson.Contains("\"ValueKind\""))
                {
                    _logger.LogWarning("检测到损坏的历史数据，跳过反序列化。记录ID: {AuditId}, 操作时间: {OperationTime}, 表名: {TableName}, JSON: {Json}", 
                        auditLog.Id, auditLog.OperationTime, GetFinalTableName(), additionalDataJson);
                    auditLog.AdditionalData = new Dictionary<string, object>();
                    return auditLog;
                }
                
                // 使用Newtonsoft.Json反序列化
                auditLog.AdditionalData = JsonConvert.DeserializeObject<Dictionary<string, object>>(additionalDataJson) 
                    ?? new Dictionary<string, object>();
                    
                _logger.LogDebug("成功反序列化AdditionalData，包含 {Count} 个字段", auditLog.AdditionalData.Count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "反序列化AdditionalData失败: {Json}", additionalDataJson);
                auditLog.AdditionalData = new Dictionary<string, object>();
            }
        }

        // 反序列化AttributeProperties
        var attributePropertiesJson = record.GetValueOrDefault("attribute_properties", "").ToString();
        if (!string.IsNullOrEmpty(attributePropertiesJson))
        {
            try
            {
                // 检查是否包含损坏的JsonElement数据
                if (attributePropertiesJson.Contains("\"ValueKind\""))
                {
                    _logger.LogWarning("检测到损坏的AttributeProperties历史数据，跳过反序列化: {Json}", attributePropertiesJson);
                    auditLog.AttributeProperties = new Dictionary<string, string>();
                }
                else
                {
                    auditLog.AttributeProperties = JsonConvert.DeserializeObject<Dictionary<string, string>>(attributePropertiesJson) 
                        ?? new Dictionary<string, string>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "反序列化AttributeProperties失败: {Json}", attributePropertiesJson);
                auditLog.AttributeProperties = new Dictionary<string, string>();
            }
        }

        return auditLog;
    }
    
    /// <summary>
    /// 将集合分割为指定大小的批次
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
    
    #endregion
}

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
/// 记录
/// </summary>
public class Records
{
    public RecordSchema? Schema { get; set; }
    public List<List<object>>? Rows { get; set; }
    public int Total_rows { get; set; }
}

/// <summary>
/// 记录架构
/// </summary>
public class RecordSchema
{
    public List<ColumnSchema>? Column_schemas { get; set; }
}

/// <summary>
/// 列架构
/// </summary>
public class ColumnSchema
{
    public string Name { get; set; } = "";
    public string Data_type { get; set; } = "";
}

#endregion
