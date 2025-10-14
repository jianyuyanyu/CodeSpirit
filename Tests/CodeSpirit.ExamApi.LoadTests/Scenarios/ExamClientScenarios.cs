using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NBomber.CSharp;
using Newtonsoft.Json;
using System.Text;
using CodeSpirit.ExamApi.LoadTests.Models;
using CodeSpirit.ExamApi.LoadTests.Services;

namespace CodeSpirit.ExamApi.LoadTests.Scenarios;

/// <summary>
/// 考试客户端测试场景
/// </summary>
public class ExamClientScenarios
{
    private readonly IConfiguration _configuration;
    private readonly Microsoft.Extensions.Logging.ILogger _logger;
    private readonly AuthenticationService _authService;
    private readonly TestUserService _userService;
    private readonly HttpClient _httpClient;
    private readonly List<ClientExamDto> _cachedExams = new();

    public ExamClientScenarios(IConfiguration configuration, Microsoft.Extensions.Logging.ILogger logger, TestUserService userService)
    {
        _configuration = configuration;
        _logger = logger;
        _httpClient = new HttpClient();
        _authService = new AuthenticationService(
            _httpClient, 
            configuration["TestConfiguration:IdentityApiUrl"] ?? "", 
            configuration["TestConfiguration:TenantId"] ?? "");
        _userService = userService;
    }

    /// <summary>
    /// 获取可用考试列表
    /// </summary>
    /// <param name="token">认证令牌</param>
    /// <returns>可用考试列表</returns>
    private async Task<List<ClientExamDto>?> GetAvailableExamsAsync(string token)
    {
        try
        {
            var examApiUrl = _configuration["TestConfiguration:ExamApiUrl"];
            var url = $"{examApiUrl}/api/exam/client/available";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Authorization", $"Bearer {token}");
            request.Headers.Add("X-Tenant-Id", _configuration["TestConfiguration:TenantId"]);

            var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var examResponse = JsonConvert.DeserializeObject<AvailableExamsResponse>(responseContent);
                return examResponse?.Data;
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取可用考试列表失败");
            return null;
        }
    }

