using GradeManagement.DAL.Models;
using GradeManagement.DAL.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace GradeManagement
{
    /// <summary>
    /// Interaction logic for InsertStudentWindow.xaml
    /// </summary>
    public partial class InsertStudentWindow : Window
    {

        StudentViewModel? CurrentStudent;
        GradeManagementSystemContext _context;
        StudentWindow StudentWindow;
        private ObservableCollection<SelectableSubject> _subjects;
        int capacity;
        public InsertStudentWindow(StudentWindow studentWindow, StudentViewModel? selectedStudent)
        {
            InitializeComponent();
            CurrentStudent = selectedStudent;
            _context = new GradeManagementSystemContext();
            StudentWindow = studentWindow;
            _subjects = new ObservableCollection<SelectableSubject>();
            capacity = 30; // số lượng + 1 trong 1 khóa học
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Begin();
        }
        private void Begin()
        {
            if (CurrentStudent != null)
            {
                txtName.Text = CurrentStudent.FullName;
                txtEmail.Text = CurrentStudent.Email;
                dpDOB.SelectedDate = CurrentStudent.Dob!.Value.ToDateTime(new TimeOnly(0, 0));
                txtRollNumber.Text = CurrentStudent.RollNumber;

                // Lấy danh sách Subjects và đánh dấu các môn học đã tham gia
                var student = _context.Students
                    .Include(s => s.StudentCourses)
                    .ThenInclude(sc => sc.Course)
                    .ThenInclude(c => c.Subject)
                    .FirstOrDefault(s => s.StudentId == CurrentStudent.StudentId);

                if (student != null)
                {
                    var enrolledSubjectIds = student.StudentCourses.Select(sc => sc.Course.SubjectId).Distinct().ToList();
                    var allSubjects = _context.Subjects.ToList();
                    foreach (var subject in allSubjects)
                    {
                        _subjects.Add(new SelectableSubject
                        {
                            Subject = subject,
                            IsSelected = enrolledSubjectIds.Contains(subject.SubjectId)
                        });
                    }
                }
            }
            else
            {
                var allSubjects = _context.Subjects.ToList();
                foreach (var subject in allSubjects)
                {
                    _subjects.Add(new SelectableSubject { Subject = subject, IsSelected = false });
                }
            }

            lbSubjects.ItemsSource = _subjects;
        }

        private void btnSave(object sender, RoutedEventArgs e)
        {
            if(CurrentStudent == null)
            {
                if(txtRollNumber.Text == string.Empty || txtName.Text == string.Empty || txtEmail.Text == string.Empty)
                {
                    MessageBox.Show("Please fill all the fields", "alert", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
               
                if (_context.Students.Any(s => s.RollNumber.ToUpper().Equals(txtRollNumber.Text.ToUpper())))
                {
                    MessageBox.Show("Roll Number already exists", "alert", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                if (_context.Students.Any(s => s.Email.ToLower().Equals(txtEmail.Text.ToLower())))
                {
                    MessageBox.Show("Email already exists", "alert", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                Student student = new Student
                {
                    FullName = txtName.Text,
                    Email = txtEmail.Text.ToLower(),
                    Dob = DateOnly.FromDateTime(dpDOB.SelectedDate!.Value),
                    RollNumber = txtRollNumber.Text.ToUpper()
                };
                _context.Students.Add(student);
                _context.SaveChanges();

               
                var selectedSubjects = _subjects.Where(s => s.IsSelected).Select(s => s.Subject!.SubjectId).ToList();
                if (selectedSubjects.Any())
                {
                    AssignRandomCourseToStudent(student, selectedSubjects);
                }

            }
            else
            {
                if (txtRollNumber.Text == string.Empty || txtName.Text == string.Empty || txtEmail.Text == string.Empty)
                {
                    MessageBox.Show("Please fill all the fields", "alert", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (_context.Students.Any(s => s.RollNumber.Equals(txtRollNumber) && s.StudentId != CurrentStudent.StudentId))
                {
                    MessageBox.Show("Roll Number already exists", "alert", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                if (_context.Students.Any(s => s.Email.Equals(txtEmail.Text) && s.StudentId != CurrentStudent.StudentId))
                {
                    MessageBox.Show("Email already exists", "alert", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                Student student = _context.Students.Where(s => s.StudentId == CurrentStudent.StudentId).FirstOrDefault()!;
                student.FullName = txtName.Text;
                student.Email = txtEmail.Text.ToLower();
                student.Dob = DateOnly.FromDateTime(dpDOB.SelectedDate!.Value);
                student.RollNumber = txtRollNumber.Text.ToUpper();
                _context.Students.Update(student);
                _context.SaveChanges();

                var selectedSubjectIds = _subjects.Where(s => s.IsSelected).Select(s => s.Subject!.SubjectId).ToList();
                UpdateCoursesForStudent(student, selectedSubjectIds);
            }

            StudentWindow.Refesh();
            this.Close();
            
        }

        private void UpdateCoursesForStudent(Student student, List<string> selectedSubjectIds)
        {
            // Xóa tất cả các khóa học hiện tại của sinh viên
            var existingCourses = _context.StudentCourses.Where(sc => sc.StudentId == student.StudentId).ToList();
            _context.StudentCourses.RemoveRange(existingCourses);
            _context.SaveChanges();
            // Gán khóa học mới cho sinh viên
            AssignRandomCourseToStudent(student, selectedSubjectIds);
        }
        private void AssignRandomCourseToStudent(Student student, List<string> selectedSubjectIds)
        {
            
            var relevantCourses = _context.Courses
                .Where(c => selectedSubjectIds.Contains(c.SubjectId)) 
                .ToList();

            if (!relevantCourses.Any())
            {
                MessageBox.Show("No courses available for the selected subjects.");
                return;
            }

            var commonClassCodes = _context.Courses
                .Where(c => selectedSubjectIds.Contains(c.SubjectId))
                .GroupBy(c => c.ClassCode)
                .Where(g => g.Select(c => c.SubjectId).Distinct().Count() == selectedSubjectIds.Count)
                .Select(g => g.Key)
                .ToList();

            
            if (commonClassCodes.Any())
            {
                var randomCommonClassCode = commonClassCodes[new Random().Next(commonClassCodes.Count)];

                foreach (var subjectId in selectedSubjectIds)
                {
                    var random = new Random();
                    var CourseId = relevantCourses
                        .Where(c => c.ClassCode == randomCommonClassCode && c.SubjectId == subjectId && _context.StudentCourses.Count(sc => sc.CourseId == c.CourseId) < capacity)
                        .Select(c => c.CourseId)
                        .FirstOrDefault();
                    
                    if (CourseId == 0)
                    {
                        CourseId = relevantCourses
                            .Where(c => c.SubjectId == subjectId &&
                                        _context.StudentCourses.Count(sc => sc.CourseId == c.CourseId) < capacity)
                            .Select(c => c.CourseId)
                            .FirstOrDefault();
                    }

                    if (CourseId == 0)
                    {
                        MessageBox.Show($"Không tìm được khóa học phù hợp cho môn {subjectId}.");
                        continue;
                    }
                    var studentCourse = new StudentCourse
                    {
                        StudentId = student.StudentId,
                        CourseId = CourseId,
                        Status = "In process"
                    };
                    _context.StudentCourses.Add(studentCourse);
                }
                
            }
            else
            {
                foreach (var subjectId in selectedSubjectIds)
                {
                    var random = new Random();
                    var CourseID = relevantCourses
                        .Where(c => c.SubjectId == subjectId  && _context.StudentCourses.Count(sc => sc.CourseId == c.CourseId) < capacity)
                        .Select(c => c.CourseId)                    
                        .FirstOrDefault();
                    if (CourseID == 0)
                    {
                        MessageBox.Show($"Không tìm được khóa học phù hợp cho môn {subjectId}.");
                        continue;
                    }
                    var studentCourse = new StudentCourse
                    {
                        StudentId = student.StudentId,
                        CourseId = CourseID,
                        Status = "In process"
                    };
                    _context.StudentCourses.Add(studentCourse);
                }
            }

            
            _context.SaveChanges();

        }
    }
}
