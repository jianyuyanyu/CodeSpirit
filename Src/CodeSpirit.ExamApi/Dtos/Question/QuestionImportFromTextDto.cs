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
        [AmisFormField(type: "editor",Placeholder = @"一、单项选择题（每题1分，80小题，共计80分）
1、SSL协议最早是由(B)提出的。
A、Microsoft
B、Netscape
C、ISO
D、IBM
【解析】SSL协议是由Netscape公司提出的，用于在互联网上提供安全通信。
【标签】SSL、安全通信

二、判断题（每空1分，共计20分）
1. 平邮包裹的到货周期较长，顾客通常要7 - 15天才能收到购买的商品, 但是提供了网上查询物流进程的服务。（ 对 ）
【解析】平邮包裹确实有网上查询物流进程的服务。
【标签】平邮、商品
", AdditionalConfig = "{\"language\":\"markdown\",\"size\":\"xl\"}")]
        [Required]
        public required string Text { get; set; }
    }
}
