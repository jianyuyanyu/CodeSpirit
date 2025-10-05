using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.Audit.Models;

/// <summary>
/// HTTP请求方法枚举
/// </summary>
public enum HttpRequestMethod
{
    /// <summary>
    /// GET请求
    /// </summary>
    [Display(Name = "GET")]
    GET = 1,
    
    /// <summary>
    /// POST请求
    /// </summary>
    [Display(Name = "POST")]
    POST = 2,
    
    /// <summary>
    /// PUT请求
    /// </summary>
    [Display(Name = "PUT")]
    PUT = 3,
    
    /// <summary>
    /// DELETE请求
    /// </summary>
    [Display(Name = "DELETE")]
    DELETE = 4,
    
    /// <summary>
    /// PATCH请求
    /// </summary>
    [Display(Name = "PATCH")]
    PATCH = 5,
    
    /// <summary>
    /// HEAD请求
    /// </summary>
    [Display(Name = "HEAD")]
    HEAD = 6,
    
    /// <summary>
    /// OPTIONS请求
    /// </summary>
    [Display(Name = "OPTIONS")]
    OPTIONS = 7
}

/// <summary>
/// 审计操作类型枚举（扩展版本）
/// </summary>
public enum AuditOperationTypeEnum
{
    /// <summary>
    /// 常规操作
    /// </summary>
    [Display(Name = "常规操作")]
    Action = 1,
    
    /// <summary>
    /// 查询
    /// </summary>
    [Display(Name = "查询")]
    Query = 2,
    
    /// <summary>
    /// 创建
    /// </summary>
    [Display(Name = "创建")]
    Create = 3,
    
    /// <summary>
    /// 更新
    /// </summary>
    [Display(Name = "更新")]
    Update = 4,
    
    /// <summary>
    /// 删除
    /// </summary>
    [Display(Name = "删除")]
    Delete = 5,
    
    /// <summary>
    /// 登录
    /// </summary>
    [Display(Name = "登录")]
    Login = 6,
    
    /// <summary>
    /// 登出
    /// </summary>
    [Display(Name = "登出")]
    Logout = 7,
    
    /// <summary>
    /// 导入
    /// </summary>
    [Display(Name = "导入")]
    Import = 8,
    
    /// <summary>
    /// 导出
    /// </summary>
    [Display(Name = "导出")]
    Export = 9,
    
    /// <summary>
    /// 批量操作
    /// </summary>
    [Display(Name = "批量操作")]
    Batch = 10,
    
    /// <summary>
    /// 文件上传
    /// </summary>
    [Display(Name = "文件上传")]
    Upload = 11,
    
    /// <summary>
    /// 文件下载
    /// </summary>
    [Display(Name = "文件下载")]
    Download = 12,
    
    /// <summary>
    /// 授权
    /// </summary>
    [Display(Name = "授权")]
    Authorize = 13,
    
    /// <summary>
    /// 系统设置
    /// </summary>
    [Display(Name = "系统设置")]
    Setting = 14
}

/// <summary>
/// 操作交互类型枚举（基于OperationActionType）
/// </summary>
public enum OperationInteractionType
{
    /// <summary>
    /// AJAX请求
    /// </summary>
    [Display(Name = "AJAX请求")]
    Ajax = 1,

    /// <summary>
    /// 表单操作
    /// </summary>
    [Display(Name = "表单操作")]
    Form = 2,

    /// <summary>
    /// 链接跳转
    /// </summary>
    [Display(Name = "链接跳转")]
    Link = 3,

    /// <summary>
    /// 服务调用
    /// </summary>
    [Display(Name = "服务调用")]
    Service = 4,

    /// <summary>
    /// AI表单操作
    /// </summary>
    [Display(Name = "AI表单操作")]
    AiForm = 5
}

/// <summary>
/// 常用HTTP状态码枚举
/// </summary>
public enum CommonHttpStatusCode
{
    /// <summary>
    /// 成功
    /// </summary>
    [Display(Name = "200 - 成功")]
    OK = 200,
    
    /// <summary>
    /// 已创建
    /// </summary>
    [Display(Name = "201 - 已创建")]
    Created = 201,
    
    /// <summary>
    /// 无内容
    /// </summary>
    [Display(Name = "204 - 无内容")]
    NoContent = 204,
    
    /// <summary>
    /// 永久重定向
    /// </summary>
    [Display(Name = "301 - 永久重定向")]
    MovedPermanently = 301,
    
    /// <summary>
    /// 临时重定向
    /// </summary>
    [Display(Name = "302 - 临时重定向")]
    Found = 302,
    
    /// <summary>
    /// 未修改
    /// </summary>
    [Display(Name = "304 - 未修改")]
    NotModified = 304,
    
    /// <summary>
    /// 请求错误
    /// </summary>
    [Display(Name = "400 - 请求错误")]
    BadRequest = 400,
    
    /// <summary>
    /// 未授权
    /// </summary>
    [Display(Name = "401 - 未授权")]
    Unauthorized = 401,
    
    /// <summary>
    /// 禁止访问
    /// </summary>
    [Display(Name = "403 - 禁止访问")]
    Forbidden = 403,
    
    /// <summary>
    /// 未找到
    /// </summary>
    [Display(Name = "404 - 未找到")]
    NotFound = 404,
    
    /// <summary>
    /// 方法不允许
    /// </summary>
    [Display(Name = "405 - 方法不允许")]
    MethodNotAllowed = 405,
    
    /// <summary>
    /// 冲突
    /// </summary>
    [Display(Name = "409 - 冲突")]
    Conflict = 409,
    
    /// <summary>
    /// 参数错误
    /// </summary>
    [Display(Name = "422 - 参数错误")]
    UnprocessableEntity = 422,
    
    /// <summary>
    /// 请求过多
    /// </summary>
    [Display(Name = "429 - 请求过多")]
    TooManyRequests = 429,
    
    /// <summary>
    /// 服务器错误
    /// </summary>
    [Display(Name = "500 - 服务器错误")]
    InternalServerError = 500,
    
    /// <summary>
    /// 未实现
    /// </summary>
    [Display(Name = "501 - 未实现")]
    NotImplemented = 501,
    
    /// <summary>
    /// 网关错误
    /// </summary>
    [Display(Name = "502 - 网关错误")]
    BadGateway = 502,
    
    /// <summary>
    /// 服务不可用
    /// </summary>
    [Display(Name = "503 - 服务不可用")]
    ServiceUnavailable = 503,
    
    /// <summary>
    /// 网关超时
    /// </summary>
    [Display(Name = "504 - 网关超时")]
    GatewayTimeout = 504
}
