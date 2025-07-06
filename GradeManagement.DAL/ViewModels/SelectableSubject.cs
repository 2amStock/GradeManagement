using GradeManagement.DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GradeManagement.DAL.ViewModels
{
    public class SelectableSubject
    {
        public virtual Subject? Subject { get; set; }
        public bool IsSelected { get; set; }
    }
}
