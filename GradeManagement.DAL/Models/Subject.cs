using System;
using System.Collections.Generic;

namespace GradeManagement.DAL.Models;

public partial class Subject
{
    public string SubjectId { get; set; } = null!;

    public string? SubjectName { get; set; }

    public virtual ICollection<Course> Courses { get; set; } = new List<Course>();
}
