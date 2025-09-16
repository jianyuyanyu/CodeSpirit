using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.ApprovalApi.Models;

/// <summary>
/// 工作流节点类型
/// </summary>
public enum WorkflowNodeType
{
    /// <summary>
    /// 开始节点
    /// </summary>
    [Display(Name = "开始节点")]
    Start = 1,
    
    /// <summary>
    /// 审批节点
    /// </summary>
    [Display(Name = "审批节点")]
    Approval = 2,
    
    /// <summary>
    /// 条件节点
    /// </summary>
    [Display(Name = "条件节点")]
    Condition = 3,
    
    /// <summary>
    /// 并行网关
    /// </summary>
    [Display(Name = "并行网关")]
    ParallelGateway = 4,
    
    /// <summary>
    /// 排他网关
    /// </summary>
    [Display(Name = "排他网关")]
    ExclusiveGateway = 5,
    
    /// <summary>
    /// 抄送节点
    /// </summary>
    [Display(Name = "抄送节点")]
    CarbonCopy = 6,
    
    /// <summary>
    /// 结束节点
    /// </summary>
    [Display(Name = "结束节点")]
    End = 7
}

/// <summary>
/// 审批模式
/// </summary>
public enum ApprovalMode
{
    /// <summary>
    /// 串行审批（依次审批）
    /// </summary>
    [Display(Name = "串行审批")]
    Sequential = 1,
    
    /// <summary>
    /// 并行审批（同时审批）
    /// </summary>
    [Display(Name = "并行审批")]
    Parallel = 2,
    
    /// <summary>
    /// 会签（所有人都需要审批）
    /// </summary>
    [Display(Name = "会签")]
    CounterSign = 3,
    
    /// <summary>
    /// 或签（任意一人审批即可）
    /// </summary>
    [Display(Name = "或签")]
    OrSign = 4
}

/// <summary>
/// 审批人类型
/// </summary>
public enum ApproverType
{
    /// <summary>
    /// 指定用户
    /// </summary>
    [Display(Name = "指定用户")]
    User = 1,
    
    /// <summary>
    /// 角色
    /// </summary>
    [Display(Name = "角色")]
    Role = 2,
    
    /// <summary>
    /// 部门
    /// </summary>
    [Display(Name = "部门")]
    Department = 3,
    
    /// <summary>
    /// 发起人
    /// </summary>
    [Display(Name = "发起人")]
    Initiator = 4,
    
    /// <summary>
    /// 发起人上级
    /// </summary>
    [Display(Name = "发起人上级")]
    InitiatorSuperior = 5,
    
    /// <summary>
    /// 动态表达式
    /// </summary>
    [Display(Name = "动态表达式")]
    Expression = 6
}

/// <summary>
/// 审批状态
/// </summary>
public enum ApprovalStatus
{
    /// <summary>
    /// 待审批
    /// </summary>
    [Display(Name = "待审批")]
    Pending = 1,
    
    /// <summary>
    /// 审批中
    /// </summary>
    [Display(Name = "审批中")]
    InProgress = 2,
    
    /// <summary>
    /// 已通过
    /// </summary>
    [Display(Name = "已通过")]
    Approved = 3,
    
    /// <summary>
    /// 已拒绝
    /// </summary>
    [Display(Name = "已拒绝")]
    Rejected = 4,
    
    /// <summary>
    /// 已撤回
    /// </summary>
    [Display(Name = "已撤回")]
    Withdrawn = 5,
    
    /// <summary>
    /// 已取消
    /// </summary>
    [Display(Name = "已取消")]
    Cancelled = 6
}

/// <summary>
/// 审批任务状态
/// </summary>
public enum ApprovalTaskStatus
{
    /// <summary>
    /// 待处理
    /// </summary>
    [Display(Name = "待处理")]
    Pending = 1,
    
    /// <summary>
    /// 已完成
    /// </summary>
    [Display(Name = "已完成")]
    Completed = 2,
    
    /// <summary>
    /// 已跳过
    /// </summary>
    [Display(Name = "已跳过")]
    Skipped = 3,
    
    /// <summary>
    /// 已取消
    /// </summary>
    [Display(Name = "已取消")]
    Cancelled = 4
}

/// <summary>
/// 审批结果
/// </summary>
public enum ApprovalResult
{
    /// <summary>
    /// 同意
    /// </summary>
    [Display(Name = "同意")]
    Approve = 1,
    
    /// <summary>
    /// 拒绝
    /// </summary>
    [Display(Name = "拒绝")]
    Reject = 2,
    
    /// <summary>
    /// 转交
    /// </summary>
    [Display(Name = "转交")]
    Transfer = 3,
    
    /// <summary>
    /// 加签
    /// </summary>
    [Display(Name = "加签")]
    AdditionalSign = 4
}

/// <summary>
/// 审批日志类型
/// </summary>
public enum ApprovalLogType
{
    /// <summary>
    /// 发起审批
    /// </summary>
    [Display(Name = "发起审批")]
    Start = 1,
    
    /// <summary>
    /// 审批通过
    /// </summary>
    [Display(Name = "审批通过")]
    Approve = 2,
    
    /// <summary>
    /// 审批拒绝
    /// </summary>
    [Display(Name = "审批拒绝")]
    Reject = 3,
    
    /// <summary>
    /// 转交任务
    /// </summary>
    [Display(Name = "转交任务")]
    Transfer = 4,
    
    /// <summary>
    /// 加签
    /// </summary>
    [Display(Name = "加签")]
    AdditionalSign = 5,
    
    /// <summary>
    /// 撤回审批
    /// </summary>
    [Display(Name = "撤回审批")]
    Withdraw = 6,
    
    /// <summary>
    /// 取消审批
    /// </summary>
    [Display(Name = "取消审批")]
    Cancel = 7,
    
    /// <summary>
    /// 系统自动处理
    /// </summary>
    [Display(Name = "系统自动处理")]
    System = 8
}

/// <summary>
/// 风险等级
/// </summary>
public enum RiskLevel
{
    /// <summary>
    /// 低风险
    /// </summary>
    [Display(Name = "低风险")]
    Low = 1,
    
    /// <summary>
    /// 中风险
    /// </summary>
    [Display(Name = "中风险")]
    Medium = 2,
    
    /// <summary>
    /// 高风险
    /// </summary>
    [Display(Name = "高风险")]
    High = 3,
    
    /// <summary>
    /// 极高风险
    /// </summary>
    [Display(Name = "极高风险")]
    Critical = 4
}
