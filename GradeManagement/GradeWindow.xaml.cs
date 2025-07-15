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



        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            Begin(); 

        }

       

        private void tabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is TabControl && tabControl.SelectedItem is TabItem selectedTab)
            {
                if (selectedTab.Header.ToString() == "Quản lý điểm")
                {
                    RefeshGradeTabs();
                }
            }
            if(e.Source is TabControl && tabControl.SelectedItem is TabItem selectedTab2)
            {
                if (selectedTab2.Header.ToString() == "Quản lý điểm thành phần")
                {
                    
                    ClearGradeItemFields();
                }
            }
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

            LoadGradeViewModel();
            LoadDgCourses();
            LoadSubjects();
            LoadLecturers();
        }

        private void LoadDgCourses()
        {
           
            dgCourses.ItemsSource = _context.Courses.Include(c => c.Lecturer).ToList();
           
        }

        private void LoadSubjects()
        {
            dgSubjects.ItemsSource = _context.Subjects.ToList();   
            subjectCount.Content = $"{_context.Subjects.Count()}";
        }

        private void LoadLecturers()
        {
            dgLecturers.ItemsSource = _context.UserAccounts
                .Where(u => u.Role == "lecturer")
                .ToList();
            lecturerCount.Content = $"{_context.UserAccounts.Count(u => u.Role == "lecturer")}";
        }   

        private void cbCourse_SelectionChanged(object sender, SelectionChangedEventArgs e)
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
            else
            {
                
                return;
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
        private void btnRefesh_Click(object sender, RoutedEventArgs e)
        {
            RefeshGradeTabs();

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
                cbSubjects.IsEnabled = true;
                ClearGradeItemFields();
                dgGradeItems.ItemsSource = _context.GradeItems
                    .Where(gi => gi.SubjectId == cbSubjects.SelectedValue as string)
                    .ToList();
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
            
            dgGradeItems.SelectedItem = null;
        }
        private void btnRefeshGradeItem_Click(object sender, RoutedEventArgs e)
        {
            ClearGradeItemFields();

        }

        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            if (dgStudentGrades.ItemsSource is List<GradeViewModel> studentList && studentList.Count > 0)
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "CSV files (*.csv)|*.csv",
                    FileName = "GradeExport.csv"
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

        private void ButtonAddLecturer_Click(object sender, RoutedEventArgs e)
        {
            if(string.IsNullOrWhiteSpace(txtLecturerName.Text) || string.IsNullOrWhiteSpace(txtLecturerEmail.Text) || string.IsNullOrWhiteSpace(txtLecturerPhone.Text) || !dpLecturerBirthDate.SelectedDate.HasValue)
            {
                MessageBox.Show("Hãy nhập đầy đủ thông tin giảng viên!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if(txtLecturerPhone.Text.Length < 10 || txtLecturerPhone.Text.Length > 11)
            {
                MessageBox.Show("Số điện thoại phải có độ dài từ 10 đến 11 ký tự!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (txtLecturerName.Text.Any(c => !char.IsLetter(c) && c != ' '))
            {
                MessageBox.Show("Tên giảng viên chỉ được chứa các chữ cái và khoảng trắng!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!txtLecturerPhone.Text.StartsWith("0"))
            {
                MessageBox.Show("Số điện thoại phải bắt đầu bằng số 0!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // kiểm tra email trùng lặp
            if (_context.UserAccounts.Any(u => u.Email == txtLecturerEmail.Text.Trim() && u.Role == "lecturer"))
            {
                MessageBox.Show("Email này đã được sử dụng!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (txtLecturerPhone.Text.Any(c => !char.IsDigit(c)))
            {
                MessageBox.Show("Số điện thoại chỉ được chứa các chữ số!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!txtLecturerEmail.Text.EndsWith("@fe.edu.vn"))
            {
                MessageBox.Show("Định dạng mail example@fe.edu.vn", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_context.UserAccounts.Any(u => u.PhoneNumber == txtLecturerPhone.Text.Trim() && u.Role == "lecturer"))
            {
                MessageBox.Show("Sđt này đã được sử dụng!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var newLecturer = new UserAccount
            
            {
                FullName = txtLecturerName.Text.Trim(),
                Email = txtLecturerEmail.Text.Trim(),
                PhoneNumber = txtLecturerPhone.Text.Trim(),
                Birthdate = DateOnly.FromDateTime(dpLecturerBirthDate.SelectedDate!.Value),
                Role = "lecturer",
                PasswordHash = "123456",
                Status = "Hoạt động"
            };

            _context.UserAccounts.Add(newLecturer);
            _context.SaveChanges();
            LoadLecturers();
            ClearLecturerFields();
        }

        private void dgLecturers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgLecturers.SelectedItem is UserAccount selectedLecturer)
            {

                txtLecturerName.Text = selectedLecturer.FullName;
                txtLecturerEmail.Text = selectedLecturer.Email;
                txtLecturerPhone.Text = selectedLecturer.PhoneNumber;
                dpLecturerBirthDate.SelectedDate = selectedLecturer.Birthdate.HasValue
                    ? selectedLecturer.Birthdate.Value.ToDateTime(new TimeOnly(0, 0))
                    : null;
                if (selectedLecturer.Status.Equals("Hoạt động"))
                {
                    btnLecturer.Content = "Khóa tài khoản";
                }
                else if (selectedLecturer.Status.Equals("Không hoạt động"))
                {
                    btnLecturer.Content = "Khôi phục tài khoản";
                }
                else
                {
                    btnLecturer.Content = "Khóa tài khoản";
                }
            }
            else
            {
                txtLecturerName.Text = string.Empty;
                txtLecturerEmail.Text = string.Empty;
                txtLecturerPhone.Text = string.Empty;
                dpLecturerBirthDate.SelectedDate = null;
            }
        }

        private void ClearLecturerFields()
        {
            txtLecturerName.Text = string.Empty;
            txtLecturerEmail.Text = string.Empty;
            txtLecturerPhone.Text = string.Empty;
            dpLecturerBirthDate.SelectedDate = null;
            dgLecturers.SelectedItem = null;
            txtSearchLecturer.Text = string.Empty;
            btnLecturer.Content = "Khóa tài khoản";
        }

        private void ButtonUpdateLecturer_Click(object sender, RoutedEventArgs e)
        {
            if(dgLecturers.SelectedItem is UserAccount selectedUser)
            {
                if (string.IsNullOrWhiteSpace(txtLecturerName.Text) || string.IsNullOrWhiteSpace(txtLecturerEmail.Text) || string.IsNullOrWhiteSpace(txtLecturerPhone.Text) || !dpLecturerBirthDate.SelectedDate.HasValue)
                {
                    MessageBox.Show("Hãy nhập đầy đủ thông tin giảng viên!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (txtLecturerPhone.Text.Length < 10 || txtLecturerPhone.Text.Length > 11)
                {
                    MessageBox.Show("Số điện thoại phải có độ dài từ 10 đến 11 ký tự!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (txtLecturerName.Text.Any(c => !char.IsLetter(c) && c != ' '))
                {
                    MessageBox.Show("Tên giảng viên chỉ được chứa các chữ cái và khoảng trắng!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!txtLecturerPhone.Text.StartsWith("0"))
                {
                    MessageBox.Show("Số điện thoại phải bắt đầu bằng số 0!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // kiểm tra email trùng lặp
                if (_context.UserAccounts.Any(u => u.Email == txtLecturerEmail.Text.Trim() && u.UserId != selectedUser.UserId && u.Role == "lecturer"))
                {
                    MessageBox.Show("Email này đã được sử dụng!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (txtLecturerPhone.Text.Any(c => !char.IsDigit(c)))
                {
                    MessageBox.Show("Số điện thoại chỉ được chứa các chữ số!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!txtLecturerEmail.Text.EndsWith("@fe.edu.vn"))
                {
                    MessageBox.Show("Định dạng mail example@fe.edu.vn", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (_context.UserAccounts.Any(u => u.PhoneNumber == txtLecturerPhone.Text.Trim() && u.UserId != selectedUser.UserId && u.Role == "lecturer"))
                {
                    MessageBox.Show("Số điện thoại này đã được sử dụng!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                selectedUser.FullName = txtLecturerName.Text.Trim();
                selectedUser.Email = txtLecturerEmail.Text.Trim();
                selectedUser.PhoneNumber = txtLecturerPhone.Text.Trim();
                selectedUser.Birthdate = DateOnly.FromDateTime(dpLecturerBirthDate.SelectedDate!.Value);
                
                _context.UserAccounts.Update(selectedUser);
                _context.SaveChanges();
                LoadLecturers();
                ClearLecturerFields();
            }
        }

        private void ButtonDeleteLecturer_Click(object sender, RoutedEventArgs e)
        {
            if(dgLecturers.SelectedItem is UserAccount selectedUser)
            {
                if (selectedUser.Status.Equals("Hoạt động"))
                {
                    
                    var confirmResult = MessageBox.Show("Bạn có chắc chắn muốn khóa giảng viên này không?", "Xác nhận khóa", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (confirmResult == MessageBoxResult.Yes)
                    {
                        selectedUser.Status = "Không hoạt động";
                        _context.UserAccounts.Update(selectedUser);
                        _context.SaveChanges();
                        LoadLecturers();
                        ClearLecturerFields();
                    }
                }
                else if (selectedUser.Status.Equals("Không hoạt động"))
                {
                    var confirmResult = MessageBox.Show("Bạn có chắc chắn muốn khôi phục giảng viên này không?", "Xác nhận khôi phục", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (confirmResult == MessageBoxResult.Yes)
                    {
                        selectedUser.Status = "Hoạt động";
                        _context.UserAccounts.Update(selectedUser);
                        _context.SaveChanges();
                        LoadLecturers();
                        ClearLecturerFields();
                    }
                }

            }
            else
            {
                MessageBox.Show("chưa chọn gì", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ButtonSearchLecturer_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSearchLecturer.Text))
            {
                dgLecturers.ItemsSource = _context.UserAccounts
                    .Where(u => u.Role == "lecturer")
                    .ToList();
            }
            else
            {
                string searchText = txtSearchLecturer.Text.Trim().ToLower();
                dgLecturers.ItemsSource = _context.UserAccounts
                    .Where(u => u.Role == "lecturer" && (u.FullName.ToLower().Contains(searchText) || u.Email.ToLower().Contains(searchText) || u.PhoneNumber!.Contains(searchText)))
                    .ToList();
            }
        }
        private void ButtonRefeshLecturer_Click(object sender, RoutedEventArgs e)
        {
            ClearLecturerFields();
            LoadLecturers();
        }

        private void ClearSubjectTab()
        {
            txtSubjectId.Text = string.Empty;
            txtSubjectName.Text = string.Empty;
            txtCredits.Text = string.Empty;
            dgSubjects.SelectedItem = null;
            txtSearchSubject.Text = string.Empty;
        }

        private void ButtonAddSubject_Click(object sender, RoutedEventArgs e)
        {
            if(string.IsNullOrWhiteSpace(txtSubjectId.Text) || string.IsNullOrWhiteSpace(txtSubjectName.Text) || string.IsNullOrWhiteSpace(txtCredits.Text))
            {
                MessageBox.Show("Hãy nhập đầy đủ thông tin môn học!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!int.TryParse(txtCredits.Text, out int credits) || credits <= 0)
            {
                MessageBox.Show("Số tín chỉ phải là một số nguyên dương!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var newSubject = new Subject
            {
                SubjectId = txtSubjectId.Text.Trim(),
                SubjectName = txtSubjectName.Text.Trim(),
                Credit = credits
            };
            // Kiểm tra xem môn học đã tồn tại chưa
            if (_context.Subjects.Any(s => s.SubjectId == newSubject.SubjectId))
            {
                MessageBox.Show("Môn học này đã tồn tại!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            _context.Subjects.Add(newSubject);
            _context.SaveChanges();
            LoadSubjects();
            ClearSubjectTab();

        }

        private void dgSubjects_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(dgSubjects.SelectedItem is Subject selectedSubject)
            {
                txtSubjectId.Text = selectedSubject.SubjectId;
                txtSubjectName.Text = selectedSubject.SubjectName;
                txtCredits.Text = selectedSubject.Credit.ToString(); ;
                txtSubjectId.IsReadOnly = true; // Chỉ cho phép chỉnh sửa tên và số tín chỉ, không cho phép chỉnh sửa mã môn học
            }
            else
            {
                txtSubjectId.Text = string.Empty;
                txtSubjectName.Text = string.Empty;
                txtCredits.Text = string.Empty;
            }
        }

        private void ButtonUpdateSubject_Click(object sender, RoutedEventArgs e)
        {
            if(dgSubjects.SelectedItem is Subject selectedSubject)
            {
                if (string.IsNullOrWhiteSpace(txtSubjectId.Text) || string.IsNullOrWhiteSpace(txtSubjectName.Text) || string.IsNullOrWhiteSpace(txtCredits.Text))
                {
                    MessageBox.Show("Hãy nhập đầy đủ thông tin môn học!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (!int.TryParse(txtCredits.Text, out int credits) || credits <= 0)
                {
                    MessageBox.Show("Số tín chỉ phải là một số nguyên dương!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                

                selectedSubject.SubjectName = txtSubjectName.Text.Trim();
                selectedSubject.Credit = credits;
                _context.Subjects.Update(selectedSubject);
                _context.SaveChanges();
                LoadSubjects();
                ClearSubjectTab();
            }
            else
            {
                MessageBox.Show("Hãy chọn một môn học để chỉnh sửa!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ButtonSearchSubject_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSearchSubject.Text))
            {
                dgSubjects.ItemsSource = _context.Subjects.ToList();
            }
            else
            {
                string searchText = txtSearchSubject.Text.Trim().ToLower();
                dgSubjects.ItemsSource = _context.Subjects
                    .Where(s => s.SubjectId.ToLower().Contains(searchText) || s.SubjectName!.ToLower().Contains(searchText))
                    .ToList();
            }
        }


        private void ButtonRefeshSubject_Click(object sender, RoutedEventArgs e)
        {
            ClearSubjectTab();
            LoadSubjects();
        }

    }
}
