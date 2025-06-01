using System;
using System.Collections.Generic;

namespace finalpracticeproject.DTOs
{
    public class UserCreateDto
    {
        public Guid UserId { get; set; }

        public string? Name { get; set; }

        public string? Email { get; set; }

        public string? Role { get; set; }

        public string? PasswordHash { get; set; }

        // Instead of full Course objects, just use IDs of courses the user will be enrolled in
        public List<Guid>? CourseIds { get; set; }
    }
}
