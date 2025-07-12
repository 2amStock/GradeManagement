using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GradeManagement.DAL.ViewModels
{
    public class StudentGradeInfo
    {
        public int GradeItemId { get; set; }
        public string? GradeItemName { get; set; } 
        public double? Mark { get; set; }          
        public double Weight { get; set; }         
    }
}
