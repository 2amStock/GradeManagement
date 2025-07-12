using System;
using System.Collections.Generic;

namespace GradeManagement.DAL.Models;

public partial class Grade
{
    public int GradeId { get; set; }

    public int? CourseId { get; set; }

    public int? StudentId { get; set; }

    public double? Mark { get; set; }

    public virtual Course? Course { get; set; }

    public virtual ICollection<Mark> Marks { get; set; } = new List<Mark>();

    public virtual Student? Student { get; set; }
}
