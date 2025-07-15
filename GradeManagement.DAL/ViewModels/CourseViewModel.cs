using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GradeManagement.DAL.ViewModels
{
    public class CourseViewModel
    {
        public int CourseId { get; set; }

        public string ClassCode { get; set; } = null!;

        public string SubjectId { get; set; } = null!;

        public DateOnly? StartDate { get; set; }

        public DateOnly? EndDate { get; set; }

        public string? LectureName { get; set; }
    }
}
