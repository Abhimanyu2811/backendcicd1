using System;
using System.Collections.Generic;

namespace finalpracticeproject.DTOs
{
    public class AssessmentSubmissionDto
    {
        public Guid AssessmentId { get; set; }
        public Guid UserId { get; set; }
        public List<AnswerDto> Answers { get; set; } = new List<AnswerDto>();
    }

    public class AnswerDto
    {
        public Guid QuestionId { get; set; }
        public Guid SelectedOptionId { get; set; }
    }
} 