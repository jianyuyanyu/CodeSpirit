using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CodeSpirit.ApprovalApi.Models;
using Newtonsoft.Json;

namespace CodeSpirit.ApprovalApi.Dtos.Visualization;

/// <summary>
/// 工作流可视化DTO
/// </summary>
public class WorkflowVisualizationDto
{
    /// <summary>
    /// 节点列表
    /// </summary>
    [DisplayName("节点列表")]
    public List<FlowChartNodeDto> Nodes { get; set; } = new();

    /// <summary>
    /// 连线列表
    /// </summary>
    [DisplayName("连线列表")]
    public List<FlowChartEdgeDto> Edges { get; set; } = new();

    /// <summary>
    /// 流程图配置
    /// </summary>
    [DisplayName("流程图配置")]
    public FlowChartConfigDto Config { get; set; } = new();
}

/// <summary>
/// 流程图节点DTO
/// </summary>
public class FlowChartNodeDto
{
    /// <summary>
    /// 节点ID
    /// </summary>
    [DisplayName("节点ID")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 节点标签
    /// </summary>
    [DisplayName("节点标签")]
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// 节点类型
    /// </summary>
    [DisplayName("节点类型")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 节点位置
    /// </summary>
    [DisplayName("节点位置")]
    public NodePositionDto Position { get; set; } = new();

    /// <summary>
    /// 节点数据
    /// </summary>
    [DisplayName("节点数据")]
    public object Data { get; set; } = new();

    /// <summary>
    /// 节点样式
    /// </summary>
    [DisplayName("节点样式")]
    public NodeStyleDto Style { get; set; } = new();
}

/// <summary>
/// 流程图连线DTO
/// </summary>
public class FlowChartEdgeDto
{
    /// <summary>
    /// 连线ID
    /// </summary>
    [DisplayName("连线ID")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 源节点ID
    /// </summary>
    [DisplayName("源节点ID")]
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// 目标节点ID
    /// </summary>
    [DisplayName("目标节点ID")]
    public string Target { get; set; } = string.Empty;

    /// <summary>
    /// 连线标签
    /// </summary>
    [DisplayName("连线标签")]
    public string? Label { get; set; }

    /// <summary>
    /// 连线类型
    /// </summary>
    [DisplayName("连线类型")]
    public string Type { get; set; } = "smoothstep";

    /// <summary>
    /// 连线样式
    /// </summary>
    [DisplayName("连线样式")]
    public EdgeStyleDto Style { get; set; } = new();
}

/// <summary>
/// 节点位置DTO
/// </summary>
public class NodePositionDto
{
    /// <summary>
    /// X坐标
    /// </summary>
    [DisplayName("X坐标")]
    public double X { get; set; }

    /// <summary>
    /// Y坐标
    /// </summary>
    [DisplayName("Y坐标")]
    public double Y { get; set; }
}

/// <summary>
/// 节点样式DTO
/// </summary>
public class NodeStyleDto
{
    /// <summary>
    /// 宽度
    /// </summary>
    [DisplayName("宽度")]
    public int Width { get; set; } = 180;

    /// <summary>
    /// 高度
    /// </summary>
    [DisplayName("高度")]
    public int Height { get; set; } = 40;

    /// <summary>
    /// 背景色
    /// </summary>
    [DisplayName("背景色")]
    public string BackgroundColor { get; set; } = "#ffffff";

    /// <summary>
    /// 边框色
    /// </summary>
    [DisplayName("边框色")]
    public string BorderColor { get; set; } = "#000000";

    /// <summary>
    /// 边框宽度
    /// </summary>
    [DisplayName("边框宽度")]
    public int BorderWidth { get; set; } = 1;

    /// <summary>
    /// 圆角
    /// </summary>
    [DisplayName("圆角")]
    public int BorderRadius { get; set; } = 5;

    /// <summary>
    /// 字体颜色
    /// </summary>
    [DisplayName("字体颜色")]
    public string Color { get; set; } = "#000000";

    /// <summary>
    /// 字体大小
    /// </summary>
    [DisplayName("字体大小")]
    public int FontSize { get; set; } = 12;
}

/// <summary>
/// 连线样式DTO
/// </summary>
public class EdgeStyleDto
{
    /// <summary>
    /// 线条颜色
    /// </summary>
    [DisplayName("线条颜色")]
    public string Stroke { get; set; } = "#000000";

    /// <summary>
    /// 线条宽度
    /// </summary>
    [DisplayName("线条宽度")]
    public int StrokeWidth { get; set; } = 2;

    /// <summary>
    /// 线条样式
    /// </summary>
    [DisplayName("线条样式")]
    public string StrokeDasharray { get; set; } = "none";
}

/// <summary>
/// 流程图配置DTO
/// </summary>
public class FlowChartConfigDto
{
    /// <summary>
    /// 是否显示网格
    /// </summary>
    [DisplayName("是否显示网格")]
    public bool ShowGrid { get; set; } = true;

    /// <summary>
    /// 是否可拖拽
    /// </summary>
    [DisplayName("是否可拖拽")]
    public bool Draggable { get; set; } = true;

    /// <summary>
    /// 是否可连接
    /// </summary>
    [DisplayName("是否可连接")]
    public bool Connectable { get; set; } = true;

    /// <summary>
    /// 是否可删除
    /// </summary>
    [DisplayName("是否可删除")]
    public bool Deletable { get; set; } = true;

    /// <summary>
    /// 缩放范围
    /// </summary>
    [DisplayName("缩放范围")]
    public ZoomRangeDto ZoomRange { get; set; } = new();
}

/// <summary>
/// 缩放范围DTO
/// </summary>
public class ZoomRangeDto
{
    /// <summary>
    /// 最小缩放
    /// </summary>
    [DisplayName("最小缩放")]
    public double Min { get; set; } = 0.1;

    /// <summary>
    /// 最大缩放
    /// </summary>
    [DisplayName("最大缩放")]
    public double Max { get; set; } = 2.0;
}

/// <summary>
/// 流程验证结果DTO
/// </summary>
public class WorkflowValidationResultDto
{
    /// <summary>
    /// 是否有效
    /// </summary>
    [DisplayName("是否有效")]
    public bool IsValid { get; set; }

    /// <summary>
    /// 错误列表
    /// </summary>
    [DisplayName("错误列表")]
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// 警告列表
    /// </summary>
    [DisplayName("警告列表")]
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// 验证详情
    /// </summary>
    [DisplayName("验证详情")]
    public List<ValidationDetailDto> Details { get; set; } = new();
}

/// <summary>
/// 验证详情DTO
/// </summary>
public class ValidationDetailDto
{
    /// <summary>
    /// 节点ID
    /// </summary>
    [DisplayName("节点ID")]
    public long? NodeId { get; set; }

    /// <summary>
    /// 节点名称
    /// </summary>
    [DisplayName("节点名称")]
    public string? NodeName { get; set; }

    /// <summary>
    /// 验证类型
    /// </summary>
    [DisplayName("验证类型")]
    public string ValidationType { get; set; } = string.Empty;

    /// <summary>
    /// 验证消息
    /// </summary>
    [DisplayName("验证消息")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 严重程度
    /// </summary>
    [DisplayName("严重程度")]
    public ValidationSeverity Severity { get; set; }
}

/// <summary>
/// 验证严重程度
/// </summary>
public enum ValidationSeverity
{
    /// <summary>
    /// 信息
    /// </summary>
    [Display(Name = "信息")]
    Info = 1,

    /// <summary>
    /// 警告
    /// </summary>
    [Display(Name = "警告")]
    Warning = 2,

    /// <summary>
    /// 错误
    /// </summary>
    [Display(Name = "错误")]
    Error = 3
}

/// <summary>
/// 流程实例状态DTO
/// </summary>
public class WorkflowInstanceStatusDto
{
    /// <summary>
    /// 实例ID
    /// </summary>
    [DisplayName("实例ID")]
    public long InstanceId { get; set; }

    /// <summary>
    /// 当前节点ID
    /// </summary>
    [DisplayName("当前节点ID")]
    public long? CurrentNodeId { get; set; }

    /// <summary>
    /// 当前节点名称
    /// </summary>
    [DisplayName("当前节点名称")]
    public string? CurrentNodeName { get; set; }

    /// <summary>
    /// 实例状态
    /// </summary>
    [DisplayName("实例状态")]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// 已完成的节点
    /// </summary>
    [DisplayName("已完成的节点")]
    public List<CompletedNodeDto> CompletedNodes { get; set; } = new();

    /// <summary>
    /// 进度百分比
    /// </summary>
    [DisplayName("进度百分比")]
    public decimal ProgressPercentage { get; set; }
}

/// <summary>
/// 已完成节点DTO
/// </summary>
public class CompletedNodeDto
{
    /// <summary>
    /// 节点ID
    /// </summary>
    [DisplayName("节点ID")]
    public long NodeId { get; set; }

    /// <summary>
    /// 节点名称
    /// </summary>
    [DisplayName("节点名称")]
    public string NodeName { get; set; } = string.Empty;

    /// <summary>
    /// 完成时间
    /// </summary>
    [DisplayName("完成时间")]
    public DateTime CompletedAt { get; set; }

    /// <summary>
    /// 处理人
    /// </summary>
    [DisplayName("处理人")]
    public string ProcessedBy { get; set; } = string.Empty;

    /// <summary>
    /// 处理结果
    /// </summary>
    [DisplayName("处理结果")]
    public string Result { get; set; } = string.Empty;
}
