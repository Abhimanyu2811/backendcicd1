using System;
using System.Collections.Generic;

namespace finalpracticeproject.DTOs
{
    public class QuestionCreateDto
    {
        public Guid QuestionId { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public List<OptionCreateDto> Options { get; set; } = new List<OptionCreateDto>();
    }
} 