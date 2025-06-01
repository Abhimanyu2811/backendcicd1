using System;
using System.Collections.Generic;

namespace finalpracticeproject.DTOs
{
    public class CourseCreateDto
    {
        public Guid CourseId { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public Guid? InstructorId { get; set; }
        public string? MediaUrl { get; set; }
        public string? CourseUrl { get; set; }

        // Don't include Assessments or full User objects in DTO
    }
}
