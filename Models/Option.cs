using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backendapi.Models;

public class Option
{
    [Key]
    public Guid OptionId { get; set; }

    public Guid QuestionId { get; set; }

    [Required]
    public string Text { get; set; } = string.Empty;

    public bool IsCorrect { get; set; }

    // Navigation
    [ForeignKey("QuestionId")]
    public virtual Question? Question { get; set; }
} 