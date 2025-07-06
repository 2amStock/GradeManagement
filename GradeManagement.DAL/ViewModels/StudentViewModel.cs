using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GradeManagement.DAL.ViewModels
{
    public class StudentViewModel
    {
        public int StudentId { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public DateOnly? Dob { get; set; }
        public string? RollNumber { get; set; }
        public string? ClassCodesWithSubjectId { get; set; }
    }
}
