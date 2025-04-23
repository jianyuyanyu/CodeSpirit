using CodeSpirit.Amis.Attributes.FormFields;
using CodeSpirit.Core.Dtos;
using CodeSpirit.Shared.JsonConverters;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.ExamApi.Dtos.ExamRecord;

/// <summary>
/// 批量批改考试分数DTO
/// </summary>
[DisplayName("批量批改")]
public class BatchModifyExamScoreDto: BatchOperationDto<long>
{
    [JsonConverter(typeof(CommaDelimitedListJsonConverter))]
    [AmisFormField(Type = "hidden")]
    public override List<long> Ids { get => base.Ids; set => base.Ids = value; }

    /// <summary>
    /// 目标分数
    /// </summary>
    [DisplayName("目标分数")]
    [Required(ErrorMessage = "目标分数不能为空")]
    [Range(0, 1000, ErrorMessage = "目标分数必须在0-1000之间")]
    public int TargetScore { get; set; }
} 