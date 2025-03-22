using CodeSpirit.ExamApi.Data.Models;

namespace CodeSpirit.ExamApi.Dtos.Question
{
    public class QuestionSelectListQueryDto
    {
        /// <summary>
        /// 题目类型
        /// </summary>
        [DisplayName("题目类型")]
        public QuestionType? Type { get; set; }

        /// <summary>
        /// 题目难度
        /// </summary>
        [DisplayName("难度")]
        public QuestionDifficulty? Difficulty { get; set; }

        /// <summary>
        /// 题目分类
        /// </summary>
        public List<long>? CategoryIds { get; set; }
    }
}
