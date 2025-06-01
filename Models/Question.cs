using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backendapi.Models;

public class Question
{
    [Key]
    public Guid QuestionId { get; set; }

    public Guid AssessmentId { get; set; }

    [Required]
    public string QuestionText { get; set; } = string.Empty;

    // Navigation
    [ForeignKey("AssessmentId")]
    public virtual Assessment? Assessment { get; set; }

    public virtual ICollection<Option> Options { get; set; } = new List<Option>();
} 