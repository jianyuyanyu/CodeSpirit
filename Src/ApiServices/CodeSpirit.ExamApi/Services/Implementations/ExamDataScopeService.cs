using CodeSpirit.Core;
using CodeSpirit.Core.Authorization;
using CodeSpirit.Core.DependencyInjection;
using CodeSpirit.ExamApi.Services.Interfaces;

namespace CodeSpirit.ExamApi.Services.Implementations;

/// <summary>
/// 考试数据可见性服务实现
/// </summary>
/// <remarks>
/// 判断逻辑：Admin 角色或拥有 exam_view_all 权限的用户可查看全部数据。
/// </remarks>
public class ExamDataScopeService : IExamDataScopeService, IScopedDependency
{
    private const string ViewAllPermission = "exam_view_all";

    private readonly ICurrentUser _currentUser;
    private readonly IHasPermissionService _hasPermissionService;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="currentUser">当前用户</param>
    /// <param name="hasPermissionService">权限检查服务</param>
    public ExamDataScopeService(ICurrentUser currentUser, IHasPermissionService hasPermissionService)
    {
        _currentUser = currentUser;
        _hasPermissionService = hasPermissionService;
    }

    /// <inheritdoc />
    public Task<bool> CanViewAllExamDataAsync()
    {
        if (!_currentUser.IsAuthenticated)
        {
            return Task.FromResult(false);
        }

        // Admin 角色可查看全部
        if (_currentUser.IsInRole("Admin"))
        {
            return Task.FromResult(true);
        }

        // 拥有 exam_view_all 权限可查看全部
        var canViewAll = _hasPermissionService.HasPermission(ViewAllPermission);
        return Task.FromResult(canViewAll);
    }

    /// <inheritdoc />
    public long? GetCurrentUserId()
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.Id.HasValue || _currentUser.Id.Value <= 0)
        {
            return null;
        }

        return _currentUser.Id.Value;
    }
}
