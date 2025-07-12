﻿using System;
using System.Collections.Generic;

namespace GradeManagement.DAL.Models;

public  class StudentCourse
{
    public int StudentId { get; set; }

    public int CourseId { get; set; }

    public string Status { get; set; } = null!;

    public virtual Course Course { get; set; } = null!;

    public virtual Student Student { get; set; } = null!;
}
