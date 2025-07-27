using GradeManagement.DAL.Models;
using GradeManagement.DAL.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
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
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private UserAccount Lecturer = null;
        private GradeManagementSystemContext _context = null;
        private List<GradeItem> GradeItems = new List<GradeItem>();
        public MainWindow(UserAccount userAccount)
        {
            InitializeComponent();
            Lecturer = userAccount;
            _context = new GradeManagementSystemContext();

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadCBCourses();
            LoadGradeViewModel();
        }

        private void LoadCBCourses()
        {
            cbCourses.SelectedValuePath = "CourseId";
            cbCourses.DisplayMemberPath = "DisplayName";
            cbCourses.ItemsSource = _context.Courses.Include(c => c.Lecturer)
                                                     .Where(c => c.LecturerId == Lecturer.UserId)
                                                     .ToList();
            cbCourses.SelectedIndex = 0;
        }

        private void cbCourses_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadGradeViewModel();
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


        private void LoadGradeViewModel()
        {
            if (cbCourses.SelectedItem is Course selectedCourse)
            {
                spGradeItemDetails.Children.Clear();

                int courseId = selectedCourse.CourseId;
                string subjectId = selectedCourse.SubjectId;


                GradeItems = _context.GradeItems
                    .Where(gi => gi.SubjectId == subjectId).OrderBy(gi => gi.Weight)
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
            else
            {

                return;
            }
        }

        private void dgStudentGrades_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
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

                if (cbCourses.SelectedItem is Course selectedCourse == false) return;
                {
                    int courseId = selectedCourse.CourseId;
                    string subjectId = selectedCourse.SubjectId;

                    var grade = _context.Grades.FirstOrDefault(g => g.StudentId == selectedStudent!.StudentId && g.CourseId == courseId);

                    var existingMarks = _context.Marks.Where(m => m.GradeId == grade!.GradeId);
                    foreach (var child in spGradeItemDetails.Children)
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
            else
            {
                MessageBox.Show("Please select a student to save grades.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

        }

        private void RefeshGradeTabs()
        {
            LoadCBCourses();


            dgStudentGrades.SelectedItem = null;

            spGradeItemDetails.Children.Clear();
            LoadGradeViewModel();

        }
        private void btnRefesh_Click(object sender, RoutedEventArgs e)
        {
            RefeshGradeTabs();

        }

        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            if (dgStudentGrades.ItemsSource is List<GradeViewModel> studentList && studentList.Count > 0)
            {
                if (cbCourses.SelectedItem is Course selectedCourse)
                {   
                    
                    SaveFileDialog saveFileDialog = new SaveFileDialog
                    {
                        Filter = "CSV files (*.csv)|*.csv",
                        FileName = selectedCourse.DisplayName + ".csv"
                    };

                    if (saveFileDialog.ShowDialog() == true)
                    {
                        var lines = new List<string>();
                        var headers = new List<string> { "RollNumber", "FullName" };

                        // Thêm tên grade items vào header
                        headers.AddRange(GradeItems.Select(g => g.GradeItemName)!);
                        lines.Add(string.Join(",", headers));

                        // Thêm dữ liệu từng sinh viên
                        foreach (var student in studentList)
                        {
                            var values = new List<string> { student.RollNumber, student.FullName };
                            foreach (var item in GradeItems)
                            {
                                student.GradeDetails.TryGetValue(item.GradeItemId, out var detail);
                                values.Add(detail?.Mark?.ToString() ?? "");
                            }
                            lines.Add(string.Join(",", values));
                        }

                        File.WriteAllLines(saveFileDialog.FileName, lines);
                        MessageBox.Show("Export thành công!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            else
            {
                MessageBox.Show("Không có dữ liệu để xuất!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void btnImport_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                var lines = File.ReadAllLines(openFileDialog.FileName);
                if (lines.Length < 2)
                {
                    MessageBox.Show("File không có dữ liệu!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var header = lines[0].Split(',');
                var gradeItemNameIndex = header.Skip(2).ToList(); // Bỏ qua RollNumber và FullName

                var gradeItemMap = GradeItems.ToDictionary(g => g.GradeItemName, g => g.GradeItemId);

                for (int i = 1; i < lines.Length; i++)
                {
                    var cols = lines[i].Split(',');
                    var rollNumber = cols[0].Trim();
                    var student = _context.Students.FirstOrDefault(s => s.RollNumber == rollNumber);
                    if (student == null) continue;

                    var selectedCourse = cbCourses.SelectedItem as Course;
                    var grade = _context.Grades.FirstOrDefault(g => g.StudentId == student.StudentId && g.CourseId == selectedCourse.CourseId);
                    if (grade == null) continue;

                    for (int j = 2; j < cols.Length; j++)
                    {
                        var gradeItemName = header[j].Trim();
                        if (!gradeItemMap.TryGetValue(gradeItemName, out int gradeItemId)) continue;

                        if (!double.TryParse(cols[j], out double markValue)) continue;

                        var existingMark = _context.Marks.FirstOrDefault(m => m.GradeId == grade.GradeId && m.GradeItemId == gradeItemId);
                        if (existingMark != null)
                        {
                            existingMark.Mark1 = markValue;
                        }
                        else
                        {
                            _context.Marks.Add(new Mark
                            {
                                GradeId = grade.GradeId,
                                GradeItemId = gradeItemId,
                                Mark1 = markValue
                            });
                        }
                    }
                }

                _context.SaveChanges();
                LoadGradeViewModel();
                MessageBox.Show("Import thành công!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        private void ButtonLogout_Click(object sender, RoutedEventArgs e)
        {

            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();
            this.Close();
        }
    }





}