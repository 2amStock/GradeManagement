using System;
using System.Collections.Generic;

namespace GradeManagement.DAL.Models;

public partial class GradeItem
{
    public int GradeItemId { get; set; }

    public int? GradeCategoryId { get; set; }

    public double Weight { get; set; }

    public string? GradeItemName { get; set; }

    public string? SubjectId { get; set; }

    public virtual GradeCategory? GradeCategory { get; set; }

    public virtual ICollection<Mark> Marks { get; set; } = new List<Mark>();

    public virtual Subject? Subject { get; set; }
}
