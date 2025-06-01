using System;

namespace finalpracticeproject.DTOs
{
    public class OptionCreateDto
    {
        public Guid OptionId { get; set; }
        public string Text { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
    }
} 