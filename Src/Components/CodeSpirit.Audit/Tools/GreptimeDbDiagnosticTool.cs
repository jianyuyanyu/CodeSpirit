using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;
using CodeSpirit.Audit.Models;

namespace CodeSpirit.Audit.Tools;

/// <summary>
/// GreptimeDB诊断工具
/// </summary>
public class GreptimeDbDiagnosticTool
{
    private readonly ILogger<GreptimeDbDiagnosticTool> _logger;
    private readonly HttpClient _httpClient;
    private readonly GreptimeDbOptions _options;

    /// <summary>
    /// 构造函数
    /// </summary>
    public GreptimeDbDiagnosticTool(ILogger<GreptimeDbDiagnosticTool> logger, IConfiguration configuration)
    {
        _logger = logger;
        _httpClient = new HttpClient();
        
        // 绑定GreptimeDB配置
        _options = new GreptimeDbOptions();
        configuration.GetSection("Audit:GreptimeDB").Bind(_options);
        
        // 如果配置为空，使用默认值
        if (string.IsNullOrEmpty(_options.Url))
        {
            _options.Url = "http://localhost:4000";
            _logger.LogWarning("GreptimeDB URL未配置，使用默认值: {Url}", _options.Url);
        }
        
        if (string.IsNullOrEmpty(_options.Database))
        {
            _options.Database = "audit_logs";
            _logger.LogWarning("GreptimeDB Database未配置，使用默认值: {Database}", _options.Database);
        }
        
        _httpClient.BaseAddress = new Uri(_options.Url);
        _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
        
        _logger.LogInformation("GreptimeDB诊断工具初始化完成 - URL: {Url}, Database: {Database}", 
            _options.Url, _options.Database);
    }

    /// <summary>
    /// 运行完整诊断
    /// </summary>
    public async Task<DiagnosticResult> RunFullDiagnosticAsync()
    {
        var result = new DiagnosticResult();
        
        _logger.LogInformation("开始运行GreptimeDB完整诊断...");
        
        // 1. 基础连接测试
        result.ConnectionTest = await TestBasicConnectionAsync();
        
        // 2. 健康检查端点测试
        result.HealthEndpointTest = await TestHealthEndpointAsync();
        
        // 3. SQL查询测试
        result.SqlQueryTest = await TestSqlQueryAsync();
        
        // 4. 数据库创建测试
        result.DatabaseCreationTest = await TestDatabaseCreationAsync();
        
        // 5. 表创建测试
        result.TableCreationTest = await TestTableCreationAsync();
        
        // 6. 写入测试
        result.WriteTest = await TestWriteAsync();
        
        _logger.LogInformation("GreptimeDB诊断完成，结果: {Result}", JsonConvert.SerializeObject(result, Formatting.Indented));
        
        return result;
    }

