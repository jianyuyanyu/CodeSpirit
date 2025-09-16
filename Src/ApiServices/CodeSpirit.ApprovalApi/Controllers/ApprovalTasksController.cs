using CodeSpirit.ApprovalApi.Dtos;
using CodeSpirit.ApprovalApi.Services;

namespace CodeSpirit.ApprovalApi.Controllers;

/// <summary>
/// 审批任务管理控制器
/// </summary>
[DisplayName("审批任务管理")]
[Navigation(Icon = "fa-solid fa-tasks")]
public class ApprovalTasksController : ApiControllerBase
{
    private readonly IApprovalTaskService _approvalTaskService;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="approvalTaskService">审批任务服务</param>
    public ApprovalTasksController(IApprovalTaskService approvalTaskService)
    {
        _approvalTaskService = approvalTaskService;
    }

    /// <summary>
    /// 获取审批任务列表
    /// </summary>
    /// <param name="query">查询条件</param>
    /// <returns>审批任务分页列表</returns>
    [HttpGet]
    [DisplayName("获取审批任务列表")]
    public async Task<ActionResult<ApiResponse<PageList<ApprovalTaskDto>>>> GetApprovalTasks([FromQuery] ApprovalTaskQueryDto query)
    {
        var result = await _approvalTaskService.GetPagedListAsync(query);
        return SuccessResponse(result);
    }

    /// <summary>
    /// 获取审批任务详情
    /// </summary>
    /// <param name="id">审批任务ID</param>
    /// <returns>审批任务详情</returns>
    [HttpGet("{id}")]
    [DisplayName("获取审批任务详情")]
    public async Task<ActionResult<ApiResponse<ApprovalTaskDto>>> GetApprovalTask(long id)
    {
        var result = await _approvalTaskService.GetAsync(id);
        return SuccessResponse(result);
    }

    /// <summary>
    /// 处理审批任务
    /// </summary>
    /// <param name="id">审批任务ID</param>
    /// <param name="dto">处理审批任务DTO</param>
    /// <returns>处理结果</returns>
    [HttpPut("{id}/process")]
    [Operation("处理", "form")]
    [DisplayName("处理审批任务")]
    public async Task<ActionResult<ApiResponse>> ProcessApprovalTask(long id, ProcessApprovalTaskDto dto)
    {
        await _approvalTaskService.ProcessTaskAsync(id, dto);
        return SuccessResponse("审批任务已处理");
    }

    /// <summary>
    /// 获取我的待办任务
    /// </summary>
    /// <param name="pageIndex">页码</param>
    /// <param name="pageSize">页大小</param>
    /// <returns>我的待办任务分页列表</returns>
    [HttpGet("my-pending")]
    [DisplayName("获取我的待办任务")]
    public async Task<ActionResult<ApiResponse<PageList<ApprovalTaskDto>>>> GetMyPendingTasks(int pageIndex = 1, int pageSize = 20)
    {
        var result = await _approvalTaskService.GetMyPendingTasksAsync("current-user", pageIndex, pageSize);
        return SuccessResponse(result);
    }

    /// <summary>
    /// 获取我的已办任务
    /// </summary>
    /// <param name="pageIndex">页码</param>
    /// <param name="pageSize">页大小</param>
    /// <returns>我的已办任务分页列表</returns>
    [HttpGet("my-completed")]
    [DisplayName("获取我的已办任务")]
    public async Task<ActionResult<ApiResponse<PageList<ApprovalTaskDto>>>> GetMyCompletedTasks(int pageIndex = 1, int pageSize = 20)
    {
        var result = await _approvalTaskService.GetMyCompletedTasksAsync("current-user", pageIndex, pageSize);
        return SuccessResponse(result);
    }

    /// <summary>
    /// 加签
    /// </summary>
    /// <param name="id">审批任务ID</param>
    /// <param name="dto">加签参数</param>
    /// <returns>加签结果</returns>
    [HttpPost("{id}/add-sign")]
    [Operation("加签", "form")]
    [DisplayName("加签")]
    public async Task<ActionResult<ApiResponse>> AddSign(long id, AddSignDto dto)
    {
        await _approvalTaskService.AddSignAsync(id, dto.ApproverId, dto.Comment);
        return SuccessResponse("加签成功");
    }

    /// <summary>
    /// 转交任务
    /// </summary>
    /// <param name="id">审批任务ID</param>
    /// <param name="dto">转交参数</param>
    /// <returns>转交结果</returns>
    [HttpPost("{id}/transfer")]
    [Operation("转交", "form")]
    [DisplayName("转交任务")]
    public async Task<ActionResult<ApiResponse>> TransferTask(long id, TransferTaskDto dto)
    {
        await _approvalTaskService.TransferTaskAsync(id, dto.ToUserId, dto.Comment);
        return SuccessResponse("任务转交成功");
    }
}

/// <summary>
/// 加签DTO
/// </summary>
public class AddSignDto
{
    /// <summary>
    /// 加签人ID
    /// </summary>
    [Required]
    [DisplayName("加签人ID")]
    public string ApproverId { get; set; } = string.Empty;

    /// <summary>
    /// 加签理由
    /// </summary>
    [StringLength(500)]
    [DisplayName("加签理由")]
    public string Comment { get; set; } = string.Empty;
}

/// <summary>
/// 转交任务DTO
/// </summary>
public class TransferTaskDto
{
    /// <summary>
    /// 接收人ID
    /// </summary>
    [Required]
    [DisplayName("接收人ID")]
    public string ToUserId { get; set; } = string.Empty;

    /// <summary>
    /// 转交理由
    /// </summary>
    [StringLength(500)]
    [DisplayName("转交理由")]
    public string Comment { get; set; } = string.Empty;
}
