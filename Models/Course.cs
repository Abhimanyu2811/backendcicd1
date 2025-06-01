using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backendapi.Models;

public partial class Course
{
    [Key]
    public Guid CourseId { get; set; }

    public string? Title { get; set; }

    public string? Description { get; set; }

    public Guid? InstructorId { get; set; }

    public string? MediaUrl { get; set; }

    public string? CourseUrl { get; set; }

    public virtual ICollection<Assessment> Assessments { get; set; } = new List<Assessment>();

    // Instructor who created the course
    [ForeignKey("InstructorId")]
    public virtual User? Instructor { get; set; }

    // Users who enrolled in this course
    public virtual ICollection<User> EnrolledStudents { get; set; } = new List<User>();
}
