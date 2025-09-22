using CodeSpirit.ApprovalApi.Dtos.ApprovalInstance;
using CodeSpirit.ApprovalApi.Models;

namespace CodeSpirit.ApprovalApi.Services;

/// <summary>
/// 审批实例服务接口
/// </summary>
public interface IApprovalInstanceService : IScopedDependency
{
    /// <summary>
    /// 发起审批
    /// </summary>
    /// <param name="dto">发起审批DTO</param>
    /// <returns>审批实例</returns>
    Task<ApprovalInstance> StartApprovalAsync(StartApprovalDto dto);

    /// <summary>
    /// 获取审批详情
    /// </summary>
    /// <param name="id">实例ID</param>
    /// <returns>审批详情</returns>
    Task<ApprovalInstanceDetailDto?> GetDetailAsync(long id);

    /// <summary>
    /// 撤回审批
    /// </summary>
    /// <param name="id">实例ID</param>
    /// <param name="reason">撤回理由</param>
    /// <returns>撤回结果</returns>
    Task<bool> WithdrawAsync(long id, string reason);

    /// <summary>
    /// 根据业务实体获取审批实例
    /// </summary>
    /// <param name="entityType">业务实体类型</param>
    /// <param name="entityId">业务实体ID</param>
    /// <returns>审批实例列表</returns>
    Task<List<ApprovalInstance>> GetByEntityAsync(string entityType, string entityId);

    /// <summary>
    /// 获取分页列表
    /// </summary>
    /// <param name="query">查询条件</param>
    /// <returns>分页列表</returns>
    Task<PageList<ApprovalInstanceDto>> GetPagedListAsync(ApprovalInstanceQueryDto query);

    /// <summary>
    /// 获取审批实例
    /// </summary>
    /// <param name="id">实例ID</param>
    /// <returns>审批实例</returns>
    Task<ApprovalInstanceDto?> GetAsync(long id);

    /// <summary>
    /// 获取我发起的审批
    /// </summary>
    /// <param name="applicantId">申请人ID</param>
    /// <param name="pageIndex">页码</param>
    /// <param name="pageSize">页大小</param>
    /// <returns>审批实例分页列表</returns>
    Task<PageList<ApprovalInstanceDto>> GetMyApplicationsAsync(string applicantId, int pageIndex = 1, int pageSize = 20);
}
