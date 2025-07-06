using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GradeManagement.DAL.ViewModels
{
    public class StudentCourseViewModel
    {
        public int CourseId { get; set; }

        public string ClassCode { get; set; } = null!;

        public string? SubjectId { get; set; }




    }
}
