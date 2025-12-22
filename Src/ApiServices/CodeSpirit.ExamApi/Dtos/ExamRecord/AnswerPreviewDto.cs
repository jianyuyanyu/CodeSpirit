using CodeSpirit.ExamApi.Dtos.Client;
using System.ComponentModel;

namespace CodeSpirit.ExamApi.Dtos.ExamRecord
{
    /// <summary>
    /// 包含正确答案的答题预览DTO
    /// </summary>
    public class ClientExamAnswerWithCorrectDto : ClientExamAnswerDto
    {
        /// <summary>
        /// 正确答案
        /// </summary>
        [DisplayName("正确答案")]
        public string CorrectAnswer { get; set; } = string.Empty;
        
        /// <summary>
        /// 题目类型
        /// </summary>
        [DisplayName("题目类型")]
        public string QuestionType { get; set; } = string.Empty;
        
        /// <summary>
        /// 得分
        /// </summary>
        [DisplayName("得分")]
        public double? Score { get; set; }
        
        /// <summary>
        /// 是否正确
        /// </summary>
        [DisplayName("是否正确")]
        public bool? IsCorrect { get; set; }
        
        /// <summary>
        /// 题目分值
        /// </summary>
        [DisplayName("题目分值")]
        public int DefaultScore { get; set; }
        
        /// <summary>
        /// 题目序号（答卷中的顺序）
        /// </summary>
        [DisplayName("题目序号")]
        public int OrderNumber { get; set; }
    }

    public class AnswerPreviewDto
    {
        /// <summary>
        /// 试卷ID
        /// </summary>
        public long ExamPaperId { get; set; } 
        
        /// <summary>
        /// 试卷总分
        /// </summary>
        public int TotalScore { get; set; }
        
        /// <summary>
        /// 考生总得分
        /// </summary>
        public double? StudentScore { get; set; }
        
        /// <summary>
        /// 考试答案列表
        /// </summary>
        public List<ClientExamAnswerWithCorrectDto> Answers { get; set; }
    }
}
