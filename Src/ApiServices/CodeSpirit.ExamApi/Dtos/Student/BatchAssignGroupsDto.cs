using CodeSpirit.Amis.Attributes.FormFields;
using CodeSpirit.Core.Dtos;
using CodeSpirit.Shared.JsonConverters;
using Newtonsoft.Json;
namespace CodeSpirit.ExamApi.Dtos.Student;

/// <summary>
/// 批量分配考生到考生组的请求DTO
/// </summary>
public class BatchAssignGroupsDto : BatchOperationDto<long>
{
    [JsonConverter(typeof(CommaDelimitedListJsonConverter))]
    [AmisFormField(Type = "hidden")]
    public override List<long> Ids { get => base.Ids; set => base.Ids = value; }
    /// <summary>
    /// 要分配的考生组ID列表
    /// </summary>
    [Required(ErrorMessage = "考生组ID列表不能为空")]
    [MinLength(1, ErrorMessage = "至少需要选择一个考生组")]
    [AmisSelectField(
        Source = "${ROOT_API}/api/exam/StudentGroups/select",
        ValueField = "id",
        LabelField = "name",
        Multiple = true,
        JoinValues = false,
        ExtractValue = true,
        Searchable = true,
        Clearable = true,
        Placeholder = "请选择考生组"
    )]
    [DisplayName("考生组")]
    public List<long> GroupIds { get; set; } = new();
}