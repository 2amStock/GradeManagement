using GradeManagement.DAL.Models;
using GradeManagement.DAL.ViewModels;
using Microsoft.IdentityModel.Tokens;
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
            cbSubjects.SelectedIndex = 0;
            cbCategorys.SelectedIndex = 0;
            dgStudentGrades.SelectedItem = null;
            dgGradeItems.SelectedItem = null;
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
                if (cbCourses.SelectedItem is Course selectedCourse == false) return;
                {
                    int courseId = selectedCourse.CourseId;
                    string subjectId = selectedCourse.SubjectId;

                    var grade = _context.Grades.FirstOrDefault(g => g.StudentId == selectedStudent.StudentId && g.CourseId == courseId);

                    var existingMarks = _context.Marks.Where(m => m.GradeId == grade!.GradeId);
                    foreach (var child in spGradeItemDetails.Children)
                    {
                        if (child is TextBox textbox && textbox.Tag is int gradeItemId)
                        {
                            string rawMark = textbox.Text.Trim();

                            
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
            cbCourses.SelectedValuePath = "CourseId";
            cbCourses.DisplayMemberPath = "DisplayName";
            cbCourses.ItemsSource = _context.Courses.ToList();
            cbSubjects.SelectedValuePath = "SubjectId";
            cbSubjects.DisplayMemberPath = "SubjectId";
            cbSubjects.ItemsSource = _context.Subjects.ToList();
            cbCategorys.SelectedValuePath = "GradeCategoryId";
            cbCategorys.DisplayMemberPath = "GradeCategoryName";
            cbCategorys.ItemsSource = _context.GradeCategories.ToList();
           
            dgStudentGrades.SelectedItem = null;
            dgGradeItems.SelectedItem = null;
            spGradeItemDetails.Children.Clear();
            LoadGradeViewModel();

        }

        private void btnAddGradeItem_Click(object sender, RoutedEventArgs e)
        {
            if (txtGradeItemName.Text.IsNullOrEmpty() || txtWeight.Text.IsNullOrEmpty()) { MessageBox.Show("Hãy nhập đầy đủ thông tin!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

            if (!double.TryParse(txtWeight.Text, out double weight) || weight <= 0 || weight > 100)
            {
                MessageBox.Show("Trọng số thuộc khoảng 0-100", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            GradeItem newGradeItem = new GradeItem
            {
                GradeItemName = txtGradeItemName.Text.Trim(),
                Weight = weight,
                SubjectId = cbSubjects.SelectedValue as string,
                GradeCategoryId = (int?)cbCategorys.SelectedValue
            };

            //check if the GradeItem already exists
            if (_context.GradeItems.Any(gi => gi.GradeItemName == newGradeItem.GradeItemName && gi.SubjectId == newGradeItem.SubjectId))
            {
                MessageBox.Show("Điểm thành phần này đã tồn tại!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            _context.GradeItems.Add(newGradeItem);
           
            _context.SaveChanges();
            dgGradeItems.ItemsSource = null;
            dgGradeItems.ItemsSource = _context.GradeItems
                .Where(gi => gi.SubjectId == cbSubjects.SelectedValue as string)
                .ToList();
            ClearGradeItemFields();
            MessageBox.Show("Đã thêm điểm thành phần thành công!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void btnEditGradeItem_Click(object sender, RoutedEventArgs e)
        {
            if (dgGradeItems.SelectedItem is GradeItem selectedGradeItem)
            {
                if (string.IsNullOrWhiteSpace(txtGradeItemName.Text) || string.IsNullOrWhiteSpace(txtWeight.Text))
                {
                    MessageBox.Show("Hãy nhập đầy đủ thông tin!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (!double.TryParse(txtWeight.Text, out double weight) || weight <= 0 || weight > 100)
                {
                    MessageBox.Show("Trọng số thuộc khoảng 0-100", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                selectedGradeItem.GradeItemName = txtGradeItemName.Text.Trim();
                selectedGradeItem.Weight = weight;
                selectedGradeItem.SubjectId = cbSubjects.SelectedValue as string;
                selectedGradeItem.GradeCategoryId = (int?)cbCategorys.SelectedValue;
                _context.SaveChanges();
                ClearGradeItemFields();
                dgGradeItems.ItemsSource = _context.GradeItems
                    .Where(gi => gi.SubjectId == cbSubjects.SelectedValue as string)
                    .ToList();
                
                MessageBox.Show("Đã cập nhật điểm thành phần thành công!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Hãy chọn một điểm thành phần để chỉnh sửa!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void btnDeleteGradeItem_Click(object sender, RoutedEventArgs e)
        {
            if (dgGradeItems.SelectedItem is GradeItem selectedGradeItem)
            {
                    var confirmResult = MessageBox.Show("Bạn có chắc chắn muốn xóa điểm thành phần này không?", "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (confirmResult == MessageBoxResult.Yes)
                    {
                    var itemToDelete = _context.GradeItems.FirstOrDefault(g => g.GradeItemId == selectedGradeItem.GradeItemId);
                    if (itemToDelete == null)
                    {
                        MessageBox.Show("Không tìm thấy GradeItem.");
                        return;
                    }

                    // Xóa thủ công các bản ghi Mark liên quan
                    var relatedMarks = _context.Marks.Where(m => m.GradeItemId == selectedGradeItem.GradeItemId).ToList();
                    _context.Marks.RemoveRange(relatedMarks);

                    // Sau đó mới xóa GradeItem
                    _context.GradeItems.Remove(itemToDelete);
                    _context.SaveChanges();

                    MessageBox.Show("Đã xóa GradeItem thành công.");
                    dgGradeItems.ItemsSource = _context.GradeItems
                            .Where(gi => gi.SubjectId == cbSubjects.SelectedValue as string)
                            .ToList();
                        ClearGradeItemFields();
                    }
            }
            else
            {
                MessageBox.Show("Hãy chọn một điểm thành phần để xóa!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void cbSubjects_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbSubjects.SelectedItem is Subject selectedSubject)
            {
                dgGradeItems.ItemsSource = _context.GradeItems
                    .Where(gi => gi.SubjectId == selectedSubject.SubjectId)
                    .ToList();
            }
            else
            {
                dgGradeItems.ItemsSource = null;
            }
        }

        private void dgGradeItems_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgGradeItems.SelectedItem is GradeItem selectedGradeItem)
            {
                txtGradeItemName.Text = selectedGradeItem.GradeItemName;
                txtWeight.Text = selectedGradeItem.Weight.ToString();
                cbCategorys.SelectedValue = selectedGradeItem.GradeCategoryId;
                cbSubjects.SelectedValue = selectedGradeItem.SubjectId;
                cbSubjects.IsEnabled = false;
            }
           
        }

        public void ClearGradeItemFields()
        {
            txtGradeItemName.Text = string.Empty;
            txtWeight.Text = string.Empty;           
            cbSubjects.IsEnabled = true;
        }
        private void btnRefeshGradeItem_Click(object sender, RoutedEventArgs e)
        {
            ClearGradeItemFields();
            
        }
    }
}
