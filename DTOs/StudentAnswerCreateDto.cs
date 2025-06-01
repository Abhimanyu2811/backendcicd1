using System;

namespace finalpracticeproject.DTOs
{
    public class StudentAnswerCreateDto
    {
        public Guid AnswerId { get; set; }
        public Guid ResultId { get; set; }
        public Guid QuestionId { get; set; }
        public Guid SelectedOptionId { get; set; }
    }
} 