using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backendapi.Models;

public partial class User
{
    [Key]
    public Guid UserId { get; set; }

    public string? Name { get; set; }

    public string? Email { get; set; }

    public string? Role { get; set; }

    public string? PasswordHash { get; set; }

    public string? PasswordSalt { get; set; }

    // Courses created by instructor
    public virtual ICollection<Course> Courses { get; set; } = new List<Course>();

    // Results from quizzes or assessments
    public virtual ICollection<Result> Results { get; set; } = new List<Result>();

    // Courses enrolled by this user (as a student)
    public virtual ICollection<Course> EnrolledCourses { get; set; } = new List<Course>();
}
