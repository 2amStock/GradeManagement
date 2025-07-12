using System;
using System.Collections.Generic;

namespace GradeManagement.DAL.Models;

public partial class Mark
{
    public int GradeId { get; set; }

    public int GradeItemId { get; set; }

    public double? Mark1 { get; set; }

    public virtual Grade Grade { get; set; } = null!;

    public virtual GradeItem GradeItem { get; set; } = null!;
}
