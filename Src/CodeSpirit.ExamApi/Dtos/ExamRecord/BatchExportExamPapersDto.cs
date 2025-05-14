using CodeSpirit.Amis.Attributes.FormFields;
using CodeSpirit.Core.Dtos;
using CodeSpirit.Shared.JsonConverters;
using Newtonsoft.Json;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.ExamApi.Dtos.ExamRecord;

/// <summary>
/// 批量导出考试试卷DTO
/// </summary>
public class BatchExportExamPapersDto : BatchOperationDto<long>
{
    [JsonConverter(typeof(CommaDelimitedListJsonConverter))]
    [AmisFormField(Type = "hidden")]
    public override List<long> Ids { get => base.Ids; set => base.Ids = value; }

    [AmisFormField(Type = "markdown", DefaultValue = "**导出说明：** 系统将排队生成导出文件，并打包为压缩包以供下载。大量数据导出可能需要较长时间，请耐心等待。")]
    public string? Tip { get; set; }
    ///// <summary>
    ///// 是否包含答案
    ///// </summary>
    //[DisplayName("是否包含答案")]
    //public bool IncludeAnswers { get; set; } = true;

    ///// <summary>
    ///// 是否包含评分信息
    ///// </summary>
    //[DisplayName("是否包含评分信息")]
    //public bool IncludeScores { get; set; } = true;
}