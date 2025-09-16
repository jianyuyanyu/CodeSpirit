using CodeSpirit.ApprovalApi.Dtos;
using CodeSpirit.ApprovalApi.Services;

namespace CodeSpirit.ApprovalApi.Controllers;

/// <summary>
/// 审批实例管理控制器
/// </summary>
[DisplayName("审批实例管理")]
[Navigation(Icon = "fa-solid fa-clipboard-check")]
public class ApprovalInstancesController : ApiControllerBase
{
    private readonly IApprovalInstanceService _approvalInstanceService;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="approvalInstanceService">审批实例服务</param>
    public ApprovalInstancesController(IApprovalInstanceService approvalInstanceService)
    {
        _approvalInstanceService = approvalInstanceService;
    }

    /// <summary>
    /// 获取审批实例列表
    /// </summary>
    /// <param name="query">查询条件</param>
    /// <returns>审批实例分页列表</returns>
    [HttpGet]
    [DisplayName("获取审批实例列表")]
    public async Task<ActionResult<ApiResponse<PageList<ApprovalInstanceDto>>>> GetApprovalInstances([FromQuery] ApprovalInstanceQueryDto query)
    {
        var result = await _approvalInstanceService.GetPagedListAsync(query);
        return SuccessResponse(result);
    }

    /// <summary>
    /// 获取审批实例详情
    /// </summary>
    /// <param name="id">审批实例ID</param>
    /// <returns>审批实例详情</returns>
    [HttpGet("{id}")]
    [DisplayName("获取审批实例详情")]
    public async Task<ActionResult<ApiResponse<ApprovalInstanceDetailDto>>> GetApprovalInstance(long id)
    {
        var result = await _approvalInstanceService.GetDetailAsync(id);
        return SuccessResponse(result);
    }

    /// <summary>
    /// 发起审批
    /// </summary>
    /// <param name="dto">发起审批DTO</param>
    /// <returns>审批实例</returns>
    [HttpPost]
    [DisplayName("发起审批")]
    public async Task<ActionResult<ApiResponse<ApprovalInstanceDto>>> StartApproval(StartApprovalDto dto)
    {
        var result = await _approvalInstanceService.StartApprovalAsync(dto);
        var resultDto = await _approvalInstanceService.GetAsync(result.Id);
        return SuccessResponse(resultDto);
    }

    /// <summary>
    /// 撤回审批
    /// </summary>
    /// <param name="id">审批实例ID</param>
    /// <param name="dto">撤回参数</param>
    /// <returns>撤回结果</returns>
    [HttpPut("{id}/withdraw")]
    [Operation("撤回", "form", null, "确定要撤回此审批吗？", "status == 'Pending' || status == 'InProgress'")]
    [DisplayName("撤回审批")]
    public async Task<ActionResult<ApiResponse>> WithdrawApproval(long id, WithdrawApprovalDto dto)
    {
        await _approvalInstanceService.WithdrawAsync(id, dto.Reason);
        return SuccessResponse("审批已撤回");
    }

    /// <summary>
    /// 获取我发起的审批
    /// </summary>
    /// <param name="pageIndex">页码</param>
    /// <param name="pageSize">页大小</param>
    /// <returns>我发起的审批分页列表</returns>
    [HttpGet("my-applications")]
    [DisplayName("获取我发起的审批")]
    public async Task<ActionResult<ApiResponse<PageList<ApprovalInstanceDto>>>> GetMyApplications(int pageIndex = 1, int pageSize = 20)
    {
        var result = await _approvalInstanceService.GetMyApplicationsAsync("current-user", pageIndex, pageSize);
        return SuccessResponse(result);
    }
}

/// <summary>
/// 撤回审批DTO
/// </summary>
public class WithdrawApprovalDto
{
    /// <summary>
    /// 撤回理由
    /// </summary>
    [Required]
    [StringLength(500)]
    [DisplayName("撤回理由")]
    public string Reason { get; set; } = string.Empty;
}
