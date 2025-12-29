using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using CodeSpirit.MultiTenant.Abstractions;
using Newtonsoft.Json;
using CodeSpirit.Core;

namespace CodeSpirit.Web.Pages.Exam
{
    /// <summary>
    /// 考试监控大屏页面模型 - 基于 AmisCards 实现
    /// </summary>
    public class ExamMonitorModel : PageModel
    {
        private readonly ILogger<ExamMonitorModel> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITenantContext _tenantContext;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logger">日志记录器</param>
        /// <param name="httpClientFactory">HTTP客户端工厂</param>
        /// <param name="tenantContext">租户上下文</param>
        public ExamMonitorModel(
            ILogger<ExamMonitorModel> logger,
            IHttpClientFactory httpClientFactory,
            ITenantContext tenantContext)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _tenantContext = tenantContext;
        }

        /// <summary>
        /// 租户ID
        /// </summary>
        public string TenantId { get; set; } = string.Empty;

        /// <summary>
        /// 考试ID
        /// </summary>
        public string ExamId { get; set; } = string.Empty;

        /// <summary>
        /// 租户名称
        /// </summary>
        public string TenantName { get; set; } = string.Empty;

        /// <summary>
        /// 考试名称
        /// </summary>
        public string ExamName { get; set; } = string.Empty;

        /// <summary>
        /// 页面GET请求处理
        /// </summary>
        /// <param name="tenantId">租户ID</param>
        /// <param name="examId">考试ID</param>
        /// <returns>页面结果</returns>
        public async Task<IActionResult> OnGet(string tenantId, string examId)
        {
            if (string.IsNullOrEmpty(tenantId))
            {
                _logger.LogWarning("[考试监控大屏] 缺少租户ID");
                return BadRequest("缺少租户ID参数");
            }

            if (string.IsNullOrEmpty(examId))
            {
                _logger.LogWarning("[考试监控大屏] 缺少考试ID");
                return BadRequest("缺少考试ID参数");
            }

            TenantId = tenantId;
            ExamId = examId;
            
            try
            {
                // 获取租户信息
                var tenantInfo = await _tenantContext.GetCurrentTenantInfoAsync();
                if (tenantInfo != null)
                {
                    TenantName = tenantInfo.DisplayName ?? tenantInfo.Name ?? "未知租户";
                    _logger.LogInformation("[考试监控大屏] 成功获取租户信息: {TenantName}", TenantName);
                }
                else
                {
                    // 如果无法从上下文获取，尝试从IdentityApi获取
                    TenantName = await GetTenantNameFromApi(tenantId);
                }

                // 获取考试信息
                ExamName = await GetExamNameFromApi(tenantId, examId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[考试监控大屏] 获取租户或考试信息时发生错误，租户ID: {TenantId}, 考试ID: {ExamId}", tenantId, examId);
                // 使用默认值
                TenantName = TenantName ?? "未知租户";
                ExamName = ExamName ?? "未知考试";
            }
            
            // 设置页面信息
            ViewData["Title"] = "考试监控大屏";
            ViewData["TenantId"] = tenantId;
            ViewData["ExamId"] = examId;
            ViewData["TenantName"] = TenantName;
            ViewData["ExamName"] = ExamName;
            
            _logger.LogInformation("[考试监控大屏] 初始化监控大屏，租户ID: {TenantId}, 考试ID: {ExamId}, 租户名称: {TenantName}, 考试名称: {ExamName}", 
                tenantId, examId, TenantName, ExamName);
            
            return Page();
        }

        /// <summary>
        /// 从IdentityApi获取租户名称
        /// </summary>
        /// <param name="tenantId">租户ID</param>
        /// <returns>租户名称</returns>
        private async Task<string> GetTenantNameFromApi(string tenantId)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                httpClient.BaseAddress = new Uri("http://identity");
                
                var response = await httpClient.GetAsync($"/api/identity/internal/tenants/{tenantId}");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<TenantInfoDto>>(content);
                    
                    if (apiResponse?.Status == 0 && apiResponse.Data != null)
                    {
                        var displayName = apiResponse.Data.DisplayName ?? apiResponse.Data.Name;
                        _logger.LogInformation("[考试监控大屏] 从IdentityApi获取租户名称: {TenantName}", displayName);
                        return displayName;
                    }
                }
                
                _logger.LogWarning("[考试监控大屏] 无法从IdentityApi获取租户信息，租户ID: {TenantId}, 状态码: {StatusCode}", 
                    tenantId, response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[考试监控大屏] 从IdentityApi获取租户名称时发生异常，租户ID: {TenantId}", tenantId);
            }
            
            return "未知租户";
        }

        /// <summary>
        /// 从ExamApi获取考试名称
        /// </summary>
        /// <param name="tenantId">租户ID</param>
        /// <param name="examId">考试ID</param>
        /// <returns>考试名称</returns>
        private async Task<string> GetExamNameFromApi(string tenantId, string examId)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                httpClient.BaseAddress = new Uri("http://exam");
                httpClient.DefaultRequestHeaders.Add("X-Tenant-ID", tenantId);
                
                var response = await httpClient.GetAsync($"/api/exam-settings/{examId}");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<ExamSettingDetailDto>>(content);
                    
                    if (apiResponse?.Status == 0 && apiResponse.Data != null)
                    {
                        _logger.LogInformation("[考试监控大屏] 从ExamApi获取考试名称: {ExamName}", apiResponse.Data.Name);
                        return apiResponse.Data.Name;
                    }
                }
                
                _logger.LogWarning("[考试监控大屏] 无法从ExamApi获取考试信息，考试ID: {ExamId}, 状态码: {StatusCode}", 
                    examId, response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[考试监控大屏] 从ExamApi获取考试名称时发生异常，考试ID: {ExamId}", examId);
            }
            
            return "未知考试";
        }

        /// <summary>
        /// 租户信息DTO（用于API响应反序列化）
        /// </summary>
        private class TenantInfoDto
        {
            public string TenantId { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string? DisplayName { get; set; }
        }

        /// <summary>
        /// 考试设置详情DTO（用于API响应反序列化）
        /// </summary>
        private class ExamSettingDetailDto
        {
            public long Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string? Description { get; set; }
        }
    }
} 