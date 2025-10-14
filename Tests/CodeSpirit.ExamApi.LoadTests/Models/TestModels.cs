using Newtonsoft.Json;

namespace CodeSpirit.ExamApi.LoadTests.Models;

/// <summary>
/// 测试用户模型
/// </summary>
public class TestUser
{
    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// 缓存的认证Token
    /// </summary>
    public string? AuthToken { get; set; }

    /// <summary>
    /// Token过期时间
    /// </summary>
    public DateTime? TokenExpiry { get; set; }

    /// <summary>
    /// 根据用户名生成密码（取后6位）
    /// </summary>
    /// <param name="username">用户名</param>
    /// <returns>生成的密码</returns>
    public static string GeneratePassword(string username)
    {
        if (string.IsNullOrEmpty(username) || username.Length < 6)
        {
            return username + "123!"; // 如果用户名少于6位，补充默认后缀
        }
        return username.Substring(username.Length - 6);
    }

    /// <summary>
    /// 从用户名创建测试用户
    /// </summary>
    /// <param name="username">用户名</param>
    /// <returns>测试用户</returns>
    public static TestUser FromUsername(string username)
    {
        return new TestUser
        {
            Username = username,
            Password = GeneratePassword(username)
        };
    }
}

/// <summary>
/// 客户端登录请求模型
/// </summary>
public class ClientLoginRequest
{
    [JsonProperty("userName")]
    public string UserName { get; set; } = string.Empty;

    [JsonProperty("password")]
    public string Password { get; set; } = string.Empty;

    [JsonProperty("tenantId")]
    public string TenantId { get; set; } = string.Empty;

    [JsonProperty("clientType")]
    public string? ClientType { get; set; }
}

/// <summary>
/// 登录响应模型
/// </summary>
public class LoginResponse
{
    [JsonProperty("code")]
    public int Code { get; set; }

    [JsonProperty("message")]
    public string? Message { get; set; }

    [JsonProperty("data")]
    public LoginData? Data { get; set; }
}

/// <summary>
/// 登录数据模型
/// </summary>
public class LoginData
{
    [JsonProperty("token")]
    public string? Token { get; set; }

    [JsonProperty("refreshToken")]
    public string? RefreshToken { get; set; }

    [JsonProperty("user")]
    public UserInfo? User { get; set; }
}

/// <summary>
/// 用户信息模型
/// </summary>
public class UserInfo
{
    [JsonProperty("id")]
    public long Id { get; set; }

    [JsonProperty("userName")]
    public string? UserName { get; set; }

    [JsonProperty("name")]
    public string? Name { get; set; }

    [JsonProperty("email")]
    public string? Email { get; set; }
}

/// <summary>
/// API响应基础模型
/// </summary>
/// <typeparam name="T">数据类型</typeparam>
public class ApiResponse<T>
{
    [JsonProperty("code")]
    public int Code { get; set; }

    [JsonProperty("message")]
    public string? Message { get; set; }

    [JsonProperty("data")]
    public T? Data { get; set; }
}

/// <summary>
/// 考生个人信息模型
/// </summary>
public class ClientProfileDto
{
    [JsonProperty("id")]
    public long Id { get; set; }

    [JsonProperty("name")]
    public string? Name { get; set; }

    [JsonProperty("userName")]
    public string? UserName { get; set; }

    [JsonProperty("email")]
    public string? Email { get; set; }

    [JsonProperty("phoneNumber")]
    public string? PhoneNumber { get; set; }
}

/// <summary>
/// 可用考试模型
/// </summary>
public class ClientExamDto
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("name")]
    public string? Name { get; set; }

    [JsonProperty("description")]
    public string? Description { get; set; }

    [JsonProperty("startTime")]
    public string? StartTime { get; set; }

    [JsonProperty("endTime")]
    public string? EndTime { get; set; }

    [JsonProperty("duration")]
    public int Duration { get; set; }

    [JsonProperty("totalScore")]
    public int TotalScore { get; set; }

    [JsonProperty("status")]
    public string? Status { get; set; }

    [JsonProperty("hasResult")]
    public bool HasResult { get; set; }
}

/// <summary>
/// 可用考试API响应模型
/// </summary>
public class AvailableExamsResponse
{
    [JsonProperty("status")]
    public int Status { get; set; }

    [JsonProperty("msg")]
    public string? Message { get; set; }

    [JsonProperty("data")]
    public List<ClientExamDto>? Data { get; set; }
}

/// <summary>
/// 考试基本信息模型
/// </summary>
public class ClientExamBasicInfoDto
{
    [JsonProperty("id")]
    public long Id { get; set; }

    [JsonProperty("title")]
    public string? Title { get; set; }

    [JsonProperty("description")]
    public string? Description { get; set; }

    [JsonProperty("duration")]
    public int Duration { get; set; }

    [JsonProperty("questionCount")]
    public int QuestionCount { get; set; }

    [JsonProperty("recordId")]
    public long? RecordId { get; set; }

    [JsonProperty("questions")]
    public List<QuestionInfo>? Questions { get; set; }
}

/// <summary>
/// 题目信息模型
/// </summary>
public class QuestionInfo
{
    [JsonProperty("questionId")]
    public int QuestionId { get; set; }

    [JsonProperty("content")]
    public string? Content { get; set; }

    [JsonProperty("type")]
    public int Type { get; set; }

    [JsonProperty("options")]
    public List<OptionInfo>? Options { get; set; }

    [JsonProperty("answer")]
    public string? Answer { get; set; }
}

/// <summary>
/// 选项信息模型
/// </summary>
public class OptionInfo
{
    [JsonProperty("label")]
    public string? Label { get; set; }

    [JsonProperty("value")]
    public string? Value { get; set; }
}

/// <summary>
/// 考试轻量信息模型（用于倒计时页面）
/// </summary>
public class ClientExamLightInfoDto
{
    [JsonProperty("id")]
    public long Id { get; set; }

    [JsonProperty("title")]
    public string? Title { get; set; }

    [JsonProperty("duration")]
    public int Duration { get; set; }

    [JsonProperty("startTime")]
    public DateTime StartTime { get; set; }

    [JsonProperty("endTime")]
    public DateTime EndTime { get; set; }

    [JsonProperty("canStart")]
    public bool CanStart { get; set; }

    [JsonProperty("remainingTime")]
    public int? RemainingTime { get; set; }
}

/// <summary>
/// 开始考试响应模型
/// </summary>
public class StartExamResponse
{
    [JsonProperty("id")]
    public long Id { get; set; }

    [JsonProperty("examSettingId")]
    public long ExamSettingId { get; set; }

    [JsonProperty("recordId")]
    public long RecordId { get; set; }
}

/// <summary>
/// 负载测试场景配置
/// </summary>
public class LoadTestScenario
{
    /// <summary>
    /// 持续时间
    /// </summary>
    public string Duration { get; set; } = "00:01:00";

    /// <summary>
    /// 请求速率（每秒请求数）
    /// </summary>
    public int Rate { get; set; } = 1;
}

/// <summary>
/// 负载测试场景配置集合
/// </summary>
public class LoadTestScenarios
{
    /// <summary>
    /// 预热场景
    /// </summary>
    public LoadTestScenario WarmUp { get; set; } = new();

    /// <summary>
    /// 正常负载场景
    /// </summary>
    public LoadTestScenario NormalLoad { get; set; } = new();

    /// <summary>
    /// 峰值负载场景
    /// </summary>
    public LoadTestScenario PeakLoad { get; set; } = new();

    /// <summary>
    /// 压力测试场景
    /// </summary>
    public LoadTestScenario StressTest { get; set; } = new();
}
