using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GradeManagement.DAL.ViewModels
{
    public class GradeViewModel
    {
        public int StudentId { get; set; }
        public string RollNumber { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public Dictionary<int, StudentGradeInfo> GradeDetails { get; set; } = new Dictionary<int, StudentGradeInfo>();
    }
}
