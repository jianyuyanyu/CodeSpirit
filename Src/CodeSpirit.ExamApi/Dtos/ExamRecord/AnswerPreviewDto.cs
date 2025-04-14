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
    }

    public class AnswerPreviewDto
    {
        public long ExamPaperId { get; set; } 
        public List<ClientExamAnswerWithCorrectDto> Answers { get; set; }
    }
}
