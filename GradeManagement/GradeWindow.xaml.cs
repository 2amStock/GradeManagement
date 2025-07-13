using GradeManagement.DAL.Models;
using GradeManagement.DAL.ViewModels;
using System;
using System.Collections.Generic;
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
    /// Interaction logic for GradeWindow.xaml
    /// </summary>
    public partial class GradeWindow : Window
    {
        GradeManagementSystemContext _context;
        List<GradeItem> GradeItems = new List<GradeItem>();
        public GradeWindow()
        {
            InitializeComponent();
            _context = new GradeManagementSystemContext();
        }



        private void GenerateGradeItemColumns(List<GradeItem> gradeItems)
        {
            while (dgStudentGrades.Columns.Count > 2)
                dgStudentGrades.Columns.RemoveAt(2);

            foreach (var item in gradeItems)
            {
                var column = new DataGridTextColumn
                {
                    Header = item.GradeItemName,
                    Binding = new Binding($"GradeDetails[{item.GradeItemId}].Mark"),
                    IsReadOnly = false
                };

                column.ElementStyle = new Style(typeof(TextBlock));
                column.ElementStyle.Setters.Add(new Setter(ToolTipProperty,
                    new Binding($"GradeDetails[{item.GradeItemId}].Weight")
                    {
                        StringFormat = "Weight: {0}%"
                    }));

                dgStudentGrades.Columns.Add(column);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Begin();
        }

        public void Begin()
        {
            cbCourses.SelectedValuePath = "CourseId";
            cbCourses.DisplayMemberPath = "DisplayName";
            cbCourses.ItemsSource = _context.Courses.ToList();
            cbSubjects.SelectedValuePath = "SubjectId";
            cbSubjects.DisplayMemberPath = "SubjectId";
            cbSubjects.ItemsSource = _context.Subjects.ToList();
            cbCategorys.SelectedValuePath = "GradeCategoryId";
            cbCategorys.DisplayMemberPath = "GradeCategoryName";
            cbCategorys.ItemsSource = _context.GradeCategories.ToList();
            cbCourses.SelectedIndex = 0; 
            dgStudentGrades.SelectedItem = null;
            spGradeItemDetails.Children.Clear();
        }

        private void dgCourse_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadGradeViewModel();
            }
        private void LoadGradeViewModel()
        {
            if (cbCourses.SelectedItem is Course selectedCourse)
            {
                spGradeItemDetails.Children.Clear();

                int courseId = selectedCourse.CourseId;
                string subjectId = selectedCourse.SubjectId;


                GradeItems = _context.GradeItems
                    .Where(gi => gi.SubjectId == subjectId)
                    .ToList();

                var Students = _context.StudentCourses
                    .Where(sc => sc.CourseId == courseId && sc.Status == "In process")
                    .Select(sc => sc.Student)
                    .ToList();

                var Grades = _context.Grades
                    .Where(g => g.CourseId == courseId)
                    .ToList();


                var GradeIds = Grades.Select(g => g.GradeId).ToList();
                var Marks = _context.Marks
                    .Where(m => GradeIds.Contains(m.GradeId))
                    .ToList();

                var StudentVMs = new List<GradeViewModel>();


                foreach (var student in Students)
                {
                    var Grade = Grades.FirstOrDefault(g => g.StudentId == student.StudentId);
                    if (Grade == null)
                    {
                        Grade = new Grade
                        {
                            CourseId = courseId,
                            StudentId = student.StudentId
                        };
                        _context.Grades.Add(Grade);
                        _context.SaveChanges();
                        Grades.Add(Grade);
                    }
                    var studentVM = new GradeViewModel
                    {
                        StudentId = student.StudentId,
                        FullName = student.FullName,
                        RollNumber = student.RollNumber
                    };
                    foreach (var item in GradeItems)
                    {
                        var mark = Marks.FirstOrDefault(m => m.GradeId == Grade.GradeId && m.GradeItemId == item.GradeItemId);

                        studentVM.GradeDetails[item.GradeItemId] = new StudentGradeInfo
                        {
                            GradeItemId = item.GradeItemId,
                            GradeItemName = item.GradeItemName,
                            Weight = item.Weight,
                            Mark = mark?.Mark1
                        };
                    }
                    StudentVMs.Add(studentVM);
                }
                dgStudentGrades.ItemsSource = StudentVMs;
                GenerateGradeItemColumns(GradeItems);
            }
        }

        private void dgStudentGrades_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgStudentGrades.SelectedItem is GradeViewModel selectedStudent)
            {
                spGradeItemDetails.Children.Clear();

                foreach (var item in GradeItems)
                {
                    if (!selectedStudent.GradeDetails.TryGetValue(item.GradeItemId, out var gradeInfo))
                        continue;

                    
                    var label = new Label
                    {
                        Content = $"{gradeInfo.GradeItemName} ({gradeInfo.Weight}%)",
                        FontWeight = FontWeights.Bold,
                        Margin = new Thickness(0, 10, 0, 2)
                    };

                    
                    var textbox = new TextBox
                    {
                        Text = gradeInfo.Mark?.ToString() ?? "",
                        Width = 300,
                        Margin = new Thickness(0, 0, 0, 10),
                        Tag = item.GradeItemId
                    };

                    

                    spGradeItemDetails.Children.Add(label);
                    spGradeItemDetails.Children.Add(textbox);
                }
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (dgStudentGrades.SelectedItem is GradeViewModel selectedStudent)
            {
                if (spGradeItemDetails.Children.Count == 0)
                {
                    MessageBox.Show("No grade items to save.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if(cbCourses.SelectedItem is Course selectedCourse == false) return;
                {
                    int courseId = selectedCourse.CourseId;
                    string subjectId = selectedCourse.SubjectId;
                    
                    var grade = _context.Grades.FirstOrDefault(g => g.StudentId == selectedStudent.StudentId && g.CourseId == courseId);
                    
                    var existingMarks = _context.Marks.Where(m => m.GradeId == grade!.GradeId);
                    foreach(var child in spGradeItemDetails.Children)
                    {
                        if (child is TextBox textbox && textbox.Tag is int gradeItemId)
                        {
                            string rawMark = textbox.Text.Trim();
                            if (string.IsNullOrWhiteSpace(rawMark))
                                continue;

                            if (!double.TryParse(rawMark, out double parsedMark))
                            {
                                MessageBox.Show($"Invalid input for GradeItem ID {gradeItemId}. Please enter a numeric value.",
                                                "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                                return;
                            }

                           
                            if (parsedMark < 0 || parsedMark > 10)
                            {
                                MessageBox.Show($"The mark '{parsedMark}' is out of range (0–10). Please enter a valid value.",
                                                "Invalid Mark", MessageBoxButton.OK, MessageBoxImage.Warning);
                                return;
                            }

                            var existingMark = existingMarks.FirstOrDefault(m => m.GradeItemId == gradeItemId);
                            
                            if (existingMark != null)
                            {
                                existingMark.Mark1 = parsedMark;
                            }
                            else
                            {
                                var newMark = new Mark
                                {
                                    GradeId = grade!.GradeId,
                                    GradeItemId = gradeItemId,
                                    Mark1 = parsedMark
                                };
                                _context.Marks.Add(newMark);
                            }
                        }
                    }

                    _context.SaveChanges();
                    MessageBox.Show("Grades saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadGradeViewModel();

                }



            }

        }

        private void btnRefesh_Click(object sender, RoutedEventArgs e)
        {
            Begin();
            
        }
    }
    }