    /// <summary>
    /// 获取随机考试ID
    /// </summary>
    /// <param name="token">认证令牌</param>
    /// <returns>考试ID，如果没有可用考试则返回null</returns>
    private async Task<string?> GetRandomExamIdAsync(string token)
    {
        try
        {
            // 如果缓存为空，重新获取
            if (_cachedExams.Count == 0)
            {
                var exams = await GetAvailableExamsAsync(token);
                if (exams != null && exams.Count > 0)
                {
                    _cachedExams.Clear();
                    _cachedExams.AddRange(exams);
                }
            }

            if (_cachedExams.Count > 0)
            {
                var randomExam = _cachedExams[Random.Shared.Next(_cachedExams.Count)];
                return randomExam.Id;
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取随机考试ID失败");
            return null;
        }
    }

    /// <summary>
    /// 创建获取用户档案场景
    /// </summary>
    public NBomber.Contracts.ScenarioProps CreateGetProfileScenario()
    {
        return Scenario.Create("get_profile", async context =>
        {
            var startTime = DateTime.UtcNow;
            string username = "";
            
            try
            {
                var user = _userService.GetRandomUser();
                if (user == null)
                {
                    _logger.LogWarning("[get_profile] 没有可用的测试用户");
                    return Response.Fail();
                }

                username = user.Username;
                _logger.LogDebug("[get_profile] 开始执行用户档案获取，用户: {Username}", username);

                var token = await _authService.GetAuthTokenAsync(user.Username, user.Password);
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("[get_profile] 用户认证失败，用户: {Username}", username);
                    return Response.Fail();
                }

                _logger.LogDebug("[get_profile] 用户认证成功，用户: {Username}", username);

                var examApiUrl = _configuration["TestConfiguration:ExamApiUrl"];
                var url = $"{examApiUrl}/api/exam/client/profile";
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("Authorization", $"Bearer {token}");
                request.Headers.Add("X-Tenant-Id", _configuration["TestConfiguration:TenantId"]);

                _logger.LogDebug("[get_profile] 发送请求到: {Url}, 用户: {Username}", url, username);

                var response = await _httpClient.SendAsync(request);
                var duration = DateTime.UtcNow - startTime;

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("[get_profile] 请求成功，用户: {Username}, 状态码: {StatusCode}, 耗时: {Duration}ms", 
                        username, response.StatusCode, duration.TotalMilliseconds);
                    return Response.Ok();
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("[get_profile] 请求失败，用户: {Username}, 状态码: {StatusCode}, 响应: {Response}, 耗时: {Duration}ms", 
                        username, response.StatusCode, errorContent, duration.TotalMilliseconds);
                    return Response.Fail();
                }
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;
                _logger.LogError(ex, "[get_profile] 执行异常，用户: {Username}, 耗时: {Duration}ms", username, duration.TotalMilliseconds);
                return Response.Fail();
            }
        });
    }

    /// <summary>
    /// 创建获取可用考试场景
    /// </summary>
    public NBomber.Contracts.ScenarioProps CreateGetAvailableExamsScenario()
    {
        return Scenario.Create("get_available_exams", async context =>
        {
            var startTime = DateTime.UtcNow;
            string username = "";
            
            try
            {
                var user = _userService.GetRandomUser();
                if (user == null)
                {
                    _logger.LogWarning("[get_available_exams] 没有可用的测试用户");
                    return Response.Fail();
                }

                username = user.Username;
                _logger.LogDebug("[get_available_exams] 开始获取可用考试，用户: {Username}", username);

                var token = await _authService.GetAuthTokenAsync(user.Username, user.Password);
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("[get_available_exams] 用户认证失败，用户: {Username}", username);
                    return Response.Fail();
                }

                _logger.LogDebug("[get_available_exams] 用户认证成功，用户: {Username}", username);

                var examApiUrl = _configuration["TestConfiguration:ExamApiUrl"];
                var url = $"{examApiUrl}/api/exam/client/available";
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("Authorization", $"Bearer {token}");
                request.Headers.Add("X-Tenant-Id", _configuration["TestConfiguration:TenantId"]);

                _logger.LogDebug("[get_available_exams] 发送请求到: {Url}, 用户: {Username}", url, username);

                var response = await _httpClient.SendAsync(request);
                var duration = DateTime.UtcNow - startTime;

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("[get_available_exams] 请求成功，用户: {Username}, 状态码: {StatusCode}, 耗时: {Duration}ms, 响应长度: {Length}", 
                        username, response.StatusCode, duration.TotalMilliseconds, responseContent.Length);
                    return Response.Ok();
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("[get_available_exams] 请求失败，用户: {Username}, 状态码: {StatusCode}, 响应: {Response}, 耗时: {Duration}ms", 
                        username, response.StatusCode, errorContent, duration.TotalMilliseconds);
                    return Response.Fail();
                }
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;
                _logger.LogError(ex, "[get_available_exams] 执行异常，用户: {Username}, 耗时: {Duration}ms", username, duration.TotalMilliseconds);
                return Response.Fail();
            }
        });
    }

    /// <summary>
    /// 创建获取考试基本信息场景
    /// </summary>
    public NBomber.Contracts.ScenarioProps CreateGetExamBasicInfoScenario()
    {
        return Scenario.Create("get_exam_basic_info", async context =>
        {
            var startTime = DateTime.UtcNow;
            string username = "";
            string? examId = null;
            
            try
            {
                var user = _userService.GetRandomUser();
                if (user == null)
                {
                    _logger.LogWarning("[get_exam_basic_info] 没有可用的测试用户");
                    return Response.Fail();
                }

                username = user.Username;
                _logger.LogDebug("[get_exam_basic_info] 开始获取考试基本信息，用户: {Username}", username);

                var token = await _authService.GetAuthTokenAsync(user.Username, user.Password);
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("[get_exam_basic_info] 用户认证失败，用户: {Username}", username);
                    return Response.Fail();
                }

                _logger.LogDebug("[get_exam_basic_info] 用户认证成功，用户: {Username}", username);

                // 获取真实的考试ID
                examId = await GetRandomExamIdAsync(token);
                if (string.IsNullOrEmpty(examId))
                {
                    _logger.LogWarning("[get_exam_basic_info] 没有可用的考试，用户: {Username}", username);
                    return Response.Fail();
                }

                _logger.LogDebug("[get_exam_basic_info] 获取到考试ID: {ExamId}, 用户: {Username}", examId, username);

                var examApiUrl = _configuration["TestConfiguration:ExamApiUrl"];
                var url = $"{examApiUrl}/api/exam/client/{examId}/basic";
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("Authorization", $"Bearer {token}");
                request.Headers.Add("X-Tenant-Id", _configuration["TestConfiguration:TenantId"]);

                _logger.LogDebug("[get_exam_basic_info] 发送请求到: {Url}, 用户: {Username}", url, username);

                var response = await _httpClient.SendAsync(request);
                var duration = DateTime.UtcNow - startTime;

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("[get_exam_basic_info] 请求成功，用户: {Username}, 考试ID: {ExamId}, 状态码: {StatusCode}, 耗时: {Duration}ms", 
                        username, examId, response.StatusCode, duration.TotalMilliseconds);
                    return Response.Ok();
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("[get_exam_basic_info] 请求失败，用户: {Username}, 考试ID: {ExamId}, 状态码: {StatusCode}, 响应: {Response}, 耗时: {Duration}ms", 
                        username, examId, response.StatusCode, errorContent, duration.TotalMilliseconds);
                    return Response.Fail();
                }
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;
                _logger.LogError(ex, "[get_exam_basic_info] 执行异常，用户: {Username}, 考试ID: {ExamId}, 耗时: {Duration}ms", username, examId, duration.TotalMilliseconds);
                return Response.Fail();
            }
        });
    }

    /// <summary>
    /// 创建获取考试轻量信息场景
    /// </summary>
    public NBomber.Contracts.ScenarioProps CreateGetExamLightInfoScenario()
    {
        return Scenario.Create("get_exam_light_info", async context =>
        {
            var startTime = DateTime.UtcNow;
            string username = "";
            string? examId = null;
            
            try
            {
                var user = _userService.GetRandomUser();
                if (user == null)
                {
                    _logger.LogWarning("[get_exam_light_info] 没有可用的测试用户");
                    return Response.Fail();
                }

                username = user.Username;
                _logger.LogDebug("[get_exam_light_info] 开始获取考试轻量信息，用户: {Username}", username);

                var token = await _authService.GetAuthTokenAsync(user.Username, user.Password);
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("[get_exam_light_info] 用户认证失败，用户: {Username}", username);
                    return Response.Fail();
                }

                _logger.LogDebug("[get_exam_light_info] 用户认证成功，用户: {Username}", username);

                // 获取真实的考试ID
                examId = await GetRandomExamIdAsync(token);
                if (string.IsNullOrEmpty(examId))
                {
                    _logger.LogWarning("[get_exam_light_info] 没有可用的考试，用户: {Username}", username);
                    return Response.Fail();
                }

                _logger.LogDebug("[get_exam_light_info] 获取到考试ID: {ExamId}, 用户: {Username}", examId, username);

                var examApiUrl = _configuration["TestConfiguration:ExamApiUrl"];
                var url = $"{examApiUrl}/api/exam/client/{examId}/light";
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("Authorization", $"Bearer {token}");
                request.Headers.Add("X-Tenant-Id", _configuration["TestConfiguration:TenantId"]);

                _logger.LogDebug("[get_exam_light_info] 发送请求到: {Url}, 用户: {Username}", url, username);

                var response = await _httpClient.SendAsync(request);
                var duration = DateTime.UtcNow - startTime;

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("[get_exam_light_info] 请求成功，用户: {Username}, 考试ID: {ExamId}, 状态码: {StatusCode}, 耗时: {Duration}ms", 
                        username, examId, response.StatusCode, duration.TotalMilliseconds);
                    return Response.Ok();
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("[get_exam_light_info] 请求失败，用户: {Username}, 考试ID: {ExamId}, 状态码: {StatusCode}, 响应: {Response}, 耗时: {Duration}ms", 
                        username, examId, response.StatusCode, errorContent, duration.TotalMilliseconds);
                    return Response.Fail();
                }
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;
                _logger.LogError(ex, "[get_exam_light_info] 执行异常，用户: {Username}, 考试ID: {ExamId}, 耗时: {Duration}ms", username, examId, duration.TotalMilliseconds);
                return Response.Fail();
            }
        });
    }

    /// <summary>
    /// 创建开始考试场景
    /// </summary>
    public NBomber.Contracts.ScenarioProps CreateStartExamScenario()
    {
        return Scenario.Create("start_exam", async context =>
        {
            var startTime = DateTime.UtcNow;
            string username = "";
            string? examId = null;
            
            try
            {
                var user = _userService.GetRandomUser();
                if (user == null)
                {
                    _logger.LogWarning("[start_exam] 没有可用的测试用户");
                    return Response.Fail();
                }

                username = user.Username;
                _logger.LogDebug("[start_exam] 开始考试，用户: {Username}", username);

                var token = await _authService.GetAuthTokenAsync(user.Username, user.Password);
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("[start_exam] 用户认证失败，用户: {Username}", username);
                    return Response.Fail();
                }

                _logger.LogDebug("[start_exam] 用户认证成功，用户: {Username}", username);

                // 获取真实的考试ID
                examId = await GetRandomExamIdAsync(token);
                if (string.IsNullOrEmpty(examId))
                {
                    _logger.LogWarning("[start_exam] 没有可用的考试，用户: {Username}", username);
                    return Response.Fail();
                }

                _logger.LogDebug("[start_exam] 获取到考试ID: {ExamId}, 用户: {Username}", examId, username);

                var examApiUrl = _configuration["TestConfiguration:ExamApiUrl"];
                var url = $"{examApiUrl}/api/exam/client/{examId}/start";
                var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Add("Authorization", $"Bearer {token}");
                request.Headers.Add("X-Tenant-Id", _configuration["TestConfiguration:TenantId"]);

                _logger.LogDebug("[start_exam] 发送请求到: {Url}, 用户: {Username}", url, username);

                var response = await _httpClient.SendAsync(request);
                var duration = DateTime.UtcNow - startTime;

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("[start_exam] 请求成功，用户: {Username}, 考试ID: {ExamId}, 状态码: {StatusCode}, 耗时: {Duration}ms", 
                        username, examId, response.StatusCode, duration.TotalMilliseconds);
                    return Response.Ok();
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("[start_exam] 请求失败，用户: {Username}, 考试ID: {ExamId}, 状态码: {StatusCode}, 响应: {Response}, 耗时: {Duration}ms", 
                        username, examId, response.StatusCode, errorContent, duration.TotalMilliseconds);
                    return Response.Fail();
                }
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;
                _logger.LogError(ex, "[start_exam] 执行异常，用户: {Username}, 考试ID: {ExamId}, 耗时: {Duration}ms", username, examId, duration.TotalMilliseconds);
                return Response.Fail();
            }
        });
    }

    /// <summary>
    /// 创建记录切屏场景
    /// </summary>
    public NBomber.Contracts.ScenarioProps CreateRecordScreenSwitchScenario()
    {
        return Scenario.Create("record_screen_switch", async context =>
        {
            var startTime = DateTime.UtcNow;
            string username = "";
            string? examId = null;
            
            try
            {
                var user = _userService.GetRandomUser();
                if (user == null)
                {
                    _logger.LogWarning("[record_screen_switch] 没有可用的测试用户");
                    return Response.Fail();
                }

                username = user.Username;
                _logger.LogDebug("[record_screen_switch] 开始记录切屏，用户: {Username}", username);

                var token = await _authService.GetAuthTokenAsync(user.Username, user.Password);
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("[record_screen_switch] 用户认证失败，用户: {Username}", username);
                    return Response.Fail();
                }

                _logger.LogDebug("[record_screen_switch] 用户认证成功，用户: {Username}", username);

                // 获取真实的考试ID
                examId = await GetRandomExamIdAsync(token);
                if (string.IsNullOrEmpty(examId))
                {
                    _logger.LogWarning("[record_screen_switch] 没有可用的考试，用户: {Username}", username);
                    return Response.Fail();
                }

                _logger.LogDebug("[record_screen_switch] 获取到考试ID: {ExamId}, 用户: {Username}", examId, username);

                var examApiUrl = _configuration["TestConfiguration:ExamApiUrl"];
                var url = $"{examApiUrl}/api/exam/client/{examId}/screen-switch";
                var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Add("Authorization", $"Bearer {token}");
                request.Headers.Add("X-Tenant-Id", _configuration["TestConfiguration:TenantId"]);

                _logger.LogDebug("[record_screen_switch] 发送请求到: {Url}, 用户: {Username}", url, username);

                var response = await _httpClient.SendAsync(request);
                var duration = DateTime.UtcNow - startTime;

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("[record_screen_switch] 请求成功，用户: {Username}, 考试ID: {ExamId}, 状态码: {StatusCode}, 耗时: {Duration}ms", 
                        username, examId, response.StatusCode, duration.TotalMilliseconds);
                    return Response.Ok();
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("[record_screen_switch] 请求失败，用户: {Username}, 考试ID: {ExamId}, 状态码: {StatusCode}, 响应: {Response}, 耗时: {Duration}ms", 
                        username, examId, response.StatusCode, errorContent, duration.TotalMilliseconds);
                    return Response.Fail();
                }
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;
                _logger.LogError(ex, "[record_screen_switch] 执行异常，用户: {Username}, 考试ID: {ExamId}, 耗时: {Duration}ms", username, examId, duration.TotalMilliseconds);
                return Response.Fail();
            }
        });
    }

    /// <summary>
    /// 创建混合操作场景
    /// </summary>
    public NBomber.Contracts.ScenarioProps CreateMixedOperationsScenario()
    {
        return Scenario.Create("mixed_operations", async context =>
        {
            var startTime = DateTime.UtcNow;
            string username = "";
            string operation = "";
            
            try
            {
                var user = _userService.GetRandomUser();
                if (user == null)
                {
                    _logger.LogWarning("[mixed_operations] 没有可用的测试用户");
                    return Response.Fail();
                }

                username = user.Username;
                var token = await _authService.GetAuthTokenAsync(user.Username, user.Password);
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("[mixed_operations] 用户认证失败，用户: {Username}", username);
                    return Response.Fail();
                }

                var examApiUrl = _configuration["TestConfiguration:ExamApiUrl"];
                var tenantId = _configuration["TestConfiguration:TenantId"];
                
                // 随机选择一个操作
                var operations = new[]
                {
                    "profile",
                    "available",
                    "basic",
                    "light"
                };
                
                operation = operations[Random.Shared.Next(operations.Length)];
                
                string url;
                if (operation == "basic" || operation == "light")
                {
                    // 对于需要考试ID的操作，获取真实的考试ID
                    var examId = await GetRandomExamIdAsync(token);
                    if (string.IsNullOrEmpty(examId))
                    {
                        _logger.LogWarning("[mixed_operations] 没有可用的考试，用户: {Username}, 操作: {Operation}", username, operation);
                        return Response.Fail();
                    }
                    
                    url = operation switch
                    {
                        "basic" => $"{examApiUrl}/api/exam/client/{examId}/basic",
                        "light" => $"{examApiUrl}/api/exam/client/{examId}/light",
                        _ => $"{examApiUrl}/api/exam/client/profile"
                    };
                    
                    _logger.LogDebug("[mixed_operations] 使用考试ID: {ExamId}, 操作: {Operation}, 用户: {Username}", examId, operation, username);
                }
                else
                {
                    url = operation switch
                    {
                        "profile" => $"{examApiUrl}/api/exam/client/profile",
                        "available" => $"{examApiUrl}/api/exam/client/available",
                        _ => $"{examApiUrl}/api/exam/client/profile"
                    };
                }

                _logger.LogDebug("[mixed_operations] 执行操作: {Operation}, 用户: {Username}, URL: {Url}", operation, username, url);

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("Authorization", $"Bearer {token}");
                request.Headers.Add("X-Tenant-Id", tenantId);

                var response = await _httpClient.SendAsync(request);
                var duration = DateTime.UtcNow - startTime;
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("[mixed_operations] 操作成功: {Operation}, 用户: {Username}, 耗时: {Duration}ms", 
                        operation, username, duration.TotalMilliseconds);
                    return Response.Ok();
                }
                else
                {
                    _logger.LogWarning("[mixed_operations] 操作失败: {Operation}, 用户: {Username}, 状态码: {StatusCode}, 耗时: {Duration}ms", 
                        operation, username, response.StatusCode, duration.TotalMilliseconds);
                    return Response.Fail();
                }
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;
                _logger.LogError(ex, "[mixed_operations] 执行异常: {Operation}, 用户: {Username}, 耗时: {Duration}ms", 
                    operation, username, duration.TotalMilliseconds);
                return Response.Fail();
            }
        });
    }

    /// <summary>
    /// 创建完整考试流程场景
    /// </summary>
    public NBomber.Contracts.ScenarioProps CreateFullExamFlowScenario()
    {
        return Scenario.Create("full_exam_flow", async context =>
        {
            var startTime = DateTime.UtcNow;
            string username = "";
            string? examId = null;
            
            try
            {
                var user = _userService.GetRandomUser();
                if (user == null)
                {
                    _logger.LogWarning("[full_exam_flow] 没有可用的测试用户");
                    return Response.Fail();
                }

                username = user.Username;
                _logger.LogInformation("[full_exam_flow] 开始执行完整考试流程，用户: {Username}", username);

                // 1. 用户登录
                _logger.LogDebug("[full_exam_flow] 步骤1: 用户登录，用户: {Username}", username);
                var loginStartTime = DateTime.UtcNow;
                var token = await _authService.GetAuthTokenAsync(user.Username, user.Password);
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("[full_exam_flow] 步骤1失败: 用户登录失败，用户: {Username}", username);
                    return Response.Fail();
                }
                var loginDuration = DateTime.UtcNow - loginStartTime;
                _logger.LogDebug("[full_exam_flow] 步骤1成功: 用户登录完成，用户: {Username}, 耗时: {Duration}ms", username, loginDuration.TotalMilliseconds);

                var examApiUrl = _configuration["TestConfiguration:ExamApiUrl"];
                var tenantId = _configuration["TestConfiguration:TenantId"];

                // 2. 获取用户档案
                _logger.LogDebug("[full_exam_flow] 步骤2: 获取用户档案，用户: {Username}", username);
                var profileStartTime = DateTime.UtcNow;
                var profileRequest = new HttpRequestMessage(HttpMethod.Get, $"{examApiUrl}/api/exam/client/profile");
                profileRequest.Headers.Add("Authorization", $"Bearer {token}");
                profileRequest.Headers.Add("X-Tenant-Id", tenantId);

                var profileResponse = await _httpClient.SendAsync(profileRequest);
                var profileDuration = DateTime.UtcNow - profileStartTime;
                if (!profileResponse.IsSuccessStatusCode)
                {
                    var profileError = await profileResponse.Content.ReadAsStringAsync();
                    _logger.LogWarning("[full_exam_flow] 步骤2失败: 获取用户档案失败，用户: {Username}, 状态码: {StatusCode}, 响应: {Response}, 耗时: {Duration}ms", 
                        username, profileResponse.StatusCode, profileError, profileDuration.TotalMilliseconds);
                    return Response.Fail();
                }
                _logger.LogDebug("[full_exam_flow] 步骤2成功: 获取用户档案完成，用户: {Username}, 耗时: {Duration}ms", username, profileDuration.TotalMilliseconds);

                // 3. 获取可用考试
                _logger.LogDebug("[full_exam_flow] 步骤3: 获取可用考试，用户: {Username}", username);
                var availableStartTime = DateTime.UtcNow;
                var availableRequest = new HttpRequestMessage(HttpMethod.Get, $"{examApiUrl}/api/exam/client/available");
                availableRequest.Headers.Add("Authorization", $"Bearer {token}");
                availableRequest.Headers.Add("X-Tenant-Id", tenantId);

                var availableResponse = await _httpClient.SendAsync(availableRequest);
                var availableDuration = DateTime.UtcNow - availableStartTime;
                if (!availableResponse.IsSuccessStatusCode)
                {
                    var availableError = await availableResponse.Content.ReadAsStringAsync();
                    _logger.LogWarning("[full_exam_flow] 步骤3失败: 获取可用考试失败，用户: {Username}, 状态码: {StatusCode}, 响应: {Response}, 耗时: {Duration}ms", 
                        username, availableResponse.StatusCode, availableError, availableDuration.TotalMilliseconds);
                    return Response.Fail();
                }
                _logger.LogDebug("[full_exam_flow] 步骤3成功: 获取可用考试完成，用户: {Username}, 耗时: {Duration}ms", username, availableDuration.TotalMilliseconds);

                // 从可用考试响应中获取真实的考试ID
                var availableContent = await availableResponse.Content.ReadAsStringAsync();
                var examResponse = JsonConvert.DeserializeObject<AvailableExamsResponse>(availableContent);
                if (examResponse?.Data == null || examResponse.Data.Count == 0)
                {
                    _logger.LogWarning("[full_exam_flow] 没有可用的考试，用户: {Username}", username);
                    return Response.Fail();
                }
                
                var selectedExam = examResponse.Data[Random.Shared.Next(examResponse.Data.Count)];
                examId = selectedExam.Id;
                _logger.LogDebug("[full_exam_flow] 选择考试ID: {ExamId}, 考试名称: {ExamName}, 用户: {Username}", examId, selectedExam.Name, username);

                // 4. 获取考试轻量信息
                _logger.LogDebug("[full_exam_flow] 步骤4: 获取考试轻量信息，用户: {Username}, 考试ID: {ExamId}", username, examId);
                var lightStartTime = DateTime.UtcNow;
                var lightInfoRequest = new HttpRequestMessage(HttpMethod.Get, $"{examApiUrl}/api/exam/client/{examId}/light");
                lightInfoRequest.Headers.Add("Authorization", $"Bearer {token}");
                lightInfoRequest.Headers.Add("X-Tenant-Id", tenantId);

                var lightInfoResponse = await _httpClient.SendAsync(lightInfoRequest);
                var lightDuration = DateTime.UtcNow - lightStartTime;
                if (!lightInfoResponse.IsSuccessStatusCode)
                {
                    var lightError = await lightInfoResponse.Content.ReadAsStringAsync();
                    _logger.LogWarning("[full_exam_flow] 步骤4失败: 获取考试轻量信息失败，用户: {Username}, 考试ID: {ExamId}, 状态码: {StatusCode}, 响应: {Response}, 耗时: {Duration}ms", 
                        username, examId, lightInfoResponse.StatusCode, lightError, lightDuration.TotalMilliseconds);
                    return Response.Fail();
                }
                _logger.LogDebug("[full_exam_flow] 步骤4成功: 获取考试轻量信息完成，用户: {Username}, 考试ID: {ExamId}, 耗时: {Duration}ms", username, examId, lightDuration.TotalMilliseconds);

                // 5. 获取考试基本信息
                _logger.LogDebug("[full_exam_flow] 步骤5: 获取考试基本信息，用户: {Username}, 考试ID: {ExamId}", username, examId);
                var basicStartTime = DateTime.UtcNow;
                var basicInfoRequest = new HttpRequestMessage(HttpMethod.Get, $"{examApiUrl}/api/exam/client/{examId}/basic");
                basicInfoRequest.Headers.Add("Authorization", $"Bearer {token}");
                basicInfoRequest.Headers.Add("X-Tenant-Id", tenantId);

                var basicInfoResponse = await _httpClient.SendAsync(basicInfoRequest);
                var basicDuration = DateTime.UtcNow - basicStartTime;
                if (!basicInfoResponse.IsSuccessStatusCode)
                {
                    var basicError = await basicInfoResponse.Content.ReadAsStringAsync();
                    _logger.LogWarning("[full_exam_flow] 步骤5失败: 获取考试基本信息失败，用户: {Username}, 考试ID: {ExamId}, 状态码: {StatusCode}, 响应: {Response}, 耗时: {Duration}ms", 
                        username, examId, basicInfoResponse.StatusCode, basicError, basicDuration.TotalMilliseconds);
                    return Response.Fail();
                }
                _logger.LogDebug("[full_exam_flow] 步骤5成功: 获取考试基本信息完成，用户: {Username}, 考试ID: {ExamId}, 耗时: {Duration}ms", username, examId, basicDuration.TotalMilliseconds);

                // 6. 开始考试
                _logger.LogDebug("[full_exam_flow] 步骤6: 开始考试，用户: {Username}, 考试ID: {ExamId}", username, examId);
                var startStartTime = DateTime.UtcNow;
                var startExamRequest = new HttpRequestMessage(HttpMethod.Post, $"{examApiUrl}/api/exam/client/{examId}/start");
                startExamRequest.Headers.Add("Authorization", $"Bearer {token}");
                startExamRequest.Headers.Add("X-Tenant-Id", tenantId);

                var startExamResponse = await _httpClient.SendAsync(startExamRequest);
                var startDuration = DateTime.UtcNow - startStartTime;
                if (!startExamResponse.IsSuccessStatusCode)
                {
                    var startError = await startExamResponse.Content.ReadAsStringAsync();
                    _logger.LogWarning("[full_exam_flow] 步骤6失败: 开始考试失败，用户: {Username}, 考试ID: {ExamId}, 状态码: {StatusCode}, 响应: {Response}, 耗时: {Duration}ms", 
                        username, examId, startExamResponse.StatusCode, startError, startDuration.TotalMilliseconds);
                    return Response.Fail();
                }
                _logger.LogDebug("[full_exam_flow] 步骤6成功: 开始考试完成，用户: {Username}, 考试ID: {ExamId}, 耗时: {Duration}ms", username, examId, startDuration.TotalMilliseconds);

                // 7. 记录切屏（模拟考试过程中的切屏行为）
                _logger.LogDebug("[full_exam_flow] 步骤7: 记录切屏，用户: {Username}, 考试ID: {ExamId}", username, examId);
                var screenStartTime = DateTime.UtcNow;
                var screenSwitchRequest = new HttpRequestMessage(HttpMethod.Post, $"{examApiUrl}/api/exam/client/{examId}/screen-switch");
                screenSwitchRequest.Headers.Add("Authorization", $"Bearer {token}");
                screenSwitchRequest.Headers.Add("X-Tenant-Id", tenantId);

                var screenSwitchResponse = await _httpClient.SendAsync(screenSwitchRequest);
                var screenDuration = DateTime.UtcNow - screenStartTime;
                if (screenSwitchResponse.IsSuccessStatusCode)
                {
                    _logger.LogDebug("[full_exam_flow] 步骤7成功: 记录切屏完成，用户: {Username}, 考试ID: {ExamId}, 耗时: {Duration}ms", username, examId, screenDuration.TotalMilliseconds);
                }
                else
                {
                    var screenError = await screenSwitchResponse.Content.ReadAsStringAsync();
                    _logger.LogDebug("[full_exam_flow] 步骤7警告: 记录切屏失败（不影响流程），用户: {Username}, 考试ID: {ExamId}, 状态码: {StatusCode}, 响应: {Response}, 耗时: {Duration}ms", 
                        username, examId, screenSwitchResponse.StatusCode, screenError, screenDuration.TotalMilliseconds);
                }

                var totalDuration = DateTime.UtcNow - startTime;
                _logger.LogInformation("[full_exam_flow] 完整考试流程执行成功，用户: {Username}, 考试ID: {ExamId}, 总耗时: {TotalDuration}ms", 
                    username, examId, totalDuration.TotalMilliseconds);
                return Response.Ok();
            }
            catch (Exception ex)
            {
                var totalDuration = DateTime.UtcNow - startTime;
                _logger.LogError(ex, "[full_exam_flow] 完整考试流程执行失败，用户: {Username}, 考试ID: {ExamId}, 总耗时: {TotalDuration}ms", 
                    username, examId, totalDuration.TotalMilliseconds);
                return Response.Fail();
            }
        });
    }
}