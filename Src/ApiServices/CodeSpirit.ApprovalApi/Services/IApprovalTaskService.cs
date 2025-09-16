using CodeSpirit.ApprovalApi.Dtos;
using CodeSpirit.ApprovalApi.Models;

namespace CodeSpirit.ApprovalApi.Services;

/// <summary>
/// 审批任务服务接口
/// </summary>
public interface IApprovalTaskService : IBaseCRUDService<ApprovalTask, ApprovalTaskDto, long, ProcessApprovalTaskDto, ProcessApprovalTaskDto>
{
    /// <summary>
    /// 处理审批任务
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <param name="dto">处理审批任务DTO</param>
    /// <returns>处理结果</returns>
    Task<bool> ProcessTaskAsync(long taskId, ProcessApprovalTaskDto dto);

    /// <summary>
    /// 获取我的待办任务
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="pageIndex">页码</param>
    /// <param name="pageSize">页大小</param>
    /// <returns>待办任务分页列表</returns>
    Task<PageList<ApprovalTaskDto>> GetMyPendingTasksAsync(string userId, int pageIndex = 1, int pageSize = 20);

    /// <summary>
    /// 获取我的已办任务
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="pageIndex">页码</param>
    /// <param name="pageSize">页大小</param>
    /// <returns>已办任务分页列表</returns>
    Task<PageList<ApprovalTaskDto>> GetMyCompletedTasksAsync(string userId, int pageIndex = 1, int pageSize = 20);

    /// <summary>
    /// 加签
    /// </summary>
    /// <param name="taskId">当前任务ID</param>
    /// <param name="approverId">加签人ID</param>
    /// <param name="comment">加签理由</param>
    /// <returns>加签结果</returns>
    Task<ApprovalTask> AddSignAsync(long taskId, string approverId, string comment = "");

    /// <summary>
    /// 转交任务
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <param name="toUserId">接收人ID</param>
    /// <param name="comment">转交理由</param>
    /// <returns>转交结果</returns>
    Task<ApprovalTask> TransferTaskAsync(long taskId, string toUserId, string comment = "");
}