    /// <summary>
    /// 测试基础连接
    /// </summary>
    private async Task<TestResult> TestBasicConnectionAsync()
    {
        try
        {
            _logger.LogInformation("测试基础连接到: {Url}", _options.Url);
            
            var response = await _httpClient.GetAsync("/");
            
            return new TestResult
            {
                Success = response.IsSuccessStatusCode,
                Message = $"状态码: {response.StatusCode}",
                Details = $"响应头: {string.Join(", ", response.Headers.Select(h => $"{h.Key}={string.Join(",", h.Value)}"))}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "基础连接测试失败");
            return new TestResult
            {
                Success = false,
                Message = ex.Message,
                Details = ex.ToString()
            };
        }
    }

    /// <summary>
    /// 测试健康检查端点
    /// </summary>
    private async Task<TestResult> TestHealthEndpointAsync()
    {
        try
        {
            _logger.LogInformation("测试健康检查端点: /health");
            
            var response = await _httpClient.GetAsync("/health");
            var content = await response.Content.ReadAsStringAsync();
            
            return new TestResult
            {
                Success = response.IsSuccessStatusCode,
                Message = $"状态码: {response.StatusCode}",
                Details = $"响应内容: {content}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "健康检查端点测试失败");
            return new TestResult
            {
                Success = false,
                Message = ex.Message,
                Details = ex.ToString()
            };
        }
    }

    /// <summary>
    /// 测试SQL查询
    /// </summary>
    private async Task<TestResult> TestSqlQueryAsync()
    {
        try
        {
            _logger.LogInformation("测试SQL查询功能");
            
            var requestBody = new { sql = "SELECT 1 as test" };
            var json = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"/v1/sql?db={_options.Database}", content);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            return new TestResult
            {
                Success = response.IsSuccessStatusCode,
                Message = $"状态码: {response.StatusCode}",
                Details = $"响应内容: {responseContent}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SQL查询测试失败");
            return new TestResult
            {
                Success = false,
                Message = ex.Message,
                Details = ex.ToString()
            };
        }
    }

    /// <summary>
    /// 测试数据库创建
    /// </summary>
    private async Task<TestResult> TestDatabaseCreationAsync()
    {
        try
        {
            _logger.LogInformation("测试数据库创建: {Database}", _options.Database);
            
            var createDbSql = $"CREATE DATABASE IF NOT EXISTS {_options.Database}";
            var requestBody = new { sql = createDbSql };
            var json = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("/v1/sql", content);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            return new TestResult
            {
                Success = response.IsSuccessStatusCode,
                Message = $"状态码: {response.StatusCode}",
                Details = $"响应内容: {responseContent}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "数据库创建测试失败");
            return new TestResult
            {
                Success = false,
                Message = ex.Message,
                Details = ex.ToString()
            };
        }
    }

    /// <summary>
    /// 测试表创建
    /// </summary>
    private async Task<TestResult> TestTableCreationAsync()
    {
        try
        {
            _logger.LogInformation("测试表创建");
            
            var tableName = $"{_options.TablePrefix}_{_options.TableName}";
            var createTableSql = $@"
                CREATE TABLE IF NOT EXISTS {tableName} (
                    id STRING,
                    user_id STRING,
                    user_name STRING,
                    ip_address STRING,
                    operation_time TIMESTAMP TIME INDEX,
                    service_name STRING,
                    controller_name STRING,
                    action_name STRING,
                    operation_type STRING,
                    description STRING,
                    request_path STRING,
                    request_method STRING,
                    request_params TEXT,
                    entity_name STRING,
                    entity_id STRING,
                    execution_duration BIGINT,
                    is_success BOOLEAN,
                    error_message TEXT,
                    status_code INT,
                    before_data TEXT,
                    after_data TEXT,
                    user_agent TEXT,
                    operation_name STRING,
                    tenant_id STRING,
                    PRIMARY KEY (id)
                )";
            
            var requestBody = new { sql = createTableSql };
            var json = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"/v1/sql?db={_options.Database}", content);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            return new TestResult
            {
                Success = response.IsSuccessStatusCode,
                Message = $"状态码: {response.StatusCode}",
                Details = $"响应内容: {responseContent}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "表创建测试失败");
            return new TestResult
            {
                Success = false,
                Message = ex.Message,
                Details = ex.ToString()
            };
        }
    }

    /// <summary>
    /// 测试写入
    /// </summary>
    private async Task<TestResult> TestWriteAsync()
    {
        try
        {
            _logger.LogInformation("测试数据写入");
            
            var tableName = $"{_options.TablePrefix}_{_options.TableName}";
            var testId = $"test-{DateTime.UtcNow:yyyyMMddHHmmss}";
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
            
            var insertSql = $@"
                INSERT INTO {tableName} (
                    id, user_id, user_name, ip_address, operation_time,
                    service_name, controller_name, action_name, operation_type, description,
                    request_path, request_method, request_params, entity_name, entity_id,
                    execution_duration, is_success, error_message, status_code,
                    before_data, after_data, user_agent, operation_name, tenant_id
                ) VALUES (
                    '{testId}',
                    'test-user',
                    'Test User',
                    '127.0.0.1',
                    '{timestamp}',
                    'DiagnosticService',
                    'DiagnosticController',
                    'TestAction',
                    'Test',
                    'GreptimeDB连接测试',
                    '/api/diagnostic/test',
                    'POST',
                    '{{""test"": true}}',
                    'DiagnosticEntity',
                    'test-entity-1',
                    100,
                    true,
                    '',
                    200,
                    '{{}}',
                    '{{""result"": ""success""}}',
                    'GreptimeDB-Diagnostic-Tool/1.0',
                    '测试操作',
                    'test-tenant'
                )";
            
            var requestBody = new { sql = insertSql };
            var json = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"/v1/sql?db={_options.Database}", content);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            return new TestResult
            {
                Success = response.IsSuccessStatusCode,
                Message = $"状态码: {response.StatusCode}",
                Details = $"响应内容: {responseContent}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "数据写入测试失败");
            return new TestResult
            {
                Success = false,
                Message = ex.Message,
                Details = ex.ToString()
            };
        }
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}

/// <summary>
/// 诊断结果
/// </summary>
public class DiagnosticResult
{
    /// <summary>
    /// 基础连接测试结果
    /// </summary>
    public TestResult ConnectionTest { get; set; } = new();
    
    /// <summary>
    /// 健康检查端点测试结果
    /// </summary>
    public TestResult HealthEndpointTest { get; set; } = new();
    
    /// <summary>
    /// SQL查询测试结果
    /// </summary>
    public TestResult SqlQueryTest { get; set; } = new();
    
    /// <summary>
    /// 数据库创建测试结果
    /// </summary>
    public TestResult DatabaseCreationTest { get; set; } = new();
    
    /// <summary>
    /// 表创建测试结果
    /// </summary>
    public TestResult TableCreationTest { get; set; } = new();
    
    /// <summary>
    /// 写入测试结果
    /// </summary>
    public TestResult WriteTest { get; set; } = new();
    
    /// <summary>
    /// 是否所有测试都通过
    /// </summary>
    public bool AllTestsPassed => ConnectionTest.Success && 
                                 HealthEndpointTest.Success && 
                                 SqlQueryTest.Success && 
                                 DatabaseCreationTest.Success && 
                                 TableCreationTest.Success && 
                                 WriteTest.Success;
}

/// <summary>
/// 测试结果
/// </summary>
public class TestResult
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// 结果消息
    /// </summary>
    public string Message { get; set; } = "";
    
    /// <summary>
    /// 详细信息
    /// </summary>
    public string Details { get; set; } = "";
}
