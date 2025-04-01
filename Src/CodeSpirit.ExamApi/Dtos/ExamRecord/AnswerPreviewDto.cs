using CodeSpirit.ExamApi.Dtos.Client;

namespace CodeSpirit.ExamApi.Dtos.ExamRecord
{
    public class AnswerPreviewDto
    {
        public long ExamPaperId { get; set; } 
        public List<ClientExamAnswerDto> Answers { get; set; }
    }
}
