using System;
using System.Collections.Generic;

namespace GradeManagement.DAL.Models;

public partial class GradeCategory
{
    public int GradeCategoryId { get; set; }

    public string GradeCategoryName { get; set; } = null!;

    public virtual ICollection<GradeItem> GradeItems { get; set; } = new List<GradeItem>();
}
