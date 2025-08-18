namespace CodeSpirit.ConfigCenter.Dtos.App;

/// <summary>
/// 快速保存请求数据传输对象
/// </summary>
public class QuickSaveRequestDto
{
    /// <summary>
    /// 修改的行数据
    /// </summary>
    public List<AppDto> Rows { get; set; }

    /// <summary>
    /// 行差异数据
    /// </summary>
    public List<AppDiffDto> RowsDiff { get; set; }

    /// <summary>
    /// 修改的应用ID列表
    /// </summary>
    public string Ids { get; set; }

    /// <summary>
    /// 未修改的行数据
    /// </summary>
    public List<AppDto> UnModifiedItems { get; set; }
} 