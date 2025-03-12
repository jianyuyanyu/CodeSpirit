using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Reflection;
using System.ComponentModel;
using CodeSpirit.Core.Attributes;

namespace CodeSpirit.Audit.Attributes;

/// <summary>
/// 审计特性，用于标记需要审计的控制器或方法
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class AuditAttribute : Attribute, IActionFilter
{
    /// <summary>
    /// 操作描述
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// 操作类型
    /// </summary>
    public AuditOperationType OperationType { get; set; } = AuditOperationType.Action;
    
    /// <summary>
    /// 是否记录请求参数
    /// </summary>
    public bool LogRequestParams { get; set; } = true;
    
    /// <summary>
    /// 是否记录响应数据
    /// </summary>
    public bool LogResponseData { get; set; } = false;
    
    /// <summary>
    /// 实体名称，默认为空，将自动从路由或参数中推断
    /// </summary>
    public string EntityName { get; set; } = string.Empty;
    
    /// <summary>
    /// 实体ID参数名，默认为id
    /// </summary>
    public string EntityIdParamName { get; set; } = "id";
    
    /// <summary>
    /// 构造函数
    /// </summary>
    public AuditAttribute()
    {
    }
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="description">操作描述</param>
    public AuditAttribute(string description)
    {
        Description = description;
    }
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="description">操作描述</param>
    /// <param name="operationType">操作类型</param>
    public AuditAttribute(string description, AuditOperationType operationType)
    {
        Description = description;
        OperationType = operationType;
    }
    
    /// <summary>
    /// 动作执行前
    /// </summary>
    public void OnActionExecuting(ActionExecutingContext context)
    {
        // 在审计中间件中实现
    }
    
    /// <summary>
    /// 动作执行后
    /// </summary>
    public void OnActionExecuted(ActionExecutedContext context)
    {
        // 在审计中间件中实现
    }
    
    /// <summary>
    /// 从控制器提取业务信息
    /// </summary>
    public static Dictionary<string, string> ExtractBusinessInfoFromController(Type controllerType, string actionName)
    {
        var result = new Dictionary<string, string>();
        
        // 获取控制器上的DisplayName特性
        var controllerDisplayName = controllerType.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName;
        if (!string.IsNullOrEmpty(controllerDisplayName))
        {
            result.Add("ControllerDisplayName", controllerDisplayName);
        }
        
        // 获取控制器上的Navigation特性
        var navigationAttr = controllerType.GetCustomAttribute<NavigationAttribute>();
        if (navigationAttr != null)
        {
            result.Add("NavigationIcon", navigationAttr.Icon);
        }
        
        // 获取方法
        var methodInfo = controllerType.GetMethod(actionName);
        if (methodInfo != null)
        {
            // 获取方法上的Operation特性
            var operationAttr = methodInfo.GetCustomAttribute<OperationAttribute>();
            if (operationAttr != null)
            {
                // 使用OperationAttributeHelper获取属性
                var properties = OperationAttributeHelper.ExtractOperationInfo(operationAttr);
                foreach (var prop in properties)
                {
                    result.Add($"Operation{prop.Key}", prop.Value);
                }
            }
        }
        
        return result;
    }
}

/// <summary>
/// 审计操作类型
/// </summary>
public enum AuditOperationType
{
    /// <summary>
    /// 常规操作
    /// </summary>
    Action,
    
    /// <summary>
    /// 查询
    /// </summary>
    Query,
    
    /// <summary>
    /// 创建
    /// </summary>
    Create,
    
    /// <summary>
    /// 更新
    /// </summary>
    Update,
    
    /// <summary>
    /// 删除
    /// </summary>
    Delete,
    
    /// <summary>
    /// 登录
    /// </summary>
    Login,
    
    /// <summary>
    /// 登出
    /// </summary>
    Logout,
    
    /// <summary>
    /// 导入
    /// </summary>
    Import,
    
    /// <summary>
    /// 导出
    /// </summary>
    Export,
    
    /// <summary>
    /// 批量操作
    /// </summary>
    Batch,
    
    /// <summary>
    /// 文件上传
    /// </summary>
    Upload,
    
    /// <summary>
    /// 文件下载
    /// </summary>
    Download,
    
    /// <summary>
    /// 授权
    /// </summary>
    Authorize,
    
    /// <summary>
    /// 系统设置
    /// </summary>
    Setting
} 