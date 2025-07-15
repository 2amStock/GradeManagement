using System;
using System.Collections.Generic;

namespace GradeManagement.DAL.Models;

public partial class UserAccount
{
    public int UserId { get; set; }

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string Role { get; set; } = null!;

    public virtual ICollection<Course> Courses { get; set; } = new List<Course>();
}
