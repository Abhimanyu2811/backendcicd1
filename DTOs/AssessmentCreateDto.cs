using System;
using System.Collections.Generic;

namespace finalpracticeproject.DTOs
{
    public class AssessmentCreateDto
    {
        public Guid AssessmentId { get; set; }
        public Guid? CourseId { get; set; }
        public string? Title { get; set; }
        public int? MaxScore { get; set; }
        public List<QuestionCreateDto> Questions { get; set; } = new List<QuestionCreateDto>();
    }
}
