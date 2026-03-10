using CodeSpirit.Core.DependencyInjection;

namespace CodeSpirit.ExamApi.Services.Interfaces;

/// <summary>
/// 考试数据可见性服务接口
/// </summary>
/// <remarks>
/// 用于判断当前用户是否可查看全部考试数据，以及获取当前用户ID用于数据过滤。
/// </remarks>
public interface IExamDataScopeService : IScopedDependency
{
    /// <summary>
    /// 检查当前用户是否可查看全部考试数据
    /// </summary>
    /// <returns>true 表示可查看全部（Admin 角色或拥有 exam_view_all 权限），false 表示仅能查看自己创建的数据</returns>
    Task<bool> CanViewAllExamDataAsync();

    /// <summary>
    /// 获取当前用户ID
    /// </summary>
    /// <returns>已认证用户返回用户ID，未认证返回 null</returns>
    long? GetCurrentUserId();
}
