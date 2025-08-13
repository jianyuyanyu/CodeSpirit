using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.MultiTenant.Models;
using CodeSpirit.Shared.EventBus.Handlers;
using Microsoft.Extensions.Logging;

namespace CodeSpirit.IdentityApi.EventHandlers;

/// <summary>
/// 身份认证API的实体文件引用事件处理器
/// 负责配置用户和租户的文件引用处理规则
/// </summary>
public class EntityFileReferenceEventHandler : EntityFileReferenceHandlerBase<EntityFileReferenceEventHandler>
{
    /// <summary>
    /// 源服务名称
    /// </summary>
    protected override string SourceService => "IdentityApi";
    
    /// <summary>
    /// 实体文件字段配置
    /// 定义各个实体的文件字段映射规则
    /// </summary>
    protected override Dictionary<Type, EntityFileConfig> EntityConfigs { get; } = new()
    {
        [typeof(ApplicationUser)] = new("用户", 
            user => ((ApplicationUser)user).AvatarUrl ?? string.Empty, 
            user => ((ApplicationUser)user).Id.ToString(), 
            user => ((ApplicationUser)user).Name, 
            "Avatar", "用户头像"),
        [typeof(TenantInfo)] = new("租户", 
            tenant => ((TenantInfo)tenant).LogoUrl ?? string.Empty, 
            tenant => ((TenantInfo)tenant).TenantId, 
            tenant => ((TenantInfo)tenant).Name, 
            "Logo", "租户Logo")
    };

    /// <summary>
    /// 构造函数
    /// </summary>
    public EntityFileReferenceEventHandler(IServiceProvider serviceProvider, ILogger<EntityFileReferenceEventHandler> logger)
        : base(serviceProvider, logger)
    {
    }
}