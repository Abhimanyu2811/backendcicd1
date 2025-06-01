using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backendapi.Models;

public partial class Result
{
    public Guid ResultId { get; set; }

    public Guid? AssessmentId { get; set; }

    public Guid? UserId { get; set; }

    public int? Score { get; set; }

    [Column(TypeName = "datetime2")]
    public DateTime? AttemptDate { get; set; }

    public virtual Assessment? Assessment { get; set; }

    public virtual User? User { get; set; }

    public virtual ICollection<StudentAnswer> StudentAnswers { get; set; } = new List<StudentAnswer>();

    //public static implicit operator Result(Result v)
    //{
    //    throw new NotImplementedException();
    //}
}
