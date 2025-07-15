using System;
using System.Collections.Generic;

namespace GradeManagement.DAL.Models;

public partial class Course
{
    public int CourseId { get; set; }

    public string ClassCode { get; set; } = null!;

    public string SubjectId { get; set; } = null!;

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public int? LecturerId { get; set; }

    public virtual ICollection<Grade> Grades { get; set; } = new List<Grade>();

    public virtual UserAccount? Lecturer { get; set; }

    public virtual ICollection<StudentCourse> StudentCourses { get; set; } = new List<StudentCourse>();

    public virtual Subject Subject { get; set; } = null!;
}
