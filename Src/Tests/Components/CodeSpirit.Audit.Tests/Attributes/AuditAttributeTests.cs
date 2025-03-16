using CodeSpirit.Audit.Attributes;
using CodeSpirit.Audit.Tests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Moq;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace CodeSpirit.Audit.Tests.Attributes;

/// <summary>
/// 审计特性测试类
/// </summary>
public class AuditAttributeTests
{
    private readonly ITestOutputHelper _output;

    public AuditAttributeTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Constructor_Default_SetsDefaultValues()
    {
        // 执行
        var attribute = new AuditAttribute();
        
        // 断言
        Assert.NotNull(attribute);
        Assert.Equal("", attribute.Description);
        Assert.Equal(AuditOperationType.Action, attribute.OperationType);
    }
    
    [Fact]
    public void Constructor_WithDescription_SetsDescription()
    {
        // 执行
        var attribute = new AuditAttribute("测试操作");
        
        // 断言
        Assert.Equal("测试操作", attribute.Description);
        Assert.Equal(AuditOperationType.Action, attribute.OperationType);
    }
    
    [Fact]
    public void Constructor_WithDescriptionAndOperationType_SetsBothProperties()
    {
        // 执行
        var attribute = new AuditAttribute("测试操作", AuditOperationType.Create);
        
        // 断言
        Assert.Equal("测试操作", attribute.Description);
        Assert.Equal(AuditOperationType.Create, attribute.OperationType);
    }
    
    [Fact]
    public void OnActionExecuting_ShouldNotModifyContext()
    {
        // 安排
        var attribute = new AuditAttribute("测试操作");
        
        // 创建有效的 ActionContext
        var actionContext = CreateActionContext();
        
        var context = new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object?>(),
            new object());
        
        // 执行
        attribute.OnActionExecuting(context);
        
        // 断言 - 只需确保方法执行不会抛出异常
        Assert.NotNull(context);
    }
    
    [Fact]
    public void OnActionExecuted_ShouldNotModifyContext()
    {
        // 安排
        var attribute = new AuditAttribute("测试操作");
        
        // 创建有效的 ActionContext
        var actionContext = CreateActionContext();
        
        var context = new ActionExecutedContext(
            actionContext,
            new List<IFilterMetadata>(),
            new object());
        
        // 执行
        attribute.OnActionExecuted(context);
        
        // 断言 - 只需确保方法执行不会抛出异常
        Assert.NotNull(context);
    }
    
    [Fact]
    public void AttributeUsage_AllowsClassAndMethod()
    {
        // 安排 & 执行
        var attributeType = typeof(AuditAttribute);
        var attributeUsage = attributeType.GetCustomAttribute<AttributeUsageAttribute>();
        
        // 断言
        Assert.NotNull(attributeUsage);
        Assert.True((attributeUsage.ValidOn & AttributeTargets.Class) != 0);
        Assert.True((attributeUsage.ValidOn & AttributeTargets.Method) != 0);
    }
    
    [Fact]
    public void AuditAttribute_ImplementsIActionFilter()
    {
        // 安排 & 执行
        var attributeType = typeof(AuditAttribute);
        
        // 断言
        Assert.True(typeof(IActionFilter).IsAssignableFrom(attributeType));
    }
    
    [Fact]
    public void Properties_AreConfigurable()
    {
        // 安排
        var attribute = new AuditAttribute("测试操作")
        {
            LogRequestParams = false,
            LogResponseData = true,
            EntityName = "测试实体",
            EntityIdParamName = "testId"
        };
        
        // 断言
        Assert.False(attribute.LogRequestParams);
        Assert.True(attribute.LogResponseData);
        Assert.Equal("测试实体", attribute.EntityName);
        Assert.Equal("testId", attribute.EntityIdParamName);
    }

    /// <summary>
    /// 创建测试用ActionContext
    /// </summary>
    private ActionContext CreateActionContext()
    {
        var httpContext = new DefaultHttpContext();
        var routeData = new RouteData();
        var actionDescriptor = new ActionDescriptor();
        return new ActionContext(httpContext, routeData, actionDescriptor);
    }
} 