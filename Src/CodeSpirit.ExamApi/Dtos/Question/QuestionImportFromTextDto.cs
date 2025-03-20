using CodeSpirit.Amis.Attributes.FormFields;
using CodeSpirit.ExamApi.Data.Models;

namespace CodeSpirit.ExamApi.Dtos.Question
{
    public class QuestionImportFromTextDto
    {
        [DisplayName("难度（TODO：从正文解析）")]
        public QuestionDifficulty QuestionDifficulty { get; set; }

        /// <summary>
        /// 分类ID
        /// </summary>
        [Required(ErrorMessage = "请选择题目分类")]
        [DisplayName("分类")]
        [AmisTreeSelectField(
            DataSource = "${ROOT_API}/api/exam/QuestionCategories/tree",
            Multiple = false,
            Cascade = true,
            ShowOutline = true,
            LabelField = "name",
            ValueField = "id"
        )]
        public long CategoryId { get; set; }

        /// <summary>
        /// Word格式文本
        /// </summary>
        [DisplayName("Word格式文本")]
        [AmisFormField(type: "editor",AdditionalConfig = "{\"language\":\"markdown\"}")]
        [Required]
        public required string Text { get; set; }
    }
}
