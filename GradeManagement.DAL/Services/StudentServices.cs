using GradeManagement.DAL.Models;
using GradeManagement.DAL.ViewModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GradeManagement.DAL.Services
{
    public class StudentServices
    {
        GradeManagementSystemContext _context;

        public StudentServices()
        {
            _context = new GradeManagementSystemContext();
        }

        public List<Student> GetStudents()
        {
            

            var students = _context.Students
        .Include(s => s.StudentCourses)
        .ThenInclude(sc => sc.Course)
        .Where(s => s.StudentCourses.Any(sc => sc.Status == "In process"))
        .ToList();

            return students;
        }

        public void AddStudent(Student student)
        {
            _context.Students.Add(student);
            _context.SaveChanges();
        }

        public List<StudentViewModel> GetStudentsWithCourses()
        {
            var students = _context.Students
                .Include(s => s.StudentCourses)
                .ThenInclude(sc => sc.Course)
                .ThenInclude(c => c.Subject)
                .Select(s => new StudentViewModel
                {
                    StudentId = s.StudentId,
                    FullName = s.FullName,
                    Email = s.Email,
                    Dob = s.Dob,
                    RollNumber = s.RollNumber,
                    ClassCodesWithSubjectId = string.Join(", ", s.StudentCourses.Select(sc => $"{sc.Course.ClassCode}({sc.Course.SubjectId})"))
                })
                .ToList();

            return students;
        }
    }
}
