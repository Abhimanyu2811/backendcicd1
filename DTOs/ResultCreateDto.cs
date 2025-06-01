using System;

namespace finalpracticeproject.DTOs
{
    public class ResultCreateDto
    {
        public Guid ResultId { get; set; }
        public Guid? AssessmentId { get; set; }
        public Guid? UserId { get; set; }
        public int? Score { get; set; }
        public DateTime? AttemptDate { get; set; }
    }
}
